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
    private State lastGameMode;

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
        return lastGameMode;
    }

    public void ResumeGameMode() {
        switch (lastGameMode) {
            case State.InDialogueMode:
                Fire(Trigger.RESUME_TO_DIALOGUE_MODE);
                break;
            // Add cases for other game modes as needed
            default:
                GD.Print($"Unknown game mode: {lastGameMode}");
                break;
        }
    }

    private void ConfigureStateTransitions() {
        stateTransitions = new Dictionary<(State, SubState, State, SubState, Trigger), Delegate>
        {
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
            //set up new game dialogues > enter Dialogue Mode
            {(State.SettingUpNewGameDialogues, SubState.None, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () => { }},

            //-----INGAME MENU-----//  

            //in dialogue mode > display ingame menu
            {(State.InDialogueMode, SubState.None, State.InGameMenuDisplayed, SubState.None, Trigger.DISPLAY_INGAME_MENU),
                () => GameManager.Instance.Display_Ingame_Menu()},
            //ingame menu > initialize save screen
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.SaveScreenInitialized, Trigger.INITIALIZE_SAVE_SCREEN),
                () => GameManager.Instance.Initialize_Save_Screen()},
            //initialize save screen > display save screen
            {(State.InGameMenuDisplayed, SubState.SaveScreenInitialized, State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.DISPLAY_SAVE_SCREEN),
                () => GameManager.Instance.Display_Save_Screen()},
            //display save screen > saving 
            {(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, State.InGameMenuDisplayed, SubState.Saving, Trigger.SAVE_GAME),
                new Action<bool>(isAutosave => GameManager.Instance.Save_Game(isAutosave))},
            //saving > saving completed
            {(State.InGameMenuDisplayed, SubState.Saving, State.InGameMenuDisplayed, SubState.SavingCompleted, Trigger.SAVING_COMPLETED),
                () => {}},
            //saving completed > display save screen (it's already displayed, we don't execute any method)
            {(State.InGameMenuDisplayed, SubState.SavingCompleted, State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.DISPLAY_SAVE_SCREEN),
                () => {}},
            //save screen > go back to ingame menu
            {(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},
            //ingame menu > dialogue mode
            {(State.InGameMenuDisplayed, SubState.None, State.InDialogueMode, SubState.None, Trigger.RESUME_TO_DIALOGUE_MODE),
                () => GameManager.Instance.Resume_To_Dialogue_Mode()},           
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
                new Action<State>(lastGameMode => GameManager.Instance.Complete_Loading_Based_On_Game_Mode(lastGameMode))},
            //complete loading based on game mode > loading completed
            {(State.InGameMenuDisplayed, SubState.CompleteLoadingBasedOnGameMode, State.InGameMenuDisplayed, SubState.LoadingCompleted, Trigger.LOADING_COMPLETED),
                () => {}},
            //loading completed > enter dialogue mode
            {(State.InGameMenuDisplayed, SubState.LoadingCompleted, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
                () =>{}},
            //in game menu > language menu
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU),
                () => GameManager.Instance.Display_Language_Menu()},
            //language menu > go back to ingame menu
            {(State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},
            //ingame menu > game credits
             {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.CreditsDisplayed, Trigger.DISPLAY_CREDITS),
                () => {}},
            //game credits > go back to ingame menu    
             {(State.InGameMenuDisplayed, SubState.CreditsDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},
            //ingame menu > exit to main menu
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed, Trigger.DISPLAY_EXIT_TO_MAIN_MENU_CONFIRMATION_POPUP),
                () => {}},

            {(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.DISPLAY_MAIN_MENU),
                () => GameManager.Instance.Display_Main_Menu()},
            
            {(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed, State.InGameMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                () => GameManager.Instance.Go_Back_To_Menu()},











            // Add more transitions as needed
        };

        // Configure transitions in the state machine
        foreach (var transition in stateTransitions.Keys) {
            stateMachine.ConfigureTransition(transition.Item1, transition.Item2, transition.Item3, transition.Item4, transition.Item5);
        }
    }


    private void OnStateChanged(State previousState, SubState previousSubstate, State newState, SubState newSubState, object[] arguments) {
        var transitionKey = (previousState, previousSubstate, newState, newSubState, stateMachine.LastTrigger);

        if (newState.ToString().Contains("Mode")) {
            lastGameMode = newState;
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

    private void HandleTransitionArguments(State newState, SubState newSubState, object[] arguments) {
        if (newState == State.InDialogueMode && newSubState == SubState.Loading && arguments.Length > 0) {
            if (arguments[0] is string filePath) {
                GameManager.Instance.Load_Game(filePath);
            }
        }
        // Add more argument handling logic as needed
    }

    public bool IsInState(State state) { return stateMachine.IsInState(state); }
    public State CurrentState => stateMachine.CurrentState;
}

// private void ConfigureTransitions() {
//   
//     // MainMenuDisplayed transitions
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.ENTER_DIALOGUE_MODE, State.InDialogueMode);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.START_NEW_GAME, State.StartingNewGame);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.INITIALIZE_LOAD_SCREEN, SubState.LoadScreenInitialized);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.DISPLAY_CREDITS, SubState.CreditsDisplayed);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU, SubState.LanguageMenuDisplayed);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.EXIT_GAME, State.ExitingGame);

