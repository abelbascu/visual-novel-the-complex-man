using Godot;
using System;
using System.Collections.Generic;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;
using Trigger = GameStateMachine.Trigger;
using static GameStateMachine;


public partial class GameStateManager : Node {

    public static GameStateManager Instance { get; private set; }
    private GameStateMachine stateMachine;
    //public event Action<State, SubState, State, SubState, object[]> OnStateChanged;
    private Dictionary<(State, SubState, State, SubState, Trigger), (Action action, Action<object[]> argsAction)> stateTransitions;


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


    private void ConfigureStateTransitions() {
        stateTransitions = new Dictionary<(State, SubState, State, SubState, Trigger), (Action, Action<object[]>)>
        {
            //SpalashScreen
             {(State.SplashScreenDisplayed, SubState.None, State.SplashScreenDisplayed, SubState.None, Trigger.DISPLAY_SPLASH_SCREEN),
                (() => GameManager.Instance.Display_Splash_Screen(), null)},
            // MainMenu transitions
            {(State.SplashScreenDisplayed, SubState.None, State.MainMenuDisplayed, SubState.None, Trigger.DISPLAY_MAIN_MENU),
                (() => GameManager.Instance.Display_Main_Menu(), null)},

            {(State.MainMenuDisplayed, SubState.None, State.StartingNewGame, SubState.None, Trigger.START_NEW_GAME),
                (() => GameManager.Instance.Starting_New_Game(), null)},

             {(State.StartingNewGame, SubState.None, State.EnterYourNameScreenDisplayed, SubState.None, Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN),
                (() => GameManager.Instance.Display_Enter_Your_Name_Screen(), null)},


             {(State.EnterYourNameScreenDisplayed, SubState.None, State.SettingUpNewGameDialogues, SubState.None, Trigger.DISPLAY_NEW_GAME_DIALOGUES),
                (() => GameManager.Instance.Display_New_Game_Dialogues(), null)},

               {(State.SettingUpNewGameDialogues, SubState.None, State.InDialogueMode, SubState.None, Trigger.ENTER_DIALOGUE_MODE),
    (() => { }, null)},


            {(State.MainMenuDisplayed, SubState.LanguageMenuDisplayed, State.MainMenuDisplayed, SubState.None, Trigger.GO_BACK_TO_MENU),
                (() => GameManager.Instance.Go_Back_To_Menu(), null)},


            {(State.InGameMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.None, Trigger.EXIT_TO_MAIN_MENU),
                (() => GameManager.Instance.Display_Main_Menu(), null)},
            
            // InGameMenu transitions
            {(State.InDialogueMode, SubState.None, State.InGameMenuDisplayed, SubState.None, Trigger.DISPLAY_INGAME_MENU),
                (() => GameManager.Instance.Display_Ingame_Menu(), null)},
            
            // Dialogue Mode transitions
            // {(State.SettingUpNewGameDialogues, SubState.None, State.InDialogueMode, SubState.None, Trigger.DISPLAY_NEW_GAME_DIALOGUES),
            //     (() => GameManager.Instance.Display_New_Game_Dialogues(), null)},
            
            // Load Screen transitions
            {(State.MainMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.LoadScreenInitialized, Trigger.INITIALIZE_LOAD_SCREEN),
                (() => GameManager.Instance.Initialize_Load_Screen(), null)},

            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.LoadScreenInitialized, Trigger.INITIALIZE_LOAD_SCREEN),
                (() => GameManager.Instance.Initialize_Load_Screen(), null)},
            
            // Save Screen transitions
            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.SaveScreenInitialized, Trigger.INITIALIZE_SAVE_SCREEN),
                (() => GameManager.Instance.Initialize_Save_Screen(), null)},

            {(State.InGameMenuDisplayed, SubState.SaveScreenInitialized, State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.DISPLAY_SAVE_SCREEN),
                 (() => { }, null)},



