using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;
using System.Threading.Tasks;

public partial class InputManager : Control {

    public static InputManager Instance { get; private set; }
    private Control currentFocusedMenu;
    private int currentFocusedIndex = -1;
    private List<Control> focusableControls = new List<Control>();
    private int currentPlayerChoiceIndex = -1;

    private float lastInputTime = 0f;
    private const float INPUT_DELAY = 0.25f;
    private const float STICK_THRESHOLD = 0.3f;

    private const float DEADZONE = 0.2f;
    private bool isStickInDeadzone = true;
    private float lastJoypadXValue = 0f;
    private float lastJoypadYValue = 0f;
    private const float INPUT_COOLDOWN = 0.2f; // 200ms cooldown


    private bool isGamePadAndKeyboardInputEnabled = true;
    private bool isInputLocked = false;

    public void SetGamePadAndKeyboardInputEnabled(bool enabled) {
        isGamePadAndKeyboardInputEnabled = enabled;
    }

    public void UnlockInput() {
        isInputLocked = false;
    }

    public override void _EnterTree() {
        if (Instance == null) {
            Instance = this;
        } else {
            QueueFree();
        }
    }

    public override void _ExitTree() {
        GameStateManager.Instance.StateChanged -= OnGameStateChanged;
    }

    public override void _Ready() {
        GameStateManager.Instance.StateChanged += OnGameStateChanged;
    }


    public override void _Input(InputEvent @event) {
        // float currentTime = (float)Time.GetTicksMsec() / 1000.0f;
        // if (currentTime - lastInputTime < INPUT_DELAY) return;

        if (!isGamePadAndKeyboardInputEnabled || isInputLocked) return;

        isInputLocked = true; //QUE HAGO CON ESTO???


        if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.None) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.None)
            || GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed)
            || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed)
            || GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.LanguageMenuDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed))
            HandleMenuInput(@event);
        else if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None))
            HandleDialogueInput(@event);
        else if (GameStateManager.Instance.IsInState(State.SplashScreenDisplayed, SubState.None)) {
            HandleSplashScreenInput(@event);
        } else if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.LoadScreenDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.LoadScreenDisplayed))
            HandleLoadScreeenInput(@event);
        else if (GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed))
            HandleSaveScreenInput(@event);

        // lastInputTime = currentTime;
        isInputLocked = false;
    }

    private async Task HandleSaveScreenInput(InputEvent @event) {
        if (@event.IsActionPressed("ui_cancel")) {
            await UIManager.Instance.saveGameScreen.OnGoBackButtonPressed();
        }
    }

    private async Task HandleLoadScreeenInput(InputEvent @event) {
        // if (@event.IsActionPressed("ui_accept")) {
        //     GameStateManager.Instance.Fire(GameStateMachine.Trigger.DISPLAY_MAIN_MENU);
        // }
        if (@event.IsActionPressed("ui_cancel")) {
            await UIManager.Instance.saveGameScreen.OnGoBackButtonPressed();
        }
    }

    private async Task HandleSplashScreenInput(InputEvent @event) {
        if (@event.IsActionPressed("ui_accept")) {
            await UIManager.Instance.splashScreen.TransitionToMainMenu();
        }
    }

    private void OnGameStateChanged(GameStateMachine.State previousState, GameStateMachine.SubState previousSubstate,
                                    GameStateMachine.State newState, GameStateMachine.SubState newSubState, object[] arguments) {
        
            // Store the current index if we're leaving a main menu or in-game menu
    if (previousState == State.MainMenuDisplayed && previousSubstate == SubState.None) {
        lastMainMenuIndex = currentFocusedIndex;
    } else if (previousState == State.InGameMenuDisplayed && previousSubstate == SubState.None) {
        lastInGameMenuIndex = currentFocusedIndex;
    }
        
        
        UpdateFocusableControls(newState, newSubState);
    }


    private int lastMainMenuIndex = -1;
    private int lastInGameMenuIndex = -1;

