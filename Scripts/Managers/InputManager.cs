using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;
using System.Threading.Tasks;

public partial class InputManager : Control {

  public static InputManager Instance { get; private set; }

  //to refernce whihc UI control has been clicked, pressed or selected
  private Control currentFocusedMenu;
  private int currentFocusedIndex = -1;
  private List<Control> focusableControls = new List<Control>();

  private int currentPlayerChoiceIndex = -1;

  //add cooldown to avoid duplicated key or button press
  private float lastInputTime = 0f;
  private const float INPUT_COOLDOWN = 0.2f; // 200ms cooldown

  //if gamepad stick moves this distance, register as movement
  private const float STICK_THRESHOLD = 0.3f;

  //we use this to disable button presses just after UI element is pressed
  //to prevent multiple button press that would break the game state transitions.
  private bool isGamePadAndKeyboardInputEnabled = true;
  public void SetGamePadAndKeyboardInputEnabled(bool enabled) {
    isGamePadAndKeyboardInputEnabled = enabled;
  }

  //when we process input let's block further input
  //until we finish processing
  public bool isProcessingInput = false;



  public override void _EnterTree() {
    if (Instance == null) {
      Instance = this;
    } else {
      QueueFree();
    }
  }


  private InputBlocker inputBlocker;
     private void InitializeInputBlocker()
    {
        var mainMenu = GetNode<MainMenu>("../UIManager/MainMenu");

        inputBlocker = mainMenu.inputBlocker;
    }



  public override void _Ready() {

    CallDeferred(nameof(InitializeInputBlocker));

    //subcribe when game changes state
    GameStateManager.Instance.StateChanged += (prevState, prevSubState, newState, newSubState, args) => {
      _ = OnGameStateChanged(prevState, prevSubState, newState, newSubState, args);
    };
  }

  public override void _ExitTree() {

    GameStateManager.Instance.StateChanged -= (prevState, prevSubState, newState, newSubState, args) => {
      _ = OnGameStateChanged(prevState, prevSubState, newState, newSubState, args);
    };
  }



  private bool isInputLocked = false;