//     // SaveScreen transitions
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.SaveScreenInitialized, Trigger.DISPLAY_SAVE_SCREEN, SubState.SaveScreenDisplayed);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed, Trigger.SAVE_GAME, SubState.Saving);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.Saving, Trigger.SAVING_COMPLETED, SubState.SavingCompleted);
//     stateMachine.ConfigureTransition(State.InGameMenuDisplayed, SubState.SavingCompleted, Trigger.DISPLAY_SAVE_SCREEN, SubState.SaveScreenDisplayed);
//     stateMachine.ConfigureTransition(SubState.SaveScreenDisplayed, Trigger.GO_BACK_TO_MENU, State.InGameMenuDisplayed);



                //  {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.SaveScreenInitialized, Trigger.INITIALIZE_SAVE_SCREEN),
                // (() => GameManager.Instance.Initialize_Save_Screen(), null)},
            
            // Language Menu transitions
            {(State.MainMenuDisplayed, SubState.None, State.MainMenuDisplayed, SubState.LanguageMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU),
                (() => GameManager.Instance.Display_Language_Menu(), null)},



            {(State.InGameMenuDisplayed, SubState.None, State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU),
                (() => GameManager.Instance.Display_Language_Menu(), null)},
            
            // Load Game transition
            {(State.MainMenuDisplayed, SubState.LoadScreenDisplayed, State.InDialogueMode, SubState.Loading, Trigger.LOAD_GAME),
                (null, (args) => { if (args.Length > 0 && args[0] is string filePath) GameManager.Instance.Load_Game(filePath); })},

            // Add more transitions as needed
        };

        // Configure transitions in the state machine
        foreach (var transition in stateTransitions.Keys) {
            stateMachine.ConfigureTransition(transition.Item1, transition.Item2, transition.Item3, transition.Item4, transition.Item5);
        }
    }


    private void OnStateChanged(State previousState, SubState previousSubstate, State newState, SubState newSubState, object[] arguments) {
        var transitionKey = (previousState, previousSubstate, newState, newSubState, stateMachine.LastTrigger);

        if (stateTransitions.TryGetValue(transitionKey, out var actions)) {
            actions.action?.Invoke();
            actions.argsAction?.Invoke(arguments);
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
//     // Splash Screen transitions
//     stateMachine.ConfigureTransition(State.SplashScreenDisplayed, Trigger.DISPLAY_SPLASH_SCREEN, State.SplashScreenDisplayed);
//     stateMachine.ConfigureTransition(State.SplashScreenDisplayed, Trigger.DISPLAY_MAIN_MENU, State.MainMenuDisplayed);

//     // MainMenuDisplayed transitions
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.ENTER_DIALOGUE_MODE, State.InDialogueMode);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.START_NEW_GAME, State.StartingNewGame);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.INITIALIZE_LOAD_SCREEN, SubState.LoadScreenInitialized);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.DISPLAY_CREDITS, SubState.CreditsDisplayed);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.DISPLAY_LANGUAGE_MENU, SubState.LanguageMenuDisplayed);
//     stateMachine.ConfigureTransition(State.MainMenuDisplayed, Trigger.EXIT_GAME, State.ExitingGame);


//     //WE NEED TO ADD SETTINGS SCREEN

//     //Setting up new game transitions
//     stateMachine.ConfigureTransition(State.StartingNewGame, Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN, State.EnterYourNameScreenDisplayed);

//     //Enter your name screen transitions
//     stateMachine.ConfigureTransition(State.EnterYourNameScreenDisplayed, Trigger.DISPLAY_NEW_GAME_DIALOGUES, State.SettingUpNewGameDialogues);

//     //Seeting up New Game Dialogues transitions
//     stateMachine.ConfigureTransition(State.SettingUpNewGameDialogues, Trigger.ENTER_DIALOGUE_MODE, State.InDialogueMode);