//     //WE NEED TO ADD SETTINGS SCREEN

//     //In Dialogue Mode transitions
//     stateMachine.ConfigureTransition(State.InDialogueMode, Trigger.START_AUTOSAVE_GAME, SubState.AutoSaving);
//     stateMachine.ConfigureTransition(State.InDialogueMode, Trigger.DISPLAY_ENDING_SCREEN, State.EndingScreenDisplayed);

//     // Autosaving transitions
//     stateMachine.ConfigureTransition(SubState.AutoSaving, Trigger.RESUME_GAME_FROM_AUTOSAVE, State.InDialogueMode);

//     // InGameMenuDisplayed transitions
//     //stateMachine.ConfigureTransition(State.InGameMenuDisplayed, Trigger.RESUME_GAME, State.InDialogueMode);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, Trigger.INITIALIZE_SAVE_SCREEN, SubState.SaveScreenInitialized);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, Trigger.INITIALIZE_LOAD_SCREEN, SubState.LoadScreenInitialized);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, Trigger.EXIT_TO_MAIN_MENU, State.MainMenuDisplayed);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU, SubState.LanguageMenuDisplayed);
//     //WE NEED TO ADD SETTINGS SCREEN


//     // SaveScreen transitions
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.SaveScreenInitialized, Trigger.DISPLAY_SAVE_SCREEN, SubState.SaveScreenDisplayed);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.SAVE_GAME, SubState.Saving);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.Saving, Trigger.SAVING_COMPLETED, SubState.SavingCompleted);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.SavingCompleted, Trigger.DISPLAY_SAVE_SCREEN, SubState.SaveScreenDisplayed);
//     stateMachine.ConfigureTransition(SubState.SaveScreenDisplayed, Trigger.GO_BACK_TO_MENU, State.InGameMenuDisplayed);

//     // LoadScreenDisplayed transitions
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.LoadScreenInitialized, Trigger.DISPLAY_LOAD_SCREEN, SubState.LoadScreenDisplayed);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, SubState.LoadScreenInitialized, Trigger.DISPLAY_LOAD_SCREEN, SubState.LoadScreenDisplayed);

//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.LoadScreenDisplayed, Trigger.LOAD_GAME, SubState.Loading);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, SubState.LoadScreenDisplayed, Trigger.LOAD_GAME, SubState.Loading);
//     //stateMachine.ConfigureTransition(State.MainMenuDisplayed, SubState.LoadScreenDisplayed, Trigger.ENTER_LOADING_SUBSTATE, SubState.Loading);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.Loading, Trigger.ENTER_DIALOGUE_MODE, State.InDialogueMode);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, SubState.Loading, Trigger.ENTER_DIALOGUE_MODE, State.InDialogueMode);
//     // stateMachine.ConfigureTransition(State.MainMenuDisplayed, SubState.Loading, Trigger.START_NEW_GAME, State.StartingNewGame);