  public override async void _Input(InputEvent @event) {

    if (inputBlocker == null || inputBlocker.IsBlocked) {
      GetViewport().SetInputAsHandled();
      return;
    }

    //GD.Print($"_GuiInput called with event: {@event}");
    if (!isGamePadAndKeyboardInputEnabled || isProcessingInput || isInputLocked) {
      GetViewport().SetInputAsHandled();
      return;
    }

    //even if we are blocking input, user can still move the mouse
    if (@event is InputEventMouseMotion mouseMotion) {
      HandleMouseMotion(mouseMotion);
      GD.Print($"currentFocusIndex: {currentFocusedIndex}");
      return; // We don't want to AcceptEvent for mouse motion
    }

    //if processing occurs, don't read any input

    //the real input processing starts here
    //this is for mouse input
    if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {

      await ProcessInputAsync(@event);
      AcceptEvent();

      //this is for gamepad and keyboard input
    } else if (isGamePadAndKeyboardInputEnabled) {
      if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel") || @event.IsActionPressed("ui_left") || @event.IsActionPressed("ui_right") || @event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down")) {
        isProcessingInput = true;
        _ = ProcessInputAsync(@event);
        AcceptEvent();
      }
    }
  }

  //depending on the game state, execute custom input methods
  private async Task ProcessInputAsync(InputEvent @event) {

    try {
      if (GameStateManager.Instance.IsInState(GameStateMachine.State.SplashScreenDisplayed, GameStateMachine.SubState.None)) {
        await HandleSplashScreenInput(@event);
      } else if (GameStateManager.Instance.IsInState(GameStateMachine.State.MainMenuDisplayed, GameStateMachine.SubState.LoadScreenDisplayed) ||
                 GameStateManager.Instance.IsInState(GameStateMachine.State.InGameMenuDisplayed, GameStateMachine.SubState.LoadScreenDisplayed) ||
                 GameStateManager.Instance.IsInState(GameStateMachine.State.InGameMenuDisplayed, GameStateMachine.SubState.SaveScreenDisplayed)) {
        await HandleMenuInput(@event);
      } // Add more state checks and handlers as needed
        else if (GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.None) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.None)
      || GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.ExitGameConfirmationPopupDisplayed)
      || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.ExitToMainMenuConfirmationPopupDisplayed)
      || GameStateManager.Instance.IsInState(State.MainMenuDisplayed, SubState.LanguageMenuDisplayed) || GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.LanguageMenuDisplayed)) {
        await HandleMenuInput(@event);
      } else if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None))

        await HandleMenuInput(@event);
      // } else {
      //   focusableControls.Clear();
      //     currentFocusedMenu = null;
      //   currentFocusedIndex = -1;
      // }

    } finally {
      isProcessingInput = false;
    }
  }


  private async Task HandleSplashScreenInput(InputEvent @event) {
    await UIManager.Instance.splashScreen.TransitionToMainMenu();
    isProcessingInput = false;
  }


  private async Task OnGameStateChanged(GameStateMachine.State previousState, GameStateMachine.SubState previousSubstate,
                                  GameStateMachine.State newState, GameStateMachine.SubState newSubState, object[] arguments) {

    // Store the current index if we're leaving a main menu or in-game menu
    if (previousState == State.MainMenuDisplayed && previousSubstate == SubState.None) {
      lastMainMenuIndex = currentFocusedIndex;
    } else if (previousState == State.InGameMenuDisplayed && previousSubstate == SubState.None) {
      lastInGameMenuIndex = currentFocusedIndex;
    }

    await UpdateFocusableControls(newState, newSubState);
    await ClearButtonHighlights();
  }


  private int lastMainMenuIndex = -1;
  private int lastInGameMenuIndex = -1;

  private async Task UpdateFocusableControls(State currentState, SubState subState) {
    focusableControls.Clear();
    currentFocusedIndex = -1; // Start with no button focused
    currentFocusedMenu = null;

    if (currentState == State.MainMenuDisplayed && subState == SubState.None) {
      currentFocusedMenu = UIManager.Instance.mainMenu;
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = (lastMainMenuIndex >= 0 && lastMainMenuIndex < focusableControls.Count)
          ? lastMainMenuIndex
          : -1;
    } else if (currentState == State.InGameMenuDisplayed && subState == SubState.None) {
      currentFocusedMenu = UIManager.Instance.mainMenu; // Assuming you have an inGameMenu
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = (lastInGameMenuIndex >= 0 && lastInGameMenuIndex < focusableControls.Count)
          ? lastInGameMenuIndex
          : -1;

    } else if (currentState == State.MainMenuDisplayed && subState == SubState.LoadScreenDisplayed) {
      currentFocusedMenu = UIManager.Instance.saveGameScreen; // Assuming you have an inGameMenu
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = (lastInGameMenuIndex >= 0 && lastInGameMenuIndex < focusableControls.Count)
          ? lastInGameMenuIndex
          : -1;
    } else if (currentState == State.MainMenuDisplayed && subState == SubState.ExitGameConfirmationPopupDisplayed) {
      currentFocusedMenu = UIManager.Instance.mainMenu.ExitGameConfirmationPanel;
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = -1; // Focus on the first button in the submenu

    } else if (currentState == State.InGameMenuDisplayed && subState == SubState.ExitToMainMenuConfirmationPopupDisplayed) {
      currentFocusedMenu = UIManager.Instance.mainMenu.ExitToMainMenuPanel;
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = -1; // Focus on the first button in the submenu
    } else if ((currentState == State.MainMenuDisplayed || currentState == State.InGameMenuDisplayed) && subState == SubState.LanguageMenuDisplayed) {
      currentFocusedMenu = UIManager.Instance.mainMenu.LanguageOptionsContainer;
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = -1; // Focus on the first button in the submenu
    } else if (currentState == State.SplashScreenDisplayed && subState == SubState.None) {
      currentFocusedMenu = UIManager.Instance.splashScreen;
      focusableControls.Add(UIManager.Instance.splashScreen.backgroundTexture);
      currentFocusedIndex = 0;
    } else if (currentState == State.InDialogueMode && subState == SubState.None) {
      CollectInteractableUIElements(UIManager.Instance.inGameMenuButton);
      if (UIManager.Instance.dialogueBoxUI.Visible)
        currentFocusedMenu = DialogueManager.Instance.dialogueBoxUI;
      CollectInteractableUIElements(currentFocusedMenu);
      currentFocusedIndex = -1; // Focus on the first button in the submenu
    } else {
      // For any other state/substate combination, clear focus
      currentFocusedIndex = -1;
    }

    await ClearButtonHighlights();
  }

  // private void CollectFocusableMenuControls(Control container) {
  //     foreach (var child in container.GetChildren()) {
  //         if (child is Button button && button.Visible) {
  //             focusableControls.Add(button);
  //         } else if (child is Control control) {
  //             CollectFocusableMenuControls(control);
  //         }
  //     }
  // }


  private void CollectInteractableUIElements(Node node) {
    if (node is Control control && control is IInteractableUI && control.Visible == true) {
      focusableControls.Add(control);
    }

    foreach (var child in node.GetChildren()) {
      CollectInteractableUIElements(child);
    }
  }

  private List<Control> GetVisibleFocusableControls() {
    return focusableControls.FindAll(control => control.Visible);
  }


  //     private void HandleClick(Control clickedControl) {
  //     GD.Print($"Clicked on: {clickedControl.Name} (Type: {clickedControl.GetType().Name})");
  // }


  private async Task HandleMouseMotion(InputEventMouseMotion mouseMotion) {
    int newFocusedIndex = -1;
    for (int i = 0; i < focusableControls.Count; i++) {
      if (focusableControls[i] is IInteractableUI && focusableControls[i] is Control focusable
      && focusable.GetGlobalRect().HasPoint(GetGlobalMousePosition()) && focusable.Visible == true) {
        newFocusedIndex = i;
        currentFocusedIndex = newFocusedIndex;
        GD.Print($"newFocusedIndex: {newFocusedIndex}, control: {focusableControls[i].Name}");
        break;
      }
    }

    if (newFocusedIndex != currentFocusedIndex) {
      // Clear previous highlight
      if (currentFocusedIndex != -1 && currentFocusedIndex < focusableControls.Count) {
        await ApplyButtonStyle(focusableControls[currentFocusedIndex] as Button, false);
      }

      // Set new highlight
      currentFocusedIndex = newFocusedIndex;
      if (currentFocusedIndex != -1) {
        await ApplyButtonStyle(focusableControls[currentFocusedIndex] as Button, true);
      }
    }
  }

  private async Task ClearButtonHighlights() {
    foreach (var control in focusableControls) {
      if (control is Button button) {
        await ApplyButtonStyle(button, false);
        button.ReleaseFocus();
      }
    }
    await Task.CompletedTask;
  }


  private async Task HandleMouseClick() {

    if (currentFocusedIndex != -1 && currentFocusedIndex < focusableControls.Count) {
      await HandleMenuAccept();
    } else {
      // Check if the click is on any button, even if it wasn't the last hovered one
      for (int i = 0; i < focusableControls.Count; i++) {
        if (focusableControls[i] is IInteractableUI && focusableControls[i] is Control focusable
        && focusable.GetGlobalRect().HasPoint(GetGlobalMousePosition()) && focusable.Visible == true) {
          currentFocusedIndex = i;
          await HandleMenuAccept();
          return;
        }
      }
    }

    // If we reach here, the click was outside any menu button
    // No need to do anything, as Godot will handle clicks on other UI elements
  }

  private async Task HandleMenuInput(InputEvent @event) {
    float currentTime = (float)Time.GetTicksMsec() / 1000.0f;
    bool isVertical = GameStateManager.Instance.CurrentSubstate != SubState.ExitGameConfirmationPopupDisplayed &&
                      GameStateManager.Instance.CurrentSubstate != SubState.ExitToMainMenuConfirmationPopupDisplayed;

    if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left) {
      await HandleMouseClick();
      return;
    }

    if (isVertical) {
      if (CanProcessInput(currentTime) &&
          (Input.IsActionPressed("ui_up") ||
           (@event is InputEventJoypadMotion joypadMotionUp &&
            joypadMotionUp.Axis == JoyAxis.LeftY &&
            joypadMotionUp.AxisValue < -STICK_THRESHOLD))) {
        await HandleVerticalNavigation(true);
        lastInputTime = currentTime;
      } else if (CanProcessInput(currentTime) &&
                 (Input.IsActionPressed("ui_down") ||
                  (@event is InputEventJoypadMotion joypadMotionDown &&
                   joypadMotionDown.Axis == JoyAxis.LeftY &&
                   joypadMotionDown.AxisValue > STICK_THRESHOLD))) {
        await HandleVerticalNavigation(false);
        lastInputTime = currentTime;
      }
    } else {
      if (CanProcessInput(currentTime) &&
          (Input.IsActionPressed("ui_left") ||
           (@event is InputEventJoypadMotion joypadMotionLeft &&
            joypadMotionLeft.Axis == JoyAxis.LeftX &&
            joypadMotionLeft.AxisValue < -STICK_THRESHOLD))) {
        await HandleHorizontalNavigation(true);
        lastInputTime = currentTime;
      } else if (CanProcessInput(currentTime) &&
                 (Input.IsActionPressed("ui_right") ||
                  (@event is InputEventJoypadMotion joypadMotionRight &&
                   joypadMotionRight.Axis == JoyAxis.LeftX &&
                   joypadMotionRight.AxisValue > STICK_THRESHOLD))) {
        await HandleHorizontalNavigation(false);
        lastInputTime = currentTime;
      }
    }

    if (Input.IsActionJustPressed("ui_accept")) {
      await HandleMenuAccept();
    } else if (Input.IsActionJustPressed("ui_cancel")) {
      await HandleMenuCancel();
    }
  }

  private bool CanProcessInput(float currentTime) {
    return currentTime - lastInputTime >= INPUT_COOLDOWN;
  }


  private DateTime lastAcceptTime = DateTime.MinValue;
  private const int DEBOUNCE_MILLISECONDS = 500; // Adjust this value as needed

  //!-------------- HANDLE ACCEPT ---------------
  private async Task HandleMenuAccept() {
    DateTime now = DateTime.Now;
    if ((now - lastAcceptTime).TotalMilliseconds < DEBOUNCE_MILLISECONDS || isProcessingInput) {
      return;
    }

    isProcessingInput = true;

    if (currentFocusedIndex >= 0 && currentFocusedIndex < focusableControls.Count) {
      Control focusedControl = focusableControls[currentFocusedIndex];
      if (focusedControl is IInteractableUI interactable && focusedControl.Visible == true) {
        GD.Print($"about to execut Interact() from {focusedControl.Name}");
        await interactable.Interact();
        //await Task.Yield();

        isProcessingInput = false;

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
        await UIManager.Instance.inGameMenuButton.OnPressed();

      else if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed || GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed)
        if (GameStateManager.Instance.CurrentSubstate == SubState.LanguageMenuDisplayed)
          await UIManager.Instance.mainMenu.OnLanguagesGoBackButtonPressed();
    // else if(GameStateManager.Instance.CurrentSubstate == SubState.LoadScreenDisplayed)
    //     await UIManager.Instance.saveGameScreen.OnGoBackButtonPressed();
  }

  private async Task HandleVerticalNavigation(bool isUp) {
    var visibleControls = GetVisibleFocusableControls();
    if (visibleControls.Count == 0) return;

    int currentVisibleIndex = currentFocusedIndex != -1 ? visibleControls.IndexOf(focusableControls[currentFocusedIndex]) : -1;

    if (currentVisibleIndex == -1) {
      currentVisibleIndex = isUp ? visibleControls.Count - 1 : 0;
    } else {
      currentVisibleIndex = (currentVisibleIndex + (isUp ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
    }

    currentFocusedIndex = focusableControls.IndexOf(visibleControls[currentVisibleIndex]);
    GD.Print($"newFocusedIndex: {currentFocusedIndex}, control: {focusableControls[currentFocusedIndex].Name}");
    await HighlightMenuButton(currentFocusedIndex);
  }

  private async Task HandleHorizontalNavigation(bool isLeft) {
    var visibleControls = GetVisibleFocusableControls();
    if (visibleControls.Count == 0) return;

    int currentVisibleIndex = currentFocusedIndex != -1 ? visibleControls.IndexOf(focusableControls[currentFocusedIndex]) : -1;

    if (currentVisibleIndex == -1) {
      currentVisibleIndex = isLeft ? visibleControls.Count - 1 : 0;
    } else {
      currentVisibleIndex = (currentVisibleIndex + (isLeft ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
    }

    currentFocusedIndex = focusableControls.IndexOf(visibleControls[currentVisibleIndex]);
    GD.Print($"newFocusedIndex: {currentFocusedIndex}, control: {focusableControls[currentFocusedIndex].Name}");
    await HighlightMenuButton(currentFocusedIndex);
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


  private async Task HighlightMenuButton(int index) {
    for (int i = 0; i < focusableControls.Count; i++) {
      if (focusableControls[i] is Button button) {
        await ApplyButtonStyle(button, i == index);
      }
    }
  }

  private async Task ApplyButtonStyle(Button button, bool isHighlighted) {

    if (button == null) return;

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
    await Task.CompletedTask;
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