using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;
using System.Threading.Tasks;

public partial class InputManager : Node {
    public static InputManager Instance { get; private set; }
    private Control currentFocusedMenu;
    private int currentFocusedIndex = -1;
    private List<Control> focusableControls = new List<Control>();
    private int currentPlayerChoiceIndex = -1;

    private float lastInputTime = 0f;
    private const float INPUT_DELAY = 0.2f;
    private const float STICK_THRESHOLD = 0.5f;

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

        switch (GameStateManager.Instance.CurrentState) {
            case State.MainMenuDisplayed:
            case State.InGameMenuDisplayed:
                HandleMenuInput(@event);
                break;
            case State.InDialogueMode:
                HandleDialogueInput(@event);
                break;
            case State.SplashScreenDisplayed:
                HandleSplashScreenInput(@event);
                break;
        }
    }

private void HandleMenuInput(InputEvent @event)
{
    bool isVertical = GameStateManager.Instance.CurrentSubstate != SubState.ExitGameConfirmationPopupDisplayed;

    if (isVertical)
    {
        if (@event.IsActionPressed("ui_up") || 
            (@event is InputEventJoypadMotion joypadMotionUp && 
             joypadMotionUp.Axis == JoyAxis.LeftY && 
             joypadMotionUp.AxisValue < -STICK_THRESHOLD))
        {
            HandleVerticalNavigation(true);
            lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
        }
        else if (@event.IsActionPressed("ui_down") || 
                 (@event is InputEventJoypadMotion joypadMotionDown && 
                  joypadMotionDown.Axis == JoyAxis.LeftY && 
                  joypadMotionDown.AxisValue > STICK_THRESHOLD))
        {
            HandleVerticalNavigation(false);
            lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
        }
    }
    else
    {
        if (@event.IsActionPressed("ui_left") || 
            (@event is InputEventJoypadMotion joypadMotionLeft && 
             joypadMotionLeft.Axis == JoyAxis.LeftX && 
             joypadMotionLeft.AxisValue < -STICK_THRESHOLD))
        {
            HandleHorizontalNavigation(true);
            lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
        }
        else if (@event.IsActionPressed("ui_right") || 
                 (@event is InputEventJoypadMotion joypadMotionRight && 
                  joypadMotionRight.Axis == JoyAxis.LeftX && 
                  joypadMotionRight.AxisValue > STICK_THRESHOLD))
        {
            HandleHorizontalNavigation(false);
            lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
        }
    }

    if (@event.IsActionPressed("ui_accept"))
    {
        HandleAccept();
    }
    else if (@event.IsActionPressed("ui_cancel"))
    {
        HandleCancel();
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
        foreach (var child in UIManager.Instance.mainMenu.YesNoButtonsHBoxContainer.GetChildren()) {
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

    private void HandleAccept() {
        if (currentFocusedIndex >= 0 && currentFocusedIndex < focusableControls.Count) {
            if (focusableControls[currentFocusedIndex] is Button button) {
                button.EmitSignal(Button.SignalName.Pressed);
            }
        }
    }

    private void HandleCancel() {
        if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed) {
            if (GameStateManager.Instance.CurrentSubstate == SubState.ExitGameConfirmationPopupDisplayed) {
                GameStateManager.Instance.Fire(GameStateMachine.Trigger.GO_BACK_TO_MENU);
            } else {
                GameStateManager.Instance.Fire(GameStateMachine.Trigger.DISPLAY_EXIT_GAME_MENU_CONFIRMATION_POPUP);
            }
        } else if (GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed) {
            if (GameStateManager.Instance.CurrentSubstate == SubState.None) {
                GameStateManager.Instance.Fire(GameStateMachine.Trigger.ENTER_DIALOGUE_MODE);
            } else {
                GameStateManager.Instance.Fire(GameStateMachine.Trigger.GO_BACK_TO_MENU);
            }
        }
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

  private void HandlePlayerChoicesInput(InputEvent @event)
{
    var choices = UIManager.Instance.playerChoicesBoxUI.GetPlayerChoiceButtons();
    if (choices.Count == 0) return;

    if (@event.IsActionPressed("ui_up") || 
        (@event is InputEventJoypadMotion joypadMotionUp && 
         joypadMotionUp.Axis == JoyAxis.LeftY && 
         joypadMotionUp.AxisValue < -STICK_THRESHOLD))
    {
        currentPlayerChoiceIndex = (currentPlayerChoiceIndex - 1 + choices.Count) % choices.Count;
        HighlightPlayerChoice(currentPlayerChoiceIndex, choices);
        lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
    }
    else if (@event.IsActionPressed("ui_down") || 
             (@event is InputEventJoypadMotion joypadMotionDown && 
              joypadMotionDown.Axis == JoyAxis.LeftY && 
              joypadMotionDown.AxisValue > STICK_THRESHOLD))
    {
        currentPlayerChoiceIndex = (currentPlayerChoiceIndex + 1) % choices.Count;
        HighlightPlayerChoice(currentPlayerChoiceIndex, choices);
        lastInputTime = (float)Time.GetTicksMsec() / 1000.0f;
    }
    else if (@event.IsActionPressed("ui_accept"))
    {
        SelectCurrentPlayerChoice(choices);
    }
}

    private void HandleSplashScreenInput(InputEvent @event) {
        if (@event.IsActionPressed("ui_accept")) {
            GameStateManager.Instance.Fire(GameStateMachine.Trigger.DISPLAY_MAIN_MENU);
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

    private List<Control> GetVisibleFocusableControls() {
        return focusableControls.FindAll(control => control.Visible);
    }

    private void UpdateFocusableControls(State currentState, SubState subState) {
        focusableControls.Clear();
        currentFocusedIndex = -1;

        switch (currentState) {
            case State.MainMenuDisplayed:
            case State.InGameMenuDisplayed:
                currentFocusedMenu = UIManager.Instance.mainMenu;
                CollectFocusableControls(UIManager.Instance.mainMenu.MainOptionsContainer);
                break;
            case State.SplashScreenDisplayed:
                currentFocusedMenu = UIManager.Instance.splashScreen;
                focusableControls.Add(UIManager.Instance.splashScreen.backgroundTexture);
                break;
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

    private void OnGameStateChanged(GameStateMachine.State previousState, GameStateMachine.SubState previousSubstate,
                                    GameStateMachine.State newState, GameStateMachine.SubState newSubState, object[] arguments) {
        UpdateFocusableControls(newState, newSubState);
    }
}