using Godot;
using System;
using System.Security.Cryptography.X509Certificates;
using static GameStateMachine;


public partial class GameManager : Control {
    public static GameManager Instance { get; private set; }
    public DialogueManager DialogueManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public GameStateManager GameStateManager { get; private set; }
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
    }

    private void GameInit() {
        // Note: Despite being displayed as "UIManager" in the Godot editor, the actual internal
        // node name is "UiManager" (with a lowercase 'i'). This seems to be a Godot bug.
        UIManager = GetNodeOrNull<UIManager>("UiManager");
        DialogueManager = GetNodeOrNull<DialogueManager>("DialogueManager");
        GameStateManager = GetNodeOrNull<GameStateManager>("GameStateManager");

        GameStateManager.Instance.Fire(Trigger.DISPLAY_SPLASH_SCREEN);
    }

    public void Display_Splash_Screen() {
        UIManager.ShowSplashScreen();
        UIManager.mainMenu.CloseMainMenu();
    }

    public void Display_Main_Menu() {
        //UIManager.HideSplashScreen();
        UIManager.mainMenu.CloseInGameMenu();
        //UIManager.Instance.HideAllUIElements();
        VisualManager.Instance.RemoveImage();
        UIManager.mainMenu.DisplayMainMenu();
    }

    public void Display_Ingame_Menu() {
        UIManager.mainMenu.DisplayInGameMenu();
        LoadSaveManager.Instance.ToggleAutosave(false);
    }

    public void Close_Ingame_Menu()
    {
        UIManager.Instance.mainMenu.CloseInGameMenu();
    }

    public void Resume_To_Dialogue_Mode(){
        //at the moment we can only go back to dialogue mode when we close the ingame menu
        Close_Ingame_Menu();
    }

    public void Go_Back_To_Menu() {
        GetTree().CallGroup("popups", "close_all");
        UIManager.mainMenu.MainOptionsContainer.Show();
    }

    public void Starting_New_Game() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        GameStateManager.Instance.Fire(Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN);
    }

    public void Display_Enter_Your_Name_Screen() {
        UIManager.Instance.inputNameScreen.Show();
        UIManager.Instance.splashScreen.Hide();
        UIManager.Instance.inputNameScreen.FadeIn();
    }

    public void Display_New_Game_Dialogues() {
        UIManager.Instance.inGameMenuButton.Show();
        DialogueManager.Instance.currentDialogueID = DialogueManager.STARTING_DIALOGUE_ID;
        DialogueManager.Instance.currentConversationID = DialogueManager.STARTING_CONVO_ID;
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject
            (DialogueManager.Instance.currentConversationID, DialogueManager.Instance.currentDialogueID);
        DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);
        LoadSaveManager.Instance.ToggleAutosave(true);

        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
    }

    public void Resume_Game_From_Ingame_Menu_Closed() {
        UIManager.Instance.mainMenu.CloseInGameMenu();
    }

    public void Load_Game(string saveFilePath) {
        UIManager.Instance.HideAllUIElements();
        //WE NEED CHANGE TO THE 'LOADING' STATE WHILE DATA IS BEING LOADED. WE NEED TO IMPLEMENT AN ANIMATED LOADING SYMBOL TO WARN THE USER.
        //WE NEED CHANGE TO THE 'LOADING' STATE WHILE DATA IS BEING LOADED. WE NEED TO IMPLEMENT AN ANIMATED LOADING SYMBOL TO WARN THE USER.
        //WE NEED CHANGE TO THE 'LOADING' STATE WHILE DATA IS BEING LOADED. WE NEED TO IMPLEMENT AN ANIMATED LOADING SYMBOL TO WARN THE USER.
        UIManager.Instance.menuOverlay.Visible = false;
        LoadSaveManager.Instance.LoadGame(saveFilePath);
        if (UIManager.Instance.mainMenu.IsVisibleInTree()) {
            UIManager.Instance.mainMenu.CloseMainMenu();
        }
        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
    }

    public void Initialize_Save_Screen() {
        UIManager.saveGameScreen.SetUpSaveOrLoadScreen(UIManager.mainMenu.SAVE_SCREEN);
    }

    public void Display_Language_Menu() {
         UIManager.Instance.mainMenu.MainOptionsContainer.Hide();
        UIManager.Instance.mainMenu.LanguageOptionsContainer.Show();
    }

    // public void Display_Save_Screen() {
    //     UIManager.saveGameScreen.DisplaySaveOrLoadScreen();
    // }

    public void Save_Game(bool isAutosave) {
        LoadSaveManager.Instance.SaveGame(isAutosave);
        UIManager.Instance.saveGameScreen.RefreshSaveSlots();
        GameStateManager.Instance.Fire(Trigger.SAVING_COMPLETED);
        GameStateManager.Instance.Fire(Trigger.DISPLAY_SAVE_SCREEN);
    }

    public void Autosave_Game(bool isAutoSave) {
        Save_Game(isAutoSave);
        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
    }

    public void Initialize_Load_Screen() {
        UIManager.saveGameScreen.SetUpSaveOrLoadScreen(UIManager.mainMenu.LOAD_SCREEN);
    }

    // public void Display_Load_Screen() {
    //     UIManager.saveGameScreen.DisplaySaveOrLoadScreen();
    // }

    public void ResumeGame() {
        UIManager.mainMenu.CloseInGameMenu();
        LoadSaveManager.Instance.ToggleAutosave(true);
    }

}