private void UpdateFocusableControls(State currentState, SubState subState) {
    focusableControls.Clear();
    currentFocusedMenu = null;

    if (currentState == State.MainMenuDisplayed && subState == SubState.None) {
        currentFocusedMenu = UIManager.Instance.mainMenu;
        CollectFocusableControls(currentFocusedMenu);
        currentFocusedIndex = (lastMainMenuIndex >= 0 && lastMainMenuIndex < focusableControls.Count) 
            ? lastMainMenuIndex 
            : 0;
    } 
    else if (currentState == State.InGameMenuDisplayed && subState == SubState.None) {
        currentFocusedMenu = UIManager.Instance.mainMenu; // Assuming you have an inGameMenu
        CollectFocusableControls(currentFocusedMenu);
        currentFocusedIndex = (lastInGameMenuIndex >= 0 && lastInGameMenuIndex < focusableControls.Count) 
            ? lastInGameMenuIndex 
            : 0;
    }
    else if (currentState == State.MainMenuDisplayed && subState == SubState.ExitGameConfirmationPopupDisplayed) {
        currentFocusedMenu = UIManager.Instance.mainMenu.ExitGameConfirmationPanel;
        CollectFocusableControls(currentFocusedMenu);
        currentFocusedIndex = 0; // Focus on the first button in the submenu
    }
    else if (currentState == State.InGameMenuDisplayed && subState == SubState.ExitGameConfirmationPopupDisplayed) {
        currentFocusedMenu = UIManager.Instance.mainMenu.ExitGameConfirmationPanel;
        CollectFocusableControls(currentFocusedMenu);
        currentFocusedIndex = 0; // Focus on the first button in the submenu
    }
    else if (currentState == State.InGameMenuDisplayed && subState == SubState.ExitToMainMenuConfirmationPopupDisplayed) {
        currentFocusedMenu = UIManager.Instance.mainMenu.ExitToMainMenuPanel;
        CollectFocusableControls(currentFocusedMenu);
        currentFocusedIndex = 0; // Focus on the first button in the submenu
    }
    else if ((currentState == State.MainMenuDisplayed || currentState == State.InGameMenuDisplayed) && subState == SubState.LanguageMenuDisplayed) {
        currentFocusedMenu = UIManager.Instance.mainMenu.LanguageOptionsContainer;
        CollectFocusableControls(currentFocusedMenu);
        currentFocusedIndex = 0; // Focus on the first button in the submenu
    }
    else if (currentState == State.SplashScreenDisplayed && subState == SubState.None) {
        currentFocusedMenu = UIManager.Instance.splashScreen;
        focusableControls.Add(UIManager.Instance.splashScreen.backgroundTexture);
        currentFocusedIndex = 0;
    }
    else {
        // For any other state/substate combination, clear focus
        currentFocusedIndex = -1;
    }

    HighlightMenuButton(currentFocusedIndex);
}

    private void CollectFocusableControls(Control container) {
        foreach (var child in container.GetChildren()) {
            if (child is Button button && button.Visible) {
                focusableControls.Add(button);
            } else if (child is Control control) {
                CollectFocusableControls(control);
            }
        }
    }

    private List<Control> GetVisibleFocusableControls() {
        return focusableControls.FindAll(control => control.Visible);
    }



    private void HandleMenuInput(InputEvent @event) {
        float currentTime = (float)Time.GetTicksMsec() / 1000.0f;
        bool isVertical = GameStateManager.Instance.CurrentSubstate != SubState.ExitGameConfirmationPopupDisplayed &&
                          GameStateManager.Instance.CurrentSubstate != SubState.ExitToMainMenuConfirmationPopupDisplayed;

        if (isVertical) {
            if (CanProcessInput(currentTime) &&
                (Input.IsActionPressed("ui_up") ||
                 (@event is InputEventJoypadMotion joypadMotionUp &&
                  joypadMotionUp.Axis == JoyAxis.LeftY &&
                  joypadMotionUp.AxisValue < -STICK_THRESHOLD))) {
                HandleVerticalNavigation(true);
                lastInputTime = currentTime;
            } else if (CanProcessInput(currentTime) &&
                       (Input.IsActionPressed("ui_down") ||
                        (@event is InputEventJoypadMotion joypadMotionDown &&
                         joypadMotionDown.Axis == JoyAxis.LeftY &&
                         joypadMotionDown.AxisValue > STICK_THRESHOLD))) {
                HandleVerticalNavigation(false);
                lastInputTime = currentTime;
            }
        } else {
            if (CanProcessInput(currentTime) &&
                (Input.IsActionPressed("ui_left") ||
                 (@event is InputEventJoypadMotion joypadMotionLeft &&
                  joypadMotionLeft.Axis == JoyAxis.LeftX &&
                  joypadMotionLeft.AxisValue < -STICK_THRESHOLD))) {
                HandleHorizontalNavigation(true);
                lastInputTime = currentTime;
            } else if (CanProcessInput(currentTime) &&
                       (Input.IsActionPressed("ui_right") ||
                        (@event is InputEventJoypadMotion joypadMotionRight &&
                         joypadMotionRight.Axis == JoyAxis.LeftX &&
                         joypadMotionRight.AxisValue > STICK_THRESHOLD))) {
                HandleHorizontalNavigation(false);
                lastInputTime = currentTime;
            }
        }

        if (Input.IsActionJustPressed("ui_accept")) {
            HandleMenuAccept();
        } else if (Input.IsActionJustPressed("ui_cancel")) {
            HandleMenuCancel();
        }
    }

    private bool CanProcessInput(float currentTime) {
        return currentTime - lastInputTime >= INPUT_COOLDOWN;
    }

    private void HandleVerticalNavigation(bool isUp) {
        var visibleControls = GetVisibleFocusableControls();
        if (visibleControls.Count == 0) return;

        int currentVisibleIndex = currentFocusedIndex != -1 ? visibleControls.IndexOf(focusableControls[currentFocusedIndex]) : -1;

        if (currentVisibleIndex == -1) {
            currentVisibleIndex = isUp ? visibleControls.Count - 1 : 0;
        } else {
            currentVisibleIndex = (currentVisibleIndex + (isUp ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
        }

        currentFocusedIndex = focusableControls.IndexOf(visibleControls[currentVisibleIndex]);
        HighlightMenuButton(currentFocusedIndex);
    }

    private void HandleHorizontalNavigation(bool isLeft) {
        var visibleControls = GetVisibleFocusableControls();
        if (visibleControls.Count == 0) return;

        int currentVisibleIndex = currentFocusedIndex != -1 ? visibleControls.IndexOf(focusableControls[currentFocusedIndex]) : -1;

        if (currentVisibleIndex == -1) {
            currentVisibleIndex = isLeft ? visibleControls.Count - 1 : 0;
        } else {
            currentVisibleIndex = (currentVisibleIndex + (isLeft ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
        }

        currentFocusedIndex = focusableControls.IndexOf(visibleControls[currentVisibleIndex]);
        HighlightMenuButton(currentFocusedIndex);
    }

    private bool isProcessingInput = false;

    private DateTime lastAcceptTime = DateTime.MinValue;
    private const int DEBOUNCE_MILLISECONDS = 500; // Adjust this value as needed

    // ... (existing code)

    private async void HandleMenuAccept() {
        DateTime now = DateTime.Now;
        if (isProcessingInput || (now - lastAcceptTime).TotalMilliseconds < DEBOUNCE_MILLISECONDS) {
            return;
        }

        if (currentFocusedIndex >= 0 && currentFocusedIndex < focusableControls.Count) {
            if (focusableControls[currentFocusedIndex] is Button button) {
                try {
                    isProcessingInput = true;
                    lastAcceptTime = now;

                    var tcs = new TaskCompletionSource<bool>();
                    void SignalHandler() {
                        tcs.TrySetResult(true);
                        button.Disconnect("pressed", Callable.From(SignalHandler));
                    }

                    button.Connect("pressed", Callable.From(SignalHandler));
                    button.EmitSignal(Button.SignalName.Pressed);

                    await tcs.Task;
                } finally {
                    isProcessingInput = false;
                }
            }
        }
    }
    private async Task HandleMenuCancel() {

        if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed)) {
            await UIManager.Instance.mainMenu.OnExitGameCancelButtonPressed();
            return;
        }

        //if the ingame menu is displayed, we close it or open it
        else if (GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed || GameStateManager.Instance.CurrentState == State.InDialogueMode)
            if (GameStateManager.Instance.CurrentSubstate == SubState.None)
                await UIManager.Instance.inGameMenuButton.OnTextureButtonPressed();

            else if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed || GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed)
                if (GameStateManager.Instance.CurrentSubstate == SubState.LanguageMenuDisplayed)
                    await UIManager.Instance.mainMenu.OnLanguagesGoBackButtonPressed();
        // else if(GameStateManager.Instance.CurrentSubstate == SubState.LoadScreenDisplayed)
        //     await UIManager.Instance.saveGameScreen.OnGoBackButtonPressed();
    }


    private void HandleDialogueInput(InputEvent @event) {
        if (DialogueManager.Instance.playerChoicesList.Count > 0) {
            HandlePlayerChoicesInput(@event);
        } else {
            if (@event.IsActionPressed("ui_accept")) {
                DialogueManager.Instance.OnDialogueBoxUIPressed();
            } else if (@event.IsActionPressed("ui_cancel")) {
                GameStateManager.Instance.Fire(GameStateMachine.Trigger.DISPLAY_INGAME_MENU);
            }
        }
    }

    private void HandlePlayerChoicesInput(InputEvent @event) {
        var choices = UIManager.Instance.playerChoicesBoxUI.GetPlayerChoiceButtons();
        if (choices.Count == 0) return;

        if (@event.IsActionPressed("ui_up") ||
            (@event is InputEventJoypadMotion joypadMotionUp &&
             joypadMotionUp.Axis == JoyAxis.LeftY &&
             joypadMotionUp.AxisValue < -STICK_THRESHOLD)) {
            currentPlayerChoiceIndex = (currentPlayerChoiceIndex - 1 + choices.Count) % choices.Count;
            HighlightPlayerChoice(currentPlayerChoiceIndex, choices);
            lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
        } else if (@event.IsActionPressed("ui_down") ||
                   (@event is InputEventJoypadMotion joypadMotionDown &&
                    joypadMotionDown.Axis == JoyAxis.LeftY &&
                    joypadMotionDown.AxisValue > STICK_THRESHOLD)) {
            currentPlayerChoiceIndex = (currentPlayerChoiceIndex + 1) % choices.Count;
            HighlightPlayerChoice(currentPlayerChoiceIndex, choices);
            lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
        } else if (@event.IsActionPressed("ui_accept")) {
            SelectCurrentPlayerChoice(choices);
        }
    }


    private void HighlightMenuButton(int index) {
        var visibleControls = GetVisibleFocusableControls();
        for (int i = 0; i < visibleControls.Count; i++) {
            if (visibleControls[i] is Button button) {
                bool isHighlighted = visibleControls[i] == focusableControls[index];
                ApplyButtonStyle(button, isHighlighted);
                if (isHighlighted) {
                    button.GrabFocus();
                }
            }
        }
    }

    private void ApplyButtonStyle(Button button, bool isHighlighted) {
        if (isHighlighted) {
            var hoverStyle = UIThemeHelper.GetHoverStyleBox();
            button.AddThemeStyleboxOverride("normal", hoverStyle);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("focus", hoverStyle);
        } else {
            var normalStyle = UIThemeHelper.GetNormalStyleBox();
            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", normalStyle);
            button.AddThemeStyleboxOverride("focus", normalStyle);
        }
    }

    private void HighlightPlayerChoice(int index, List<PlayerChoiceButton> choices) {
        for (int i = 0; i < choices.Count; i++) {
            var playerChoiceButton = choices[i];
            bool isHighlighted = i == index;
            ApplyPlayerChoiceStyle(playerChoiceButton, isHighlighted);
        }
    }

    private void ApplyPlayerChoiceStyle(PlayerChoiceButton choiceButton, bool isHighlighted) {
        var button = choiceButton.GetNode<Button>("Button");
        if (button != null) {
            var styleBox = isHighlighted ? choiceButton.hoverStyleBox : choiceButton.normalStyleBox;
            button.AddThemeStyleboxOverride("normal", styleBox);
        }
    }

    private void SelectCurrentPlayerChoice(List<PlayerChoiceButton> choices) {
        if (currentPlayerChoiceIndex >= 0 && currentPlayerChoiceIndex < choices.Count) {
            var selectedChoice = choices[currentPlayerChoiceIndex];
            DialogueManager.Instance.OnPlayerChoiceButtonUIPressed(selectedChoice.dialogueObject);
        }
    }
}