//     //In Dialogue Mode transitions
//     stateMachine.ConfigureTransition(State.InDialogueMode, Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN, State.EnterYourNameScreenDisplayed);
//     stateMachine.ConfigureTransition(State.InDialogueMode, Trigger.DISPLAY_INGAME_MENU, State.InGameMenuDisplayed);
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

// public void DISPLAY_SPLASH_SCREEN() { stateMachine.Fire(Trigger.DISPLAY_SPLASH_SCREEN); }
// public void DISPLAY_MAIN_MENU() { stateMachine.Fire(Trigger.DISPLAY_MAIN_MENU); }
// public void DISPLAY_INGAME_MENU() { stateMachine.Fire(Trigger.DISPLAY_INGAME_MENU); }
// public void START_NEW_GAME() { stateMachine.Fire(Trigger.START_NEW_GAME); }
// public void DISPLAY_ENTER_YOUR_NAME_SCREEN() { stateMachine.Fire(Trigger.DISPLAY_ENTER_YOUR_NAME_SCREEN); }
// public void DISPLAY_NEW_GAME_DIALOGUES() { stateMachine.Fire(Trigger.DISPLAY_NEW_GAME_DIALOGUES); }
// public void ENTER_DIALOGUE_MODE() { stateMachine.Fire(Trigger.ENTER_DIALOGUE_MODE); }
// public void INITIALIZE_LOAD_SCREEN() { stateMachine.Fire(Trigger.INITIALIZE_LOAD_SCREEN); }
// public void DISPLAY_LOAD_SCREEN() { stateMachine.Fire(Trigger.DISPLAY_LOAD_SCREEN); }
// public void LOAD_GAME(string filePath) { stateMachine.Fire(Trigger.LOAD_GAME, filePath); }
// public void ENTER_LOADING_SUBSTATE() { stateMachine.Fire(Trigger.ENTER_LOADING_SUBSTATE); }
// public void INITIALIZE_SAVE_SCREEN() { stateMachine.Fire(Trigger.INITIALIZE_SAVE_SCREEN); }
// public void DISPLAY_SAVE_SCREEN() { stateMachine.Fire(Trigger.DISPLAY_SAVE_SCREEN); }
// public void SAVE_GAME(bool isAutosave) { stateMachine.Fire(Trigger.SAVE_GAME, isAutosave); }
// public void SAVING_COMPLETED() { stateMachine.Fire(Trigger.SAVING_COMPLETED); }

// //public void BACK_TO_SAVE_SCREEN_FROM_SAVE_COMPLETED() { stateMachine.Fire(Trigger.BACK_TO_SAVE_SCREEN_FROM_SAVE_COMPLETED); }
// public void START_AUTOSAVE_GAME() { stateMachine.Fire(Trigger.START_AUTOSAVE_GAME); }
// public void RESUME_GAME_FROM_AUTOSAVE() { stateMachine.Fire(Trigger.RESUME_GAME_FROM_AUTOSAVE); }
// //public void RESUME_GAME() { stateMachine.Fire(Trigger.RESUME_GAME); }
// public void GO_BACK_TO_MENU() { stateMachine.Fire(Trigger.GO_BACK_TO_MENU); }
// public void EXIT_TO_MAIN_MENU() { stateMachine.Fire(Trigger.EXIT_TO_MAIN_MENU); }
// public void DISPLAY_CREDITS() { stateMachine.Fire(Trigger.DISPLAY_CREDITS); }
// public void HIDE_CREDITS() { stateMachine.Fire(Trigger.HIDE_CREDITS); }
// public void DISPLAY_LANGUAGE_MENU() { stateMachine.Fire(Trigger.DISPLAY_LANGUAGE_MENU); }
// public void DISPLAY_ENDING_SCREEN() { stateMachine.Fire(Trigger.DISPLAY_ENDING_SCREEN); }
// public void EXIT_GAME() { stateMachine.Fire(Trigger.EXIT_GAME); }




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




