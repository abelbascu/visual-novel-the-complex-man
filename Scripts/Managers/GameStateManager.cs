using Godot;
using System;
using System.Collections.Generic;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;
using Trigger = GameStateMachine.Trigger;
using static GameStateMachine;
using System.Linq.Expressions;

public partial class GameStateManager : Node {


    // public enum GameMode {
    //     None,
    //     Dialogue,
    //     Minigame,
    //     Cutscene,
    // }

    public static GameStateManager Instance { get; private set; }
    private GameStateMachine stateMachine;
    //public event Action<State, SubState, State, SubState, object[]> OnStateChanged;
    private Dictionary<(State, SubState, State, SubState, Trigger), Delegate> stateTransitions;
    public State LastGameMode {get; private set;}

    public override void _EnterTree() {
        if (Instance == null) {
            Instance = this;
        } else {
            QueueFree();
        }
    }

    public override void _Ready() {
        stateMachine = new GameStateMachine();
        ConfigureStateTransitions();
        //ConfigureStateTransitionActions();
        stateMachine.StateChanged += OnStateChanged;
    }

    public void Fire(Trigger trigger, params object[] args) {
        stateMachine.Fire(trigger, args);
    }

    public State GetLastGameMode() {
        return LastGameMode;
    }

    public void ResumeGameMode() {
        switch (LastGameMode) {
            case State.InDialogueMode:
                Fire(Trigger.ENTER_DIALOGUE_MODE);
                break;
            // Add cases for other game modes as needed
            default:
                GD.Print($"Unknown game mode: {LastGameMode}");
                break;
        }
    }