//     stateMachine.ConfigureTransition(SubState.LoadScreenDisplayed, Trigger.GO_BACK_TO_MENU, State.MainMenuDisplayed);
//     stateMachine.ConfigureTransition(SubState.LoadScreenDisplayed, Trigger.GO_BACK_TO_MENU, State.InGameMenuDisplayed);

//     // Loading transitions
//     //stateMachine.ConfigureTransition(State.MainMenuDisplayed, SubState.Loading, Trigger.START_NEW_GAME, State.SettinUpNewGame);


//     // Saving transitions
//     //stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.Saving, Trigger.BACK_TO_SAVE_SCREEN_FROM_SAVE_COMPLETED, SubState.SaveScreenDisplayed);
//     // Credits transitions
//     stateMachine.ConfigureTransition(SubState.CreditsDisplayed, Trigger.HIDE_CREDITS, State.MainMenuDisplayed);

//     // LanguageSelection transitions
//     //stateMachine.ConfigureTransition(SubState.LanguageSelectionDisplayed, Trigger.ChangeLanguage, State.LanguageChanged); //SHOULD WE HAVE A 'LanguageChanged' state?
//     stateMachine.ConfigureTransition(SubState.LanguageMenuDisplayed, Trigger.GO_BACK_TO_MENU, State.MainMenuDisplayed);
//     stateMachine.ConfigureTransition(SubState.LanguageMenuDisplayed, Trigger.GO_BACK_TO_MENU, State.InGameMenuDisplayed);
// }

//You can add methods for other actions that need multiple arguments 
//EXAMPLE: public void SOME_OTHER_ACTION(string arg1, int arg2, bool arg3) {stateMachine.Fire(Trigger.SOME_OTHER_TRIGGER, arg1, arg2, arg3);}

//    private void Fire(Trigger trigger, params object[] arguments) {
//         stateMachine.Fire(trigger, arguments);
//     }


// public enum Trigger {
//     DISPLAY_SPLASH_SCREEN  - DONE
//     DISPLAY_MAIN_MENU,     - DONE
//     DISPLAY_INGAME_MENU,   - DONE
//     START_NEW_GAME,        - DONE
//     DISPLAY_ENTER_YOUR_NAME_SCREEN,  -DONE
//     DISPLAY_NEW_GAME_DIALOGUES       -DONE
//     ENTER_DIALOGUE_MODE              -DONE
//     DISPLAY_LOAD_SCREEN,             -DONE
//     LOAD_GAME,                       -DONE                   
//     ENTER_LOADING_SUBSTATE,          -DONE
//     DISPLAY_SAVE_SCREEN,             -DONE            
//     SAVE_GAME,                       -DONE
//     BACK_TO_SAVE_SCREEN_FROM_SAVE_COMPLETED, //NOT IMPLEMENTED, WE DON'T NEED IT 
//     START_AUTOSAVE_GAME,             -DONE
//     RESUME_GAME_FROM_AUTOSAVE,                                                       //NOT IMPLEMENTED, WE DON'T NEED IT , JUST USING ENTER_DIALOGUE_MODE()
//     RESUME_GAME,                                                                     //NOT IMPLEMENTED, WE DON'T NEED IT , JUST USING ENTER_DIALOGUE_MODE()
//     GO_BACK_TO_MENU, //from any submenu inside main OR ingame menu
//     EXIT_TO_MAIN_MENU, //froM the confirmation popup in INGAME MENU. We must warn user he can lose progress
//     DISPLAY_CREDITS,
//     HIDE_CREDITS,
//     DISPLAY_LANGUAGE_MENU, //ShowLanguageSelection,
//     DISPLAY_ENDING_SCREEN, //It can be a lost game, a victory, partial victory, neutral ending, etc.
//     EXIT_GAME
// }




