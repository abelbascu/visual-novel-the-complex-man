using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using State = GameStateMachine.State;
using SubState = GameStateMachine.SubState;
using System.Threading.Tasks;

public partial class InputManager : Control {
  public static InputManager Instance { get; private set; }
  //where we grab all the IInteractableUI controls
  private Control currentFocusedScene;
  //we need to know the game state to assign a currentFocusedScene
  private State currentState;
  private SubState currentSubstate;
  //store focusable UI controls here
  private List<Control> focusableUIControls = new List<Control>();
  //index of the control that has been clicked, pressed or selected
  private int currentFocusedIndex;
  //store the last index of the menu button that was focused so when we come back
  //from a submenu we can focus on the same button before we entered the submenu
  private int lastMainMenuIndex = -1;
  private int lastInGameMenuIndex = -1;
  //the same but come back to last player choice index 
  //that was focused before opening ingame menu
  private int lastPlayerChoiceIndex = -1;
  //add cooldown to avoid duplicated key or button press
  //both vars are used together in CanAcceptInput
  private float lastInputTime = 0f;
  private const float INPUT_COOLDOWN = 0.1f; // 200ms cooldown
  //both vars are for debounce for accept button
  private DateTime lastAcceptTime = DateTime.MinValue;
  private const int DEBOUNCE_MILLISECONDS = 200; // Adjust this value as needed
  //if gamepad stick moves this distance, register as movement
  private const float STICK_THRESHOLD = 0.4f;
  //NECESSARY onditiions to block further input
  public bool isProcessingInput = false; //DO NOT REMOVE
  private bool lastInputWasKeyboardOrGamepad = false;

  public override void _EnterTree() {
    if (Instance == null) {
      Instance = this;
    } else {
      QueueFree();
    }
  }

  public override void _ExitTree() {
    GameStateManager.Instance.StateChanged -= (prevState, prevSubState, newState, newSubState, args) => {
      _ = OnGameStateChanged(prevState, prevSubState, newState, newSubState, args);
    };
  }

  public override void _Ready() {
    //subcribe when game changes state, so we can refresh the UI focusableUIControls list
    GameStateManager.Instance.StateChanged += (prevState, prevSubState, newState, newSubState, args) => {
      _ = OnGameStateChanged(prevState, prevSubState, newState, newSubState, args);
    };
  }

  //try to prevent repeated fast input that would break the game state transitions
  private bool CanProcessInput(float currentTime) {
    return currentTime - lastInputTime >= INPUT_COOLDOWN;
  }

