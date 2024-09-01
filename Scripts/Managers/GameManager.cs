using Godot;
using System;
using System.Security.Cryptography.X509Certificates;
using static GameStateMachine;
using System.Threading.Tasks;


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

    public async Task Display_Ingame_Menu() {
        await UIManager.Instance.mainMenu.DisplayInGameMenu();
    }

    public async Task Close_Ingame_Menu() {
        await UIManager.Instance.mainMenu.CloseInGameMenu();
    }

    public async Task Resume_To_Dialogue_Mode() {
        //at the moment we can only go back to dialogue mode when we close the ingame menu
        await Close_Ingame_Menu();
    }

    public async Task Go_Back_To_Menu() {
        GetTree().CallGroup("popups", "close_all");

        if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed) {
            await UIManager.Instance.mainMenu.DisplayMainMenu();
        } else {
            await UIManager.Instance.mainMenu.DisplayInGameMenu();
        }
        //     UIManager.Instance.mainMenu.MainOptionsContainer.TopLevel = true;
        //     UIManager.Instance.mainMenu.MainOptionsContainer.Show();
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

    public async Task Display_New_Game_Dialogues() {

        //WE NEED A FADE IN HERE
        //WE NEED A FADE IN HERE

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
        UIManager.Instance.HideAllUIElements();
        //WE NEED CHANGE TO THE 'LOADING' STATE WHILE DATA IS BEING LOADED. WE NEED TO IMPLEMENT AN ANIMATED LOADING SYMBOL TO WARN THE USER.
        UIManager.Instance.menuOverlay.Visible = false;
        LoadSaveManager.Instance.LoadGame(saveFilePath);
        if (UIManager.Instance.mainMenu.IsVisibleInTree()) {
            await UIManager.Instance.mainMenu.CloseMainMenu();
        }

        GameStateManager.Instance.Fire(Trigger.COMPLETE_LOADING_BASED_ON_GAME_MODE, GameStateManager.Instance.GetLastGameMode());
    }

    public void Enter_Dialogue_Mode() {
        if (GameStateManager.Instance.CurrentState == State.InDialogueMode) {
            UIManager.Instance.dialogueBoxUI.TopLevel = true;
            UIManager.Instance.playerChoicesBoxUI.TopLevel = true;
        }
    }

    public void Complete_Loading_Based_On_Game_Mode(State lastGameMode) {
        switch (lastGameMode) {
            case State.InDialogueMode:
                Initialize_Dialogue_Mode_Settings_On_Loaded_Game();
                GameStateManager.Instance.Fire(Trigger.LOADING_COMPLETED); //WE NEED TO ADD THIS ONE ON EVERY NEW CASE!!
                GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
                break;

            default:
                //if gamemode was not saved correctly, we try to load the last saved dialogue or player choices 
                lastGameMode = State.InDialogueMode;
                Initialize_Dialogue_Mode_Settings_On_Loaded_Game();
                GameStateManager.Instance.Fire(Trigger.LOADING_COMPLETED); //WE NEED TO ADD THIS ONE ON EVERY NEW CASE!!
                GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
                break;

        }
    }
    public void Initialize_Dialogue_Mode_Settings_On_Loaded_Game() {
        UIManager.Instance.inGameMenuButton.Show();
        //A BIT HACKY FIX where if the currentDialogObj is a PlayerChoice, and the user clicked on it and then saved the game, and if its DestinationDialogueID are PlayerChoices,
        //it means that they were all already saved in a List and displayed to the screen, so when loading that saved game it should not display that currentDialogObj, but only its associated playerChoices
        if (DialogueManager.Instance.playerChoicesList != null && DialogueManager.Instance.currentDialogueObject.Actor == "1") //if the current dialogue object it's a single player choice
        {
            //notice that we don't use DisplayDialogueOrPlayerChoice(DialogueObject dialogObj) to avoid displaying the already visited player choice that is still hold in the current dialogue object
            //until the player selects a new player choice. Notice that most times, after an NPC or actor dialogue, a group of player choices may be displayed, but it may also happen that after a 
            //player choice is displayed, more new player chocies are displayed. We are solving this rare case here. 
            DialogueManager.Instance.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, DialogueManager.Instance.SetIsPlayerChoiceBeingPrinted);
            VisualManager.Instance.DisplayVisual(VisualManager.Instance.VisualPath, VisualManager.Instance.visualType);
        } else
            DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);
    }

    public void Initialize_Save_Screen() {
        UIManager.saveGameScreen.SetUpSaveOrLoadScreen(UIManager.Instance.mainMenu.SAVE_SCREEN);
    }

    public async Task Display_Save_Screen() {
        await UIManager.Instance.saveGameScreen.DisplaySaveScreen();
    }

    public async Task Initialize_Load_Screen() {
        if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed)
            await UIManager.Instance.mainMenu.CloseMainMenu();
        else
            await UIManager.Instance.mainMenu.CloseInGameMenu();

        UIManager.saveGameScreen.SetUpSaveOrLoadScreen(UIManager.Instance.mainMenu.LOAD_SCREEN);
    }

    public async Task Display_Load_Screen() {
        await UIManager.Instance.saveGameScreen.DisplaySaveScreen();
    }


    public async Task Display_Language_Menu() {
        await UIManager.Instance.mainMenu.DisplayLanguageMenu();
    }

    public async Task Save_Game(bool isAutosave) {
        await LoadSaveManager.Instance.SaveGame(isAutosave);
    }


    public async Task ResumeGame() {
        await UIManager.Instance.mainMenu.CloseInGameMenu();
    }

    public async Task Exit_To_Main_Menu() {
        // CloseInGameMenu();
        UIManager.Instance.HideAllUIElements();
        VisualManager.Instance.RemoveImage();
        //HERE WE NEED TO HIDE ANYTHING THAT IS DISPLAYED ON SCREEN //HERE WE NEED TO HIDE ANYTHING THAT IS DISPLAYED ON SCREEN //HERE WE NEED TO HIDE ANYTHING THAT IS DISPLAYED ON SCREEN 
        await UIManager.Instance.mainMenu.DisplayMainMenu();
    }

    public void Exit_Game() {
        GetTree().Quit();
    }


    public void Autosave_Game() {
        //Save_Game(isAutoSave); This is not needed, as the game executes the autosave automatically every x time.
        UIManager.Instance.mainMenu.HideIngameMenuIcon();
        //GameStateManager.Instance.Fire(Trigger.AUTOSAVE_COMPLETED);
        // GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
    }

    public void Autosave_Completed() {
        UIManager.Instance.mainMenu.ShowIngameMenuIcon();
    }

    public void NotifyAutosaveCompleted() {
        //SHOW A MESSAGE AT THE BOTTOM RIGHT THAT THE AUTOSAVE WAS COMPLETED
    }


    // public void Display_Credits()
    // {

    // }

}
