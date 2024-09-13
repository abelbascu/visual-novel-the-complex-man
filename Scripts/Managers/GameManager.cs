using Godot;
using System;
using System.Security.Cryptography.X509Certificates;
using static GameStateMachine;
using System.Threading.Tasks;


public partial class GameManager : Control {

  [Export] public string language { get; set; } = "";
  public static GameManager Instance { get; private set; }
  public DialogueManager DialogueManager { get; private set; }
  public UIManager UIManager { get; private set; }
  public GameStateManager GameStateManager { get; private set; }
  public InputManager InputManager { get; private set; }
  //public PlayerStateManager PlayerStateManager { get; private set; }
  // public MediaManager MediaManager { get; private set; }
  // public MinigameManager MinigameManager { get; private set; }
  // public AchievementsManager AchievementsManager { get; private set; }
  //private GameStateMachine stateMachine;
  private PackedScene mainMenuPackedScene;
  private Control mainMenuScene;
  private MainMenu mainMenu;


  public enum GameState {
    MainMenu,
    Dialogue,
    Cinematic,
    Minigame
  }

  public override void _Ready() {


    // Make GameManager fill its parent
    AnchorRight = 1;
    AnchorBottom = 1;
    // Ignore mouse input if it doesn't need to interact directly
    MouseFilter = MouseFilterEnum.Ignore;

    if (Instance == null) {
      Instance = this;
      GameInit();
    } else {
      QueueFree();
    }

    SetupInitialLanguage();
    LoadTranslations();
  }


  private void SetupInitialLanguage() {
    //***** SET LANGUAGE HERE *****
    //we check what language the user has in his Windows OS
    string currentCultureName = System.Globalization.CultureInfo.CurrentCulture.Name;
    string[] parts = currentCultureName.Split('-');
    language = parts[0];
    TranslationServer.SetLocale(language);
    //for testing purposes, will change the language directly here so we do not have to tinker witn Windows locale settings each time
    language = "en";
    TranslationServer.SetLocale(language);
  }

  private void LoadTranslations() {
    // Get the directory path
    string translationsDir = "res://Translations/";

    // Use DirAccess to iterate through files
    using var dir = DirAccess.Open(translationsDir);
    if (dir != null) {
      dir.ListDirBegin();
      string fileName = dir.GetNext();
      while (fileName != "") {
        if (!dir.CurrentIsDir() && fileName.EndsWith(".translation")) {
          string fullPath = translationsDir + fileName;
          var translation = GD.Load<Translation>(fullPath);
          if (translation != null) {
            TranslationServer.AddTranslation(translation);
            GD.Print($"Loaded translation: {fullPath}");
          } else {
            GD.PrintErr($"Failed to load translation: {fullPath}");
          }
        }
        fileName = dir.GetNext();
      }
    } else {
      GD.PrintErr("An error occurred when trying to access the translations directory.");
    }

    // Print loaded translations for debugging
    var loadedLocales = TranslationServer.GetLoadedLocales();
    foreach (string locale in loadedLocales) {
      GD.Print($"Loaded translation locale: {locale}");
    }
  }

  private void GameInit() {

    UIManager = GetNodeOrNull<UIManager>("UIManager");
    DialogueManager = GetNodeOrNull<DialogueManager>("DialogueManager");
    GameStateManager = GetNodeOrNull<GameStateManager>("GameStateManager");
    InputManager = GetNodeOrNull<InputManager>("InputManager");
    //here we set the first FSM state
    GameStateManager.Instance.Fire(Trigger.DISPLAY_SPLASH_SCREEN);
  }

  public void Display_Splash_Screen() {
    UIManager.Instance.ShowSplashScreen();
    // UIManager.mainMenu.CloseMainMenu();
  }

  public async Task Display_Main_Menu() {
    //UIManager.HideSplashScreen();

    if (UIManager.Instance.mainMenu.MainOptionsContainer.Visible == true)
      await UIManager.Instance.mainMenu.CloseInGameMenu();

    await UIManager.Instance.mainMenu.DisplayMainMenu();
  }

  public async Task Display_Main_Menu_Container_Only() {
    await UIManager.Instance.mainMenu.DisplayMainMenuContainerOnly();
  }

