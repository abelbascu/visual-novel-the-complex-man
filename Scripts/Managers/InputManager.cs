using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;


public partial class InputManager : Node {

    public static InputManager Instance { get; private set; }
    private Control currentFocusedMenu;
    private int currentFocusedIndex = -1;
    private List<Control> focusableControls = new List<Control>();
    private int currentPlayerChoiceIndex = -1;

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
        switch (GameStateManager.Instance.CurrentState) {
            case GameStateMachine.State.MainMenuDisplayed:
            case GameStateMachine.State.InGameMenuDisplayed:
                UpdateFocusableControls(GameStateManager.Instance.CurrentState, GameStateManager.Instance.CurrentSubstate);
                HandleMenuInput(@event);
                break;
            case GameStateMachine.State.InDialogueMode:
                if (DialogueManager.Instance.playerChoicesList.Count > 0) {
                    HandlePlayerChoicesInput(@event);
                } else {
                    HandleDialogueInput(@event);
                }
                break;
                // Add more cases for other states as needed
        }
    }

    private void UpdateFocusableControls(State currentState, SubState subState) {

        if (currentFocusedIndex == -1) {
            focusableControls.Clear();
            currentFocusedIndex = -1;
        }

        switch (currentState) {
            case GameStateMachine.State.MainMenuDisplayed:
                currentFocusedMenu = UIManager.Instance.mainMenu;
                CollectFocusableControls(UIManager.Instance.mainMenu.MainOptionsContainer);
                break;
            case GameStateMachine.State.InGameMenuDisplayed:
                currentFocusedMenu = UIManager.Instance.mainMenu;
                CollectFocusableControls(UIManager.Instance.mainMenu.MainOptionsContainer);
                break;
            case GameStateMachine.State.InDialogueMode:
                // No focusable controls in dialogue mode
                break;
                // Add more cases for other states and substates
        }

        // SetInitialFocus();
    }

    private void CollectFocusableControls(Control container) {
        // focusableControls.Clear();
        CollectVisibleFocusableControlsRecursive(container);
    }

    private void CollectVisibleFocusableControlsRecursive(Control container) {
        foreach (var child in container.GetChildren()) {
            if (child is Button button && button.Visible) {
                focusableControls.Add(button);
            } else if (child is Control control) {
                CollectVisibleFocusableControlsRecursive(control);
            }
        }
    }

    public void SetInitialFocus() {
        if (focusableControls.Count > 0) {
            currentFocusedIndex = -1;
            focusableControls[0].GrabFocus();
        }
    }

    private void HandleMenuInput(InputEvent @event) {
        if (currentFocusedMenu == null) return;

        if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down")) {
            HandleVerticalNavigation(@event.IsActionPressed("ui_up"));
        } else if (@event.IsActionPressed("ui_accept")) {
            HandleAccept();
        } else if (@event.IsActionPressed("ui_cancel")) {
            HandleCancel();
        }
    }

    private void HandleVerticalNavigation(bool isUp) {
        var visibleControls = GetVisibleFocusableControls();
        if (visibleControls.Count == 0) return;

        GD.Print($"Visible controls count: {visibleControls.Count}"); // Debug log

        if (currentFocusedIndex == -1 || !visibleControls.Contains(focusableControls[currentFocusedIndex])) {
            currentFocusedIndex = isUp ? visibleControls.Count - 1 : 0;
        } else {
            int currentVisibleIndex = visibleControls.IndexOf(focusableControls[currentFocusedIndex]);
            int newVisibleIndex = (currentVisibleIndex + (isUp ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
            currentFocusedIndex = focusableControls.IndexOf(visibleControls[newVisibleIndex]);
        }

        GD.Print($"Current focused index: {currentFocusedIndex}"); // Debug log
        HighlightMenuButton(currentFocusedIndex);
    }

    private List<Control> GetVisibleFocusableControls() {
        return focusableControls.Where(control => control.Visible).ToList();
    }

    private Button currentHighlightedButton;


    private void HighlightMenuButton(int index) {
        var visibleControls = GetVisibleFocusableControls();
        for (int i = 0; i < visibleControls.Count; i++) {
            if (visibleControls[i] is Button button) {
                bool isHighlighted = i == index;

                if (isHighlighted) {
                    // De-highlight the previously highlighted button
                    if (currentHighlightedButton != null && currentHighlightedButton != button) {
                        ApplyNormalStyle(currentHighlightedButton);
                    }

                    // Highlight the new button
                    ApplyHoverStyle(button);
                    currentHighlightedButton = button;
                } else if (button != currentHighlightedButton) {
                    // Ensure non-highlighted buttons have normal style
                    ApplyNormalStyle(button);
                }

                GD.Print($"Button {i} ('{button.Name}') highlighted: {isHighlighted}"); // Debug log
            }
        }
    }

    private void ApplyHoverStyle(Button button) {
        var hoverStyle = UIThemeHelper.GetHoverStyleBox();
        button.AddThemeStyleboxOverride("normal", hoverStyle);
    }

    private void ApplyNormalStyle(Button button) {
        var normalStyle = UIThemeHelper.GetNormalStyleBox();
        button.AddThemeStyleboxOverride("normal", normalStyle);
    }


    private void OnGameStateChanged(GameStateMachine.State previousState, GameStateMachine.SubState previousSubstate,
                                    GameStateMachine.State newState, GameStateMachine.SubState newSubState, object[] arguments) {
        UpdateFocusableControls(newState, newSubState);
    }


    private void OnButtonFocused(Button button) {
        UIThemeHelper.ApplyFocusStyleToButton(button, true);
    }

    private void OnButtonUnfocused(Button button) {
        UIThemeHelper.ApplyFocusStyleToButton(button, false);
    }

    private void HandleAccept() {
        if (currentFocusedIndex >= 0 && currentFocusedIndex < focusableControls.Count) {
            if (focusableControls[currentFocusedIndex] is Button button) {
                button.EmitSignal(Button.SignalName.Pressed);
            }
        }
    }

    private void HandleCancel() {
        switch (GameStateManager.Instance.CurrentState) {
            case GameStateMachine.State.InGameMenuDisplayed:
                GameStateManager.Instance.Fire(GameStateMachine.Trigger.ENTER_DIALOGUE_MODE);
                break;
            case GameStateMachine.State.MainMenuDisplayed:
                if (GameStateManager.Instance.CurrentSubstate == GameStateMachine.SubState.LanguageMenuDisplayed) {
                    GameStateManager.Instance.Fire(GameStateMachine.Trigger.GO_BACK_TO_MENU);
                }
                // Add more submenu cases as needed
                break;
                // Add more cases for other states
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

    private void HandlePlayerChoicesInput(InputEvent @event) {
        var choices = UIManager.Instance.playerChoicesBoxUI.GetPlayerChoiceButtons();
        GD.Print($"Player choices count: {choices.Count}"); // Debug log

        if (choices.Count == 0) return;

        if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down")) {
            bool isUp = @event.IsActionPressed("ui_up");
            int oldIndex = currentPlayerChoiceIndex;
            currentPlayerChoiceIndex = (currentPlayerChoiceIndex + (isUp ? -1 : 1) + choices.Count) % choices.Count;
            GD.Print($"Player choice index changed from {oldIndex} to {currentPlayerChoiceIndex}"); // Debug log
            HighlightPlayerChoice(currentPlayerChoiceIndex, choices);
        } else if (@event.IsActionPressed("ui_accept")) {
            SelectCurrentPlayerChoice(choices);
        }
    }

    private void HighlightPlayerChoice(int index, List<PlayerChoiceButton> choices) {
        for (int i = 0; i < choices.Count; i++) {
            var playerChoiceButton = choices[i];
            bool isHighlighted = i == index;
            var button = playerChoiceButton.GetNodeOrNull<Button>(".");
            if (button == null) {
                button = playerChoiceButton.GetNodeOrNull<Button>("Button");
            }
            if (button != null) {
                var styleBox = isHighlighted ? playerChoiceButton.hoverStyleBox : playerChoiceButton.normalStyleBox;
                if (styleBox != null) {
                    button.AddThemeStyleboxOverride("normal", styleBox.Duplicate() as StyleBoxFlat);
                    GD.Print($"Player choice {i} highlighted: {isHighlighted}"); // Debug log
                } else {
                    GD.PrintErr($"StyleBox not found for PlayerChoiceButton {i}"); // Error log
                }
            } else {
                GD.PrintErr($"Button not found in PlayerChoiceButton {i}. Hierarchy: {playerChoiceButton.GetPath()}"); // Detailed error log
            }
        }
    }

    private void ApplyHighlightToPlayerChoice(PlayerChoiceButton choiceButton, bool isHighlighted) {
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