  public override async void _Input(InputEvent @event) {
    //if there is any pending input to process, do not accept more input
    if (InputBlocker.IsInputBlocked) {
      GetViewport().SetInputAsHandled();
      return;
      //isProcessingInput is a second necessary blocker to prevent unwanted input
    }
    if (isProcessingInput) {
      GetViewport().SetInputAsHandled();
      return;
    }
    //----------------- READ INPUT FROM MOUSE, GAMEPAD OR KEYBOARD -----------------------------------------------
    if (@event is InputEventMouseMotion) {
      HandleMouseMotion();
      GD.Print($"currentFocusIndex: {currentFocusedIndex}");
      return;
    }
    if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left && !InputBlocker.IsInputBlocked && !isProcessingInput) {
      lastInputWasKeyboardOrGamepad = false;
      isProcessingInput = true;
      await ProcessInputAsync(@event);
      AcceptEvent(); //stop propagating event
    } else if (!InputBlocker.IsInputBlocked && !isProcessingInput) {
      //gamepad and keyboard input
      if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_cancel") || @event.IsActionPressed("ui_left")
      || @event.IsActionPressed("ui_right") || @event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down")) {
        lastInputWasKeyboardOrGamepad = true;
        isProcessingInput = true;
        _ = ProcessInputAsync(@event);
        AcceptEvent(); //stop propagating event
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
      } else if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
        //await ToSignal(GetTree(), "process_frame"); //we give some time to process
        await HandleMenuInput(@event);
      } else if (GameStateManager.Instance.IsInState(State.EnterYourNameScreenDisplayed, SubState.None)) {
        //await ToSignal(GetTree(), "process_frame");
        if (@event.IsActionPressed("ui_accept")) {
          await HandleMenuInput(@event);
        }
      }
    } finally {
      isProcessingInput = false;
    }
  }

  private async Task HandleSplashScreenInput(InputEvent @event) {
    await InputBlocker.BlockNewInput(async () => {
      await UIManager.Instance.splashScreen.TransitionToMainMenu();
    });
    isProcessingInput = false;
  }

  private async Task OnGameStateChanged(GameStateMachine.State previousState, GameStateMachine.SubState previousSubstate,
                                  GameStateMachine.State newState, GameStateMachine.SubState newSubState, object[] arguments) {
    //every time that state changes, get the proper scene and refresh the UI focusableUIControls list 
    // Store the current menu or ingame menu button to hightlight it back when returning to it from a submenu (done in UpdateFocusableControls)
    //but do so only if we used keyboard or gamepad to enter submenu. For mouse in will depend what UI control the cursor is on.
    if (previousState == State.MainMenuDisplayed && previousSubstate == SubState.None) {
      lastMainMenuIndex = currentFocusedIndex;
    } else if (previousState == State.InGameMenuDisplayed && previousSubstate == SubState.None) {
      lastInGameMenuIndex = currentFocusedIndex;
    } else if (previousState == State.InDialogueMode && UIManager.Instance.playerChoicesBoxUI.Visible) {
      lastPlayerChoiceIndex = currentFocusedIndex;
    }

    currentState = newState;
    currentSubstate = newSubState;
    GD.Print($"In OnGameStateChanged() input manager, we now update FocusedControls for {newState}, {newSubState}");
    await UpdateFocusableControls();
    GD.Print("FocusableControlsUpdated");
    foreach (Control focusable in focusableUIControls)
      GD.Print($"{focusable.Name}");
  }

  //this is called when the game state changes, so we can update the focusableUIControls list
  public async Task UpdateFocusableControls() {
    focusableUIControls.Clear();
    currentFocusedIndex = -1; // Start with no button focused
    currentFocusedScene = null;
    //Splash screen
    if (currentState == State.SplashScreenDisplayed && currentSubstate == SubState.None) {
      currentFocusedScene = UIManager.Instance.splashScreen;
      focusableUIControls.Add(UIManager.Instance.splashScreen.backgroundTexture);
      currentFocusedIndex = 0;
      //Mainn menu
    } else if (currentState == State.MainMenuDisplayed && currentSubstate == SubState.None) {
      currentFocusedScene = UIManager.Instance.mainMenu;
      SetFocusableControls(currentFocusedScene);
      if (lastInputWasKeyboardOrGamepad && lastMainMenuIndex != -1 && lastMainMenuIndex < focusableUIControls.Count) {
        currentFocusedIndex = lastMainMenuIndex;
      } else { currentFocusedIndex = GetIndexOfControlUnderMouse(); }
      //Ingame menu
    } else if (currentState == State.InGameMenuDisplayed && currentSubstate == SubState.None) {
      SetFocusableControls(UIManager.Instance.inGameMenuButton);
      currentFocusedScene = UIManager.Instance.mainMenu;
      SetFocusableControls(currentFocusedScene);
      if (lastInputWasKeyboardOrGamepad && lastInGameMenuIndex != -1 && lastInGameMenuIndex < focusableUIControls.Count) {
        currentFocusedIndex = lastInGameMenuIndex;
      } else { currentFocusedIndex = GetIndexOfControlUnderMouse(); }
      //Load game screen
    } else if ((currentState == State.MainMenuDisplayed || currentState == State.InGameMenuDisplayed) && currentSubstate == SubState.LoadScreenDisplayed) {
      currentFocusedScene = UIManager.Instance.saveGameScreen;
      SetFocusableControls(currentFocusedScene);
      currentFocusedIndex = -1;
      //Save game screen
    } else if ((currentState == State.MainMenuDisplayed || currentState == State.InGameMenuDisplayed) && currentSubstate == SubState.SaveScreenDisplayed) {
      currentFocusedScene = UIManager.Instance.saveGameScreen;
      SetFocusableControls(currentFocusedScene);
      currentFocusedIndex = -1;
      //Exit game popup
    } else if (currentState == State.MainMenuDisplayed && currentSubstate == SubState.ExitGameConfirmationPopupDisplayed) {
      currentFocusedScene = UIManager.Instance.mainMenu.ExitGameConfirmationPanel;
      SetFocusableControls(currentFocusedScene);
      currentFocusedIndex = -1; // Focus on the first button in the submenu
      //Exit to main menu popup
    } else if (currentState == State.InGameMenuDisplayed && currentSubstate == SubState.ExitToMainMenuConfirmationPopupDisplayed) {
      currentFocusedScene = UIManager.Instance.mainMenu.ExitToMainMenuPanel;
      SetFocusableControls(currentFocusedScene);
      currentFocusedIndex = -1;
      //Language menu
      currentFocusedIndex = -1; // Focus on the first button in the submenu
    } else if ((currentState == State.MainMenuDisplayed || currentState == State.InGameMenuDisplayed) && currentSubstate == SubState.LanguageMenuDisplayed) {
      currentFocusedScene = UIManager.Instance.mainMenu.LanguageOptionsContainer;
      SetFocusableControls(currentFocusedScene);
      currentFocusedIndex = -1; // Focus on the first button in the submenu
      //Dialogue Mode
    } else if (currentState == State.InDialogueMode && currentSubstate == SubState.None) {
      SetFocusableControls(UIManager.Instance.inGameMenuButton);
      //get dialogue box
      if (UIManager.Instance.dialogueBoxUI.Visible)
        currentFocusedScene = DialogueManager.Instance.dialogueBoxUI;
      //get player choices box
      else if (UIManager.Instance.playerChoicesBoxUI.Visible)
        currentFocusedScene = DialogueManager.Instance.playerChoicesBoxUI;
      SetFocusableControls(currentFocusedScene);
      //enter your name screen (we won't use it for now)
    } else if (currentState == State.EnterYourNameScreenDisplayed) {
      SetFocusableControls(UIManager.Instance.inputNameScreen);
    } else {
      // For any other state/substate combination, clear focus
      currentFocusedIndex = -1;
    }

    if (currentFocusedIndex != -1) {
      await HighlightFocusableControl(currentFocusedIndex, true);
    } else {
      await ClearAllFocusableControlsHighlights();
    }
  }


  private List<Control> GetFocusableControls() {
    return focusableUIControls.FindAll(control => control.Visible);
  }


  private void SetFocusableControls(Node node) {
    //we pass the screen and get the focusable UI controls
    if (node is Control control) {
      if (control is IInteractableUI && control.Visible) {
        focusableUIControls.Add(control);
        GD.Print($"Focusable control: {control.Name}, Type: {control.GetType().Name}");
        return; // Stop traversing this control tree
      }
    }
    //we keep traversing the tree until we find a IInteractableUI or any control type specified above
    if (node != null) {
      foreach (var child in node.GetChildren()) {
        SetFocusableControls(child);
      }
    }
  }


  private int GetIndexOfControlUnderMouse() {
    for (int i = 0; i < focusableUIControls.Count; i++) {
      if (focusableUIControls[i] is Control focusable &&
          focusable.GetGlobalRect().HasPoint(GetGlobalMousePosition()) &&
          focusable.Visible) {
        return i;
      }
    }
    return -1;
  }

  private async void HandleMouseMotion() {
    //Ignore mouse motion in dialogue mode, we don´t want to change focus index by hovering
    //over the ingame menu icon. If we ui_accepted then we would not advance the dialogue
    if (GameStateManager.Instance.CurrentState == State.InDialogueMode) {
      return;
    }
    lastInputWasKeyboardOrGamepad = false;
    int newFocusedIndex = -1;
    for (int i = 0; i < focusableUIControls.Count; i++) {
      if (focusableUIControls[i] is IInteractableUI && focusableUIControls[i] is Control focusable
      && focusable.GetGlobalRect().HasPoint(GetGlobalMousePosition()) && focusable.Visible == true) {
        //don't assign directly to currentFocusedIndex, we may need to dehighlight the last focused control first
        newFocusedIndex = i;
        GD.Print($"newFocusedIndex: {newFocusedIndex}, control: {focusableUIControls[i].Name}");
        break;
      }
    }
    if (newFocusedIndex != currentFocusedIndex) {
      //dehighlight last focused control first
      if (currentFocusedIndex != -1 && currentFocusedIndex < focusableUIControls.Count) {
        await HighlightFocusableControl(currentFocusedIndex, false);

      }
      // Set new highlight
      if (newFocusedIndex != -1 && newFocusedIndex < focusableUIControls.Count) {
        currentFocusedIndex = newFocusedIndex;
        await HighlightFocusableControl(currentFocusedIndex, true);
      } else {
        currentFocusedIndex = -1;
      }
    }
  }

  private async Task HandleMouseClick() {
    for (int i = 0; i < focusableUIControls.Count; i++) {
      if (focusableUIControls[i] is IInteractableUI && focusableUIControls[i] is Control focusable
          && focusable.GetGlobalRect().HasPoint(GetGlobalMousePosition()) && focusable.Visible == true) {
        currentFocusedIndex = i;
        GD.Print($"clicked on currentFocusedIndex: {currentFocusedIndex}, control: {focusableUIControls[i].Name}");
        await HandleAcceptInputPressed();
        return;
      }
    }
  }

  //when we open the ingame menu, 
  public async Task SetCurrentFocusedUIControlIndexAfterClosingInGameMenu() {
    await UpdateFocusableControls();
    // If player choices are visible, focus on last saved player choice if it exists.
    if (UIManager.Instance.playerChoicesBoxUI.Visible) {
      if (lastPlayerChoiceIndex != -1) {
        currentFocusedIndex = lastPlayerChoiceIndex;
      }
    }
    // If dialogue box is visible, focus on it
    else if (UIManager.Instance.dialogueBoxUI.Visible) {
      for (int i = 0; i < focusableUIControls.Count; i++) {
        if (focusableUIControls[i] == UIManager.Instance.dialogueBoxUI.dialogueLineLabel) {
          currentFocusedIndex = i;
          break;
        }
      }
    }
    if (currentFocusedIndex != -1) {
      await HighlightFocusableControl(currentFocusedIndex, true);
    } else {
      //or clear all if mouse is not over any IInteractableUI control
      await ClearAllFocusableControlsHighlights();
    }
  }

  private async Task HandleMenuInput(InputEvent @event) {
    float currentTime = (float)Time.GetTicksMsec() / 1000.0f;
    bool isVertical = GameStateManager.Instance.CurrentSubstate != SubState.ExitGameConfirmationPopupDisplayed &&
                      GameStateManager.Instance.CurrentSubstate != SubState.ExitToMainMenuConfirmationPopupDisplayed &&
                      GameStateManager.Instance.CurrentState != State.EnterYourNameScreenDisplayed;
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
      await HandleAcceptInputPressed();
    } else if (Input.IsActionJustPressed("ui_cancel")) {
      await HandleMenuCancel();
    }
  }

  //!-------------- HANDLE ACCEPT ---------------
  private async Task HandleAcceptInputPressed() {
    DateTime now = DateTime.Now;
    if ((now - lastAcceptTime).TotalMilliseconds < DEBOUNCE_MILLISECONDS) {
      return;
    }
    isProcessingInput = true;
    Control focusedControl;
    if (lastInputWasKeyboardOrGamepad) {
      if (GameStateManager.Instance.CurrentState == State.InDialogueMode) {
        // For keyboard/gamepad input in dialogue mode, ensure we're focusing on tthe first
        //dialogue or playerchoice, not the ingame menu button
        //so if user pressed action we continue the dialogue NOT open the ingame menu
        if (currentFocusedIndex == 0 || currentFocusedIndex == -1) {
          await SetCurrentFocusedUIControlIndexAfterClosingInGameMenu();
        }
      }
      focusedControl = currentFocusedIndex >= 0 && currentFocusedIndex < focusableUIControls.Count
          ? focusableUIControls[currentFocusedIndex]
          : null;
    } else {
      //if the key press was not gamepad or keybaord, get the control under mouse cursor
      focusedControl = GetControlUnderMouse();
    }
    if (focusedControl != null && focusedControl.Visible) {
      // IInteractableUI interactable = null;
      IInteractableUI interactable = focusedControl as IInteractableUI;
      if (focusedControl is InGameMenuButton inGameMenuButton) {
        interactable = inGameMenuButton.textureButton;
      } else if (focusedControl is IInteractableUI) {
        interactable = (IInteractableUI)focusedControl;
      }

      if (interactable != null) {
        try {
          //in certain cases like player choices, the object will be destroyed
          //befor the await, so let's use strings of the object for after deletion logs
          string controlName = focusedControl.Name;
          string controlType = focusedControl.GetType().Name;
          lastAcceptTime = now;
          var tcs = new TaskCompletionSource<bool>();
          GD.Print($"About to execute Interact() from {controlName},{controlType}");
          // Start the interaction
          _ = interactable.Interact().ContinueWith(_ => tcs.TrySetResult(true));
          // Wait for the interaction to complete
          await tcs.Task;
          GD.Print($"We finished executing Interact() from {controlName},{controlType}");
          await Task.Yield();
        } finally {
          isProcessingInput = false;
        }
      }
    }
  }

  private Control GetControlUnderMouse() {
    for (int i = 0; i < focusableUIControls.Count; i++) {
      if (focusableUIControls[i] is Control focusable
          && focusable.GetGlobalRect().HasPoint(GetGlobalMousePosition()) && focusable.Visible == true) {
        return focusable;
      }
    }
    return null;
  }

  private async Task HandleMenuCancel() {
    if (InputBlocker.IsInputBlocked) {
      return;
    }
    //if the ingame menu is displayed, we close it or open it
    else if ((GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed || GameStateManager.Instance.CurrentState == State.InDialogueMode) && GameStateManager.Instance.CurrentSubstate == SubState.None) {
      //if (GameStateManager.Instance.CurrentSubstate == SubState.None)
      await UIManager.Instance.inGameMenuButton.OnPressed();
      return;
    } else if (GameStateManager.Instance.CurrentSubstate == SubState.LoadScreenDisplayed) {
      await UIManager.Instance.saveGameScreen.OnGoBackButtonPressed();
      return;
    } else if (GameStateManager.Instance.CurrentSubstate == SubState.SaveScreenDisplayed) {
      await UIManager.Instance.saveGameScreen.OnGoBackButtonPressed();
      return;

    } else if (GameStateManager.Instance.CurrentSubstate == SubState.LanguageMenuDisplayed) {
      //if (GameStateManager.Instance.CurrentSubstate == SubState.None)
      await UIManager.Instance.mainMenu.OnLanguagesGoBackButtonPressed();
      return;
    } else if (GameStateManager.Instance.CurrentSubstate == SubState.ExitToMainMenuConfirmationPopupDisplayed) {
      await UIManager.Instance.mainMenu.OnExitToMainMenuCancelButtonPressed();
      return;
    } else if (GameStateManager.Instance.CurrentSubstate == SubState.ExitGameConfirmationPopupDisplayed) {
      await UIManager.Instance.mainMenu.OnExitGameCancelButtonPressed();
      return;
    }
  }

  private async Task HandleVerticalNavigation(bool isUp) {
    var visibleControls = GetFocusableControls();
    if (visibleControls.Count == 0) return;
    int newIndex = GetNextValidFocusableControlIndex(isUp);
    if (newIndex != -1 && newIndex != currentFocusedIndex) {
      currentFocusedIndex = focusableUIControls.IndexOf(visibleControls[newIndex]);
      GD.Print($"newFocusedIndex: {currentFocusedIndex}, control: {focusableUIControls[currentFocusedIndex].Name}");
      await HighlightFocusableControl(currentFocusedIndex, true);
      // Scroll to the newly focused control if it's an InteractableUIButton within a SaveGameSlot
      if (focusableUIControls[currentFocusedIndex] is InteractableUIButton button) {
        var saveGameSlot = FindSaveGameSlotParent(button);
        if (saveGameSlot != null) {
          var saveGameScreen = UIManager.Instance.saveGameScreen;
          saveGameScreen.ScrollToControl(saveGameSlot);
        }
      }
    }
  }

  private SaveGameSlot FindSaveGameSlotParent(Node node) {
    if (node == null) return null;
    if (node is SaveGameSlot saveGameSlot) return saveGameSlot;
    return FindSaveGameSlotParent(node.GetParent());
  }

  private bool IsValidButtonForNavigation(Control control) {
    // Add any additional conditions here if needed
    return control.Visible;
  }

  private int GetNextValidFocusableControlIndex(bool isUp) {
    var visibleControls = GetFocusableControls();
    bool isInGameMenuVisible = UIManager.Instance.inGameMenuButton.Visible;
    bool isPlayerChoicesVisible = UIManager.Instance.playerChoicesBoxUI.Visible;
    // If no controls are visible, return -1
    if (visibleControls.Count == 0) return -1;

    int count = visibleControls.Count;
    int currentIndex;
    if (currentFocusedIndex == -1) {
      // If no button is currently focused, select the first or last valid button
      currentIndex = isUp ? count - 1 : 0;
      // Skip in-game menu button if necessary
      if (currentIndex == 0 && isInGameMenuVisible) {
        currentIndex = 1;
      }
    }
    // If a button is focused, start from the current position
    else {
      currentIndex = visibleControls.IndexOf(focusableUIControls[currentFocusedIndex]);
      // Move to next/previous button
      if (isUp) {
        currentIndex = (currentIndex - 1 + count) % count;
      } else {
        currentIndex = (currentIndex + 1) % count;
      }
    }
    // Find the next valid button
    int startIndex = currentIndex;
    do {
      // Skip the in-game menu button if it's visible and we're not in player choices
      if (currentIndex == 0 && isInGameMenuVisible) {
        currentIndex = isUp ? count - 1 : 1;
      }
      //It checks if the current button is valid for navigation.
      //If it's valid, it returns that index immediately.
      if (IsValidButtonForNavigation(visibleControls[currentIndex])) {
        return currentIndex;
      }
      //If it's not valid, it moves to the next button in the specified direction.
      if (isUp) {
        currentIndex = (currentIndex - 1 + count) % count;
      } else {
        currentIndex = (currentIndex + 1) % count;
      }
      //It keeps doing this until it finds a valid button or has checked all buttons.
      // If we've looped back to the start index, break to avoid infinite loop
      if (currentIndex == startIndex) break;
    } while (true);
    // If no valid button found, return -1
    return -1;
  }


  private async Task HandleHorizontalNavigation(bool isLeft) {
    var visibleControls = GetFocusableControls();
    if (visibleControls.Count == 0) return;
    int currentVisibleIndex = currentFocusedIndex != -1 ? visibleControls.IndexOf(focusableUIControls[currentFocusedIndex]) : -1;
    if (currentVisibleIndex == -1) {
      currentVisibleIndex = isLeft ? visibleControls.Count - 1 : 0;
    } else {
      currentVisibleIndex = (currentVisibleIndex + (isLeft ? -1 : 1) + visibleControls.Count) % visibleControls.Count;
    }
    currentFocusedIndex = focusableUIControls.IndexOf(visibleControls[currentVisibleIndex]);
    GD.Print($"newFocusedIndex: {currentFocusedIndex}, control: {focusableUIControls[currentFocusedIndex].Name}");
    await HighlightFocusableControl(currentFocusedIndex, true);
  }

  private async Task HighlightFocusableControl(int index, bool isHighlighted) {
    for (int i = 0; i < focusableUIControls.Count; i++) {
      if (focusableUIControls[i] is PlayerChoiceButton playerChoiceButton) {
        playerChoiceButton.ApplyStyle(i == index && isHighlighted);
      } else if (focusableUIControls[i] is InteractableUIButton button) {
        await ApplyButtonStyle(button, i == index && isHighlighted);
      }
    }
  }

  private async Task ClearAllFocusableControlsHighlights() {
    foreach (var control in focusableUIControls) {
      if (control is InteractableUIButton button) {
        await ApplyButtonStyle(button, false);
        button.ReleaseFocus();
      } else if (control is PlayerChoiceButton playerChoiceButton) {
        playerChoiceButton.ApplyStyle(false);
      }
    }
    await Task.CompletedTask;
  }

  private async Task ApplyButtonStyle(InteractableUIButton button, bool isHighlighted) {
    if (button == null) return;
    if (isHighlighted) {
      var hoverStyle = UIThemeHelper.GetPixelArtHoverStyleBox();
      button.AddThemeStyleboxOverride("normal", hoverStyle);
      button.AddThemeStyleboxOverride("hover", hoverStyle);
      button.AddThemeStyleboxOverride("focus", hoverStyle);
    } else {
      var normalStyle = UIThemeHelper.GetPixelArtNormalStyleBox();
      button.AddThemeStyleboxOverride("normal", normalStyle);
      button.AddThemeStyleboxOverride("hover", normalStyle);
      button.AddThemeStyleboxOverride("focus", normalStyle);
    }
    await Task.CompletedTask;
  }
}