  public async Task Display_Ingame_Menu() {

    await UIManager.Instance.mainMenu.DisplayInGameMenu();
  }

  public async Task Close_Ingame_Menu() {
    await UIManager.Instance.mainMenu.CloseInGameMenu();
  }


  public async Task Go_Back_To_Menu() {
    GetTree().CallGroup("popups", "close_all");

    if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed) {
      await UIManager.Instance.mainMenu.DisplayMainMenuContainerOnly();
    } else if (GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed) {
      await UIManager.Instance.mainMenu.DisplayInGameMenu();
    }
  }

  public async Task Starting_New_Game() {
    //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
    GameStateManager.Instance.Fire(Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN);
  }

  public async Task Display_Enter_Your_Name_Screen() {
    await UIManager.Instance.inputNameScreen.Show();
    UIManager.Instance.splashScreen.Hide();
    // UIManager.Instance.inputNameScreen.Show();
  }

  public void Enter_Dialogue_Mode() {
    if (GameStateManager.Instance.CurrentState == State.InDialogueMode) {
      LoadSaveManager.Instance.ResumeGameTimer();
      UIManager.Instance.dialogueBoxUI.TopLevel = true;
      UIManager.Instance.playerChoicesBoxUI.TopLevel = true;
      UIManager.Instance.inGameMenuButton.EnableIngameMenuButton();
    }
  }

  public async Task Display_New_Game_Dialogues() {

    DialogueManager.Instance.currentDialogueID = DialogueManager.STARTING_DIALOGUE_ID;
    DialogueManager.Instance.currentConversationID = DialogueManager.STARTING_CONVO_ID;
    DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject
        (DialogueManager.Instance.currentConversationID, DialogueManager.Instance.currentDialogueID);
    DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);

    await UIManager.Instance.FadeOutScreenOverlay(2.0f);

    //await UIManager.Instance.FadeOut();
    GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
    UIManager.Instance.inGameMenuButton.Show();
  }

  public async Task Resume_Game_From_Ingame_Menu_Closed() {

    await UIManager.Instance.mainMenu.CloseInGameMenu();
  }

  public async Task Load_Game(string saveFilePath) {

    //UIManager.Instance.mainMenu.DisableAllButtons();
    UIManager.Instance.menuOverlay.Visible = false;
    LoadSaveManager.Instance.LoadGame(saveFilePath);
    await UIManager.Instance.FadeInScreenOverlay(1.5f);
    UIManager.Instance.mainMenu.SetContainerButtonsVisibility(UIManager.Instance.mainMenu.MainOptionsContainer, false);
    UIManager.Instance.mainMenu.SetContainerButtonsVisibility(UIManager.Instance.mainMenu.LanguageOptionsContainer, false);
    UIManager.Instance.saveGameScreen.DeleteAllSaveOrLoadSlots();
    UIManager.Instance.saveGameScreen.goBackButton.Visible = false;
    UIManager.Instance.saveGameScreen.Hide();

    GameStateManager.Instance.Fire(Trigger.COMPLETE_LOADING_BASED_ON_GAME_MODE, GameStateManager.Instance.GetLastGameMode());
  }

  public async Task Initialize_Load_Screen() {

    bool isLoadScreen = false;

    GD.Print("In Initialize_Load_Screen, before calling SetUpSaveOrLoadScreen");
    isLoadScreen = await UIManager.saveGameScreen.SetUpSaveOrLoadScreen(UIManager.Instance.mainMenu.LOAD_SCREEN);

    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");

    if (isLoadScreen) {
      GD.Print("In Initialize_Load_Screen, before triggering DISPLAY_LOAD_SCREEN");
      GameStateManager.Instance.Fire(Trigger.DISPLAY_LOAD_SCREEN);
    } else
      GameStateManager.Instance.Fire(Trigger.DISPLAY_SAVE_SCREEN);
  }

  public async Task Display_Load_Screen() {


    if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed)
      await UIManager.Instance.mainMenu.CloseMainMenu();
    else
      await UIManager.Instance.mainMenu.CloseInGameMenu();
    GD.Print("In Display_Load_Screen, before triggering DisplaySaveScreen, no triggers here");
    await UIManager.Instance.saveGameScreen.DisplaySaveScreen();

  }

  public async Task Complete_Loading_Based_On_Game_Mode(State LastGameMode) {


    switch (LastGameMode) {
      case State.InDialogueMode:
        await Initialize_Dialogue_Mode_Settings_On_Loaded_Game();
        GameStateManager.Instance.Fire(Trigger.LOADING_COMPLETED); //WE NEED TO ADD THIS ONE ON EVERY NEW CASE!!
        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        break;

      default:
        //if gamemode was not saved correctly, we try to load the last saved dialogue or player choices 
        LastGameMode = State.InDialogueMode;
        await Initialize_Dialogue_Mode_Settings_On_Loaded_Game();
        GameStateManager.Instance.Fire(Trigger.LOADING_COMPLETED); //WE NEED TO ADD THIS ONE ON EVERY NEW CASE!!
        UIManager.Instance.mainMenu.SetContainerButtonsVisibility(UIManager.Instance.mainMenu.MainOptionsContainer, false);
        UIManager.Instance.mainMenu.SetContainerButtonsVisibility(UIManager.Instance.mainMenu.LanguageOptionsContainer, false);
        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        break;
    }
  }

  //THIS LOOKS LIKE SHOULD BE MOVED TO DIALOGUE MANAGER
  //THIS LOOKS LIKE SHOULD BE MOVED TO DIALOGUE MANAGER
  public async Task Initialize_Dialogue_Mode_Settings_On_Loaded_Game() {

    UIManager.Instance.inGameMenuButton.DisableIngameMenuButton();
    UIManager.Instance.inGameMenuButton.Show();
    //A BIT HACKY FIX where if the currentDialogObj is a PlayerChoice, and the user clicked on it and then saved the game, and if its DestinationDialogueID are PlayerChoices,
    //it means that they were all already saved in a List and displayed to the screen, so when loading that saved game it should not display that currentDialogObj, but only its associated playerChoices
    if (DialogueManager.Instance.playerChoicesList != null && DialogueManager.Instance.currentDialogueObject.Actor == "1") //if the current dialogue object it's a single player choice
    {
      //notice that we don't use DisplayDialogueOrPlayerChoice(DialogueObject dialogObj) to avoid displaying the already visited player choice that is still saved in the current dialogue object
      //until the player selects a new player choice. Notice that most times, after an NPC or actor dialogue, a group of player choices may be displayed, but it may also happen that after a 
      //player choice is displayed, more new player chocies are displayed. We are solving this rare case here. 
      DialogueManager.Instance.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, DialogueManager.Instance.SetIsPlayerChoiceBeingPrinted);
      VisualManager.Instance.DisplayVisual(VisualManager.Instance.VisualPath, VisualManager.Instance.visualType);
    } else
      DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);

    await UIManager.Instance.FadeOutScreenOverlay(1.5f);
    UIManager.Instance.inGameMenuButton.EnableIngameMenuButton();
  }


  public async Task Initialize_Save_Screen() {

    await UIManager.saveGameScreen.SetUpSaveOrLoadScreen(UIManager.Instance.mainMenu.SAVE_SCREEN);

  }

  public async Task Display_Save_Screen() {
    await UIManager.Instance.saveGameScreen.DisplaySaveScreen();
  }

  public async Task Display_Language_Menu() {
    await UIManager.Instance.mainMenu.DisplayLanguageMenu();
  }

  public async Task Save_Game() {
    await LoadSaveManager.Instance.PerformManualSave();
  }

  public async Task Autosave_Game() {
    UIManager.Instance.inGameMenuButton.DisableIngameMenuButton();
    UIManager.Instance.mainMenu.HideIngameMenuIcon();
    await LoadSaveManager.Instance.PerformAutosave();
  }

  public void Autosave_Completed() {
    UIManager.Instance.mainMenu.ShowIngameMenuIcon();
  }

  public async Task ResumeGame() {
    await UIManager.Instance.mainMenu.CloseInGameMenu();
  }

  public void Exit_Game() {
    GetTree().Quit();
  }


}