    private void ConfigureStateTransitions() {
        stateTransitions = new Dictionary<(State, SubState, State, SubState, Trigger), Delegate>
        {
            //--------------------------------------------------------------------------------------------------------------------------------------//  
            //------------------------------------------------------GLOBAL GAME STATE CHART---------------------------------------------------------// 
            //--------------------------------------------------------------------------------------------------------------------------------------//   

            //-----FROM GAME INTRO TO DIALOGUE MODE-----//  
          
            //splashscreen > splashscreen
            {(State.None, SubState.None, State.SplashScreenDisplayed, SubState.None, Trigger.DISPLAY_SPLASH_SCREEN),
                () => GameManager.Instance.Display_Splash_Screen()},
            // splashscreen > main Menu
            {(State.SplashScreenDisplayed, SubState.None, State.MainMenuDisplayed, SubState.None, Trigger.DISPLAY_MAIN_MENU),
                () => GameManager.Instance.Display_Main_Menu()},
            //main Menu > start new game
            {(State.MainMenuDisplayed, SubState.None, State.StartingNewGame, SubState.None, Trigger.START_NEW_GAME),
                () => GameManager.Instance.Starting_New_Game()},
            //start New Game > enter your name screen
            {(State.StartingNewGame, SubState.None, State.EnterYourNameScreenDisplayed, SubState.None, Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN),
                () => GameManager.Instance.Display_Enter_Your_Name_Screen()},
            //enter your name screen > set up new game dialogues 
            {(State.EnterYourNameScreenDisplayed, SubState.None, State.SettingUpNewGameDialogues, SubState.None, Trigger.DISPLAY_NEW_GAME_DIALOGUES),
                () => GameManager.Instance.Display_New_Game_Dialogues()},
            //set up new game dialogues > enter dialogue node
            {(State.SettingUpNewGameDialogues, SubState.None, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () => GameManager.Instance.Enter_Dialogue_Mode()},

            //-----INGAME MENU-----//  

            //in dialogue mode > display ingame menu
            {(State.InDialogueMode, SubState.None, State.InGameMenuDisplayed, SubState.None, Trigger.DISPLAY_INGAME_MENU),
                () =>{}},
            //in game menu > enter dialogue node
            {(State.InGameMenuDisplayed, SubState.None, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () => GameManager.Instance.Enter_Dialogue_Mode()},
            //ingame menu > initialize save screen
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.SaveScreenInitialized, Trigger.INITIALIZE_SAVE_SCREEN),
                () => GameManager.Instance.Initialize_Save_Screen()},
            //initialize save screen > display save screen
            {(State.InGameMenuDisplayed, SubState.SaveScreenInitialized, State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.DISPLAY_SAVE_SCREEN),
                () => GameManager.Instance.Display_Save_Screen()},
            //display save screen > saving 
            {(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, State.InGameMenuDisplayed, SubState.Saving, Trigger.SAVE_GAME),
                new Action<bool>(isAutosave => GameManager.Instance.Save_Game())},
            //saving > saving completed
            {(State.InGameMenuDisplayed, SubState.Saving, State.InGameMenuDisplayed, SubState.SavingCompleted, Trigger.SAVING_COMPLETED),
                () => UIManager.Instance.saveGameScreen.EnableInputAfterSavingComplete()},  
            //saving completed > display save screen (it's already displayed, we don't execute any method)
            {(State.InGameMenuDisplayed, SubState.SavingCompleted, State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.DISPLAY_SAVE_SCREEN),
                () => {}},
            //save screen > go back to ingame menu
            {(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},        
            //ingame menu > initialize load screen
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.LoadScreenInitialized, Trigger.INITIALIZE_LOAD_SCREEN),
                () => GameManager.Instance.Initialize_Load_Screen()},
            //initialize load screen > display load screen
             {(State.InGameMenuDisplayed, SubState.LoadScreenInitialized, State.InGameMenuDisplayed, SubState.LoadScreenDisplayed, Trigger.DISPLAY_LOAD_SCREEN),
                () => GameManager.Instance.Display_Load_Screen()},
            //load screen > go back to ingame menu    
             {(State.InGameMenuDisplayed, SubState.LoadScreenDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},
             //load Screen > loading
            {(State.InGameMenuDisplayed, SubState.LoadScreenDisplayed, State.InGameMenuDisplayed, SubState.Loading, Trigger.LOAD_GAME),
                new Action<string>(filePath => GameManager.Instance.Load_Game(filePath))},
            //loading > complete loading base on game mode 
            {(State.InGameMenuDisplayed, SubState.Loading, State.InGameMenuDisplayed, SubState.CompleteLoadingBasedOnGameMode, Trigger.COMPLETE_LOADING_BASED_ON_GAME_MODE),
                new Action<State>(LastGameMode => GameManager.Instance.Complete_Loading_Based_On_Game_Mode(LastGameMode))},
            //complete loading based on game mode > loading completed
            {(State.InGameMenuDisplayed, SubState.CompleteLoadingBasedOnGameMode, State.InGameMenuDisplayed, SubState.LoadingCompleted, Trigger.LOADING_COMPLETED),
                () => {}},
            //loading completed > enter dialogue mode
            {(State.InGameMenuDisplayed, SubState.LoadingCompleted, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () =>GameManager.Instance.Enter_Dialogue_Mode()},
            //in game menu > language menu
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU),
                () => GameManager.Instance.Display_Language_Menu()},
            //language menu > go back to ingame menu
            {(State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => {}},
            //ingame menu > game credits
             {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.CreditsDisplayed, Trigger.DISPLAY_CREDITS),
                () => {}},
            //game credits > go back to ingame menu    
             {(State.InGameMenuDisplayed, SubState.CreditsDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => {}},
            //ingame menu > exit to main menu confirmation popup
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed, Trigger.DISPLAY_EXIT_TO_MAIN_MENU_CONFIRMATION_POPUP),
                () => {}},
            //exit to main menu confirmation popup > display main menu
            {(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.DISPLAY_MAIN_MENU),
                () => GameManager.Instance.Display_Main_Menu()},
            //exit to main menu confirmation popup > back to ingame menu
            {(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},

            //-----AUTOSAVE-----//  

            // dialogue mode => autosaving
            {(State.InDialogueMode, SubState.None, State.InDialogueMode, SubState.AutoSaving, Trigger.AUTOSAVE_GAME),
                () => GameManager.Instance.Autosave_Game()},
            //autoaaving > autosave complete
            {(State.InDialogueMode, SubState.AutoSaving, State.InDialogueMode, SubState.AutoSavingCompleted, Trigger.AUTOSAVE_COMPLETED),
                () => GameManager.Instance.Autosave_Completed()},
            //autosave complete > dialogue mode
            {(State.InDialogueMode, SubState.AutoSavingCompleted, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () => GameManager.Instance.Enter_Dialogue_Mode()},

            //-----MAIN MENU-----//  
             
            //main menu > initialize load screen
            {(State.MainMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.LoadScreenInitialized, Trigger.INITIALIZE_LOAD_SCREEN),
                () => GameManager.Instance.Initialize_Load_Screen()},
            //initialize load screen > display load screen
             {(State.MainMenuDisplayed, SubState.LoadScreenInitialized, State.MainMenuDisplayed, SubState.LoadScreenDisplayed, Trigger.DISPLAY_LOAD_SCREEN),
                () => GameManager.Instance.Display_Load_Screen()},
            //load screen > go back to main menu    
             {(State.MainMenuDisplayed, SubState.LoadScreenDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},
             //load Screen > loading
            {(State.MainMenuDisplayed, SubState.LoadScreenDisplayed, State.MainMenuDisplayed, SubState.Loading, Trigger.LOAD_GAME),
                new Action<string>(filePath => GameManager.Instance.Load_Game(filePath))},
            //loading > complete loading base on game mode 
            {(State.MainMenuDisplayed, SubState.Loading, State.MainMenuDisplayed, SubState.CompleteLoadingBasedOnGameMode, Trigger.COMPLETE_LOADING_BASED_ON_GAME_MODE),
                new Action<State>(LastGameMode => GameManager.Instance.Complete_Loading_Based_On_Game_Mode(LastGameMode))},
            //complete loading based on game mode > loading completed
            {(State.MainMenuDisplayed, SubState.CompleteLoadingBasedOnGameMode, State.MainMenuDisplayed, SubState.LoadingCompleted, Trigger.LOADING_COMPLETED),
                () => {}},
            //loading completed > enter dialogue mode
            {(State.MainMenuDisplayed, SubState.LoadingCompleted, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () =>GameManager.Instance.Enter_Dialogue_Mode()},
            //main menu > language menu
            {(State.MainMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.LanguageMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU),
                () => GameManager.Instance.Display_Language_Menu()},
            //language menu > go back to main menu
            {(State.MainMenuDisplayed, SubState.LanguageMenuDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => {}},
            //main menu > game credits
             {(State.MainMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.CreditsDisplayed, Trigger.DISPLAY_CREDITS),
                () => {}},
            //game credits > go back to main menu    
             {(State.MainMenuDisplayed, SubState.CreditsDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => {}},
            //main menu > exit game confirmation popup
            {(State.MainMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed, Trigger.DISPLAY_EXIT_GAME_MENU_CONFIRMATION_POPUP),
                () => {}},
            //exit game confirmation popup > exit game
            {(State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed, State.MainMenuDisplayed, SubState.ExitingGame, Trigger.EXIT_GAME),
                () => GameManager.Instance.Exit_Game()},
            //exit game confirmation popup > back to ingame menu
            {(State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => {}}

        };

        // Configure transitions in the state machine
        foreach (var transition in stateTransitions.Keys) {
            stateMachine.ConfigureTransition(transition.Item1, transition.Item2, transition.Item3, transition.Item4, transition.Item5);
        }
    }

    private void OnStateChanged(State previousState, SubState previousSubstate, State newState, SubState newSubState, object[] arguments) {
        var transitionKey = (previousState, previousSubstate, newState, newSubState, stateMachine.LastTrigger);

        if (newState.ToString().Contains("Mode")) {
            LastGameMode = newState;
        }

        if (stateTransitions.TryGetValue(transitionKey, out var actions)) {
            actions.DynamicInvoke(arguments);
        } else {
            // Handle default transitions or log unhandled transitions
            GD.Print($"Unhandled transition: {previousState}.{previousSubstate} -> {newState}.{newSubState}, Trigger: {stateMachine.LastTrigger}");

            // Fall back to default state handlers if needed
            HandleDefaultStateTransition(newState, newSubState);
        }
    }

    private void HandleDefaultStateTransition(State newState, SubState newSubState) {
        switch (newState) {
            case State.SplashScreenDisplayed:
                GameManager.Instance.Display_Splash_Screen();
                break;
            case State.MainMenuDisplayed:
                GameManager.Instance.Display_Main_Menu();
                break;
            case State.StartingNewGame:
                GameManager.Instance.Starting_New_Game();
                break;
            case State.EnterYourNameScreenDisplayed:
                GameManager.Instance.Display_Enter_Your_Name_Screen();
                break;
            case State.EndingScreenDisplayed:
                // Implement ending screen logic
                break;
            case State.ExitingGame:
                // Implement game exit logic
                break;
        }
    }

    public bool IsInState(State state, SubState substate) {
        return stateMachine.IsInState(state, substate);
    }
    public State CurrentState => stateMachine.CurrentState;
    public SubState CurrentSubstate => stateMachine.CurrentSubState;
    public State PreviousState => stateMachine.previousState;
    public SubState PreviousSubstate => stateMachine.previousSubState;
}


