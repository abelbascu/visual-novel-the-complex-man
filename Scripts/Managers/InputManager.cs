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
    private const float INPUT_DELAY = 0.2f;
    private const float STICK_THRESHOLD = 0.5f;

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
        float currentTime = (float)Time.GetTicksMsec() / 1000.0f;
        if (currentTime - lastInputTime < INPUT_DELAY) return;

        if (!isGamePadAndKeyboardInputEnabled || isInputLocked) return;

        isInputLocked = true;

        //GD.Print($"Current State: {GameStateManager.Instance.CurrentState}, Current Substate: {GameStateManager.Instance.CurrentSubstate}");

        try {
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

            lastInputTime = currentTime;
        } finally {
            
            isInputLocked = false;
        }


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
        UpdateFocusableControls(newState, newSubState);
    }


    private void UpdateFocusableControls(State currentState, SubState subState) {
        focusableControls.Clear();
        currentFocusedIndex = -1;
        currentFocusedMenu = null;

        if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.None) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.None)) {
            currentFocusedMenu = UIManager.Instance.mainMenu;
            CollectFocusableControls(currentFocusedMenu);
        } else if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed)) {
            currentFocusedMenu = UIManager.Instance.mainMenu.ExitGameConfirmationPanel;
            CollectFocusableControls(currentFocusedMenu);
        } else if (GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed)) {
            currentFocusedMenu = UIManager.Instance.mainMenu.ExitToMainMenuPanel;
            CollectFocusableControls(currentFocusedMenu);
        } else if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.LanguageMenuDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed)) {
            currentFocusedMenu = UIManager.Instance.mainMenu.LanguageOptionsContainer;
            CollectFocusableControls(currentFocusedMenu);
        } else if (GameStateManager.Instance.IsInState(State.SplashScreenDisplayed, SubState.None)) {
            currentFocusedMenu = UIManager.Instance.splashScreen;
            focusableControls.Add(UIManager.Instance.splashScreen.backgroundTexture);
        }

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
        bool isVertical = GameStateManager.Instance.CurrentSubstate != SubState.ExitGameConfirmationPopupDisplayed && GameStateManager.Instance.CurrentSubstate != SubState.ExitToMainMenuConfirmationPopupDisplayed;

        if (isVertical) {
            if (@event.IsActionPressed("ui_up") ||
                (@event is InputEventJoypadMotion joypadMotionUp &&
                 joypadMotionUp.Axis == JoyAxis.LeftY &&
                 joypadMotionUp.AxisValue < -STICK_THRESHOLD)) {
                HandleVerticalNavigation(true);
                lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
            } else if (@event.IsActionPressed("ui_down") ||
                       (@event is InputEventJoypadMotion joypadMotionDown &&
                        joypadMotionDown.Axis == JoyAxis.LeftY &&
                        joypadMotionDown.AxisValue > STICK_THRESHOLD)) {
                HandleVerticalNavigation(false);
                lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
            }
        } else {
            if (@event.IsActionPressed("ui_left") ||
                (@event is InputEventJoypadMotion joypadMotionLeft &&
                 joypadMotionLeft.Axis == JoyAxis.LeftX &&
                 joypadMotionLeft.AxisValue < -STICK_THRESHOLD)) {
                HandleHorizontalNavigation(true);
                lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
            } else if (@event.IsActionPressed("ui_right") ||
                       (@event is InputEventJoypadMotion joypadMotionRight &&
                        joypadMotionRight.Axis == JoyAxis.LeftX &&
                        joypadMotionRight.AxisValue > STICK_THRESHOLD)) {
                HandleHorizontalNavigation(false);
                lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
            }
        }

        if (@event.IsActionPressed("ui_accept")) {
            HandleMenuAccept();
        } else if (@event.IsActionPressed("ui_cancel")) {
            HandleMenuCancel();
        }
    }

    private void HandleVerticalNavigation(bool isUp) {
        var visibleControls = GetVisibleFocusableControls();
        if (visibleControls.Count == 0) return;

        if (currentFocusedIndex == -1 || !visibleControls.Contains(focusableControls[currentFocusedIndex])) {
            currentFocusedIndex = isUp ? visibleControls.Count - 1 : 0;
        } else {
            int currentVisibleIndex = visibleControls.IndexOf(focusableControls[currentFocusedIndex]);
            int newVisibleIndex = (currentVisibleIndex + (isUp ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
            currentFocusedIndex = focusableControls.IndexOf(visibleControls[newVisibleIndex]);
        }

        HighlightMenuButton(currentFocusedIndex);
    }



    private void HandleHorizontalNavigation(bool isLeft) {
        List<Button> buttons = new();
        foreach (var child in focusableControls) {
            if (child is Button button) {
                buttons.Add(button);
            }
        }
        int currentIndex = buttons.IndexOf(buttons.FirstOrDefault(b => b.HasFocus()));
        int newIndex = isLeft ? 0 : 1;
        if (currentIndex != -1) {
            newIndex = (currentIndex + (isLeft ? -1 : 1) + buttons.Count) % buttons.Count;
        }
        buttons[newIndex].GrabFocus();
    }

    private void HandleMenuAccept() {
        if (currentFocusedIndex >= 0 && currentFocusedIndex < focusableControls.Count) {
            if (focusableControls[currentFocusedIndex] is Button button) {
                button.EmitSignal(Button.SignalName.Pressed);
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
                bool isHighlighted = i == index;
                ApplyButtonStyle(button, isHighlighted);
            }
        }
    }

    private void ApplyButtonStyle(Button button, bool isHighlighted) {
        if (isHighlighted) {
            button.GrabFocus();
            var hoverStyle = UIThemeHelper.GetHoverStyleBox();
            button.AddThemeStyleboxOverride("normal", hoverStyle);
        } else {
            var normalStyle = UIThemeHelper.GetNormalStyleBox();
            button.AddThemeStyleboxOverride("normal", normalStyle);
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