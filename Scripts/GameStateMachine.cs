using System;
using System.Collections.Generic;
using Godot;

public class GameStateMachine {

    public enum State {
        None,
        SplashScreenDisplayed,
        MainMenuDisplayed,
        InGameMenuDisplayed,
        StartingNewGame,
        SettingUpNewGameDialogues,
        InDialogueMode,
        EnterYourNameScreenDisplayed,
        EndingScreenDisplayed,
        ExitingGame
    }

    public enum SubState {
        None,
        LoadScreenInitialized,
        LoadScreenDisplayed,
        Loading,
        CompleteLoadingBasedOnGameMode,
        LoadingCompleted,
        SaveScreenInitialized,
        SaveScreenDisplayed,
        Saving,
        SavingCompleted,
        AutoSaving,
        AutoSavingCompleted,
        CreditsDisplayed,
        LanguageMenuDisplayed,
        ExitToMainMenuConfirmationPopupDisplayed,
        ExitGameConfirmationPopupDisplayed,
        ExitingGame
    }

    public enum Trigger {
        DISPLAY_SPLASH_SCREEN,
        DISPLAY_MAIN_MENU,
        DISPLAY_INGAME_MENU,
        START_NEW_GAME,
        DISPLAY_ENTER_YOUR_NAME_SCREEN,
        DISPLAY_NEW_GAME_DIALOGUES,
        ENTER_DIALOGUE_MODE,
        INITIALIZE_LOAD_SCREEN,
        DISPLAY_LOAD_SCREEN,
        LOAD_GAME,
        COMPLETE_LOADING_BASED_ON_GAME_MODE,
        LOADING_COMPLETED,
        ENTER_LOADING_SUBSTATE,
        INITIALIZE_SAVE_SCREEN,
        DISPLAY_SAVE_SCREEN,
        SAVE_GAME,
        SAVING_COMPLETED,
        AUTOSAVE_GAME,
        AUTOSAVE_COMPLETED,
        RESUME_TO_DIALOGUE_MODE,
        RESUME_GAME_FROM_AUTOSAVE,
        GO_BACK_TO_MENU, //from any submenu inside main OR ingame menu
        DISPLAY_EXIT_TO_MAIN_MENU_CONFIRMATION_POPUP,
        EXIT_TO_MAIN_MENU, //froM the confirmation popup in INGAME MENU. We must warn user he can lose progress
        DISPLAY_CREDITS,
        HIDE_CREDITS,
        DISPLAY_LANGUAGE_MENU,
        DISPLAY_ENDING_SCREEN, //It can be a lost game, a victory, partial victory, neutral ending, etc.
        DISPLAY_EXIT_GAME_MENU_CONFIRMATION_POPUP,
        EXIT_GAME
    }

    private State currentState;
    private SubState currentSubState;
    private Dictionary<(State, SubState), Dictionary<Trigger, (State, SubState)>> transitions;
    private object[] _transitionArguments;
    public event Action<State, SubState, State, SubState, object[]> StateChanged;
    public Trigger LastTrigger { get; private set; }
    public State previousState;
    public SubState previousSubState;

    public GameStateMachine() {
        transitions = new Dictionary<(State, SubState), Dictionary<Trigger, (State, SubState)>>();
        currentSubState = SubState.None;
    }

    // public void ConfigureTransition(State fromState, Trigger trigger, State toState) {
    //     ConfigureTransition(fromState, SubState.None, trigger, toState, SubState.None);
    // }

    // // Overload 2: Transitions from a substate back to its parent state
    // public void ConfigureTransition(SubState fromSubState, Trigger trigger, State state) {
    //     ConfigureTransition(state, fromSubState, trigger, state, SubState.None);
    // }

    // // Overload 3: Transitions from a state to its substate
    // public void ConfigureTransition(State state, Trigger trigger, SubState toSubState) {
    //     ConfigureTransition(state, SubState.None, trigger, state, toSubState);
    // }

    // // Overload 4: Transitions between substates within the same state
    // public void ConfigureTransition(State state, SubState fromSubState, Trigger trigger, SubState toSubState) {
    //     ConfigureTransition(state, fromSubState, trigger, state, toSubState);
    // }

    // // Overload 5: Transitions from a state-substate to another state without substate
    // public void ConfigureTransition(State fromState, SubState fromSubState, Trigger trigger, State toState) {
    //     ConfigureTransition(fromState, fromSubState, trigger, toState, SubState.None);
    // }

    // Overload 6: Full control over substates
    public void ConfigureTransition(State fromState, SubState fromSubState, State toState, SubState toSubState, Trigger trigger) {
        var fromKey = (fromState, fromSubState);
        if (!transitions.ContainsKey(fromKey)) {
            transitions[fromKey] = new Dictionary<Trigger, (State, SubState)>();
        }
        transitions[fromKey][trigger] = (toState, toSubState);
    }

    public void Fire(Trigger trigger, params object[] arguments) {

        var currentKey = (currentState, currentSubState);
        if (!transitions.ContainsKey(currentKey) || !transitions[currentKey].ContainsKey(trigger)) {
            throw new InvalidOperationException($"No valid transition from {currentState}.{currentSubState} with trigger {trigger}");
        }

        LastTrigger = trigger;
        _transitionArguments = arguments;
        previousState = currentState;
        previousSubState = currentSubState;
        (currentState, currentSubState) = transitions[currentKey][trigger];
        GD.Print($"State changed from {previousState} ({previousSubState}) to {currentState} ({currentSubState}), , Trigger: {trigger}");
        StateChanged?.Invoke(previousState, previousSubState, currentState, currentSubState, arguments);
    }

    public T GetArgument<T>(int index) {
        if (_transitionArguments == null | index < 0 || index >= _transitionArguments.Length || !(_transitionArguments is T)) {
            throw new InvalidOperationException($"No argument of type {typeof(T)} available");
        }
        return (T)_transitionArguments[index];
    }

    public bool IsInState(State state, SubState subState = SubState.None) {
        return currentState == state && currentSubState == subState;
    }


    public State CurrentState => currentState;
    public SubState CurrentSubState => currentSubState;
    public State PreviousState => previousState;
}