using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static GameStateMachine;

public partial class MainMenu : Control {

  //UIElements
  public Panel ExitGameConfirmationPanel;
  public Panel ExitToMainMenuPanel;
  public ConfirmationDialog creditsConfirmationDialog;
  public VBoxContainer MainOptionsContainer;
  public VBoxContainer LanguageOptionsContainer;
  public TextureRect mainMenuBackgroundImage;

  //Buttons
  InteractableUIButton startNewGameButton;
  InteractableUIButton saveGameButton;
  InteractableUIButton continueGameButton;
  InteractableUIButton loadGameButton;
  InteractableUIButton languageButton;
  InteractableUIButton creditsButton;
  InteractableUIButton exitGameButton;
  InteractableUIButton exitToMainMenuButton;
  InteractableUIButton settingsButton;
  InteractableUIButton languagesGoBackButton;
  InteractableUIButton YesExitGameButton;
  InteractableUIButton NoExitGameButton;
  InteractableUIButton YesExitToMainMenuButton;
  InteractableUIButton NoExitToMainMenuButton;
  InteractableUIButton englishButton;
  InteractableUIButton frenchButton;
  InteractableUIButton catalanButton;

  //Button containers
  public HBoxContainer YesNoButtonsHBoxContainer;
  private HBoxContainer YesNoExitToMenuButtonsHBoxContainer;

  //Constants
  public bool LOAD_SCREEN = true;
  public bool SAVE_SCREEN = false;

  //Events
  public Action MainMenuOpened;
  public Action InGameMenuOpened;
  public Action MainMenuClosed;
  public Action InGameMenuClosed;
  public Action StartNewGameButtonPressed;
  public Action SaveGameButtonPressed;
  public Action LoadGameButtonPressed;

  //Translations
  private string wantToQuitGameTRANSLATE = "WANT_QUIT_GAME?";
  private string wantToQuitToMainMenuTRANSLATE = "WANT_QUIT_TO_MAIN_MENU?";

  //Text labels
  private RichTextLabel WantToQuitGameLabel;
  private RichTextLabel WantToQuitToMainMenuLabel;

  public InputBlocker inputBlocker { get; private set; }




  public override void _Ready() {

    inputBlocker = new InputBlocker();

    GetUINodes();
    SetupButtonEvents();
    ApplyCustomStyles();

    //assign the loc keys so each time the language changes the text property is updated
    WantToQuitGameLabel.Text = wantToQuitGameTRANSLATE;
    WantToQuitToMainMenuLabel.Text = wantToQuitToMainMenuTRANSLATE;

    // GameStateManager.Instance.StateChanged += OnGameStateChanged;
  }

  //  private void OnGameStateChanged(GameStateMachine.State previousState, GameStateMachine.SubState previousSubstate, 
  //                                 GameStateMachine.State newState, GameStateMachine.SubState newSubState, object[] arguments)
  // {
  //     if (newState == GameStateMachine.State.MainMenuDisplayed || 
  //         newState == GameStateMachine.State.InGameMenuDisplayed)
  //     {
  //         InputManager.Instance.SetInitialFocus();
  //     }
  // }

  private void GetUINodes() {

    //main buttons container
    MainOptionsContainer = GetNode<VBoxContainer>("MainOptionsContainer");

    //main buttons
    startNewGameButton = GetNode<InteractableUIButton>("MainOptionsContainer/StartNewGameButton");
    saveGameButton = GetNode<InteractableUIButton>("MainOptionsContainer/SaveGameButton"); ;
    continueGameButton = GetNode<InteractableUIButton>("MainOptionsContainer/ContinueButton"); ;
    loadGameButton = GetNode<InteractableUIButton>("MainOptionsContainer/LoadGameButton");
    languageButton = GetNode<InteractableUIButton>("MainOptionsContainer/LanguageButton");
    settingsButton = GetNode<InteractableUIButton>("MainOptionsContainer/SettingsButton"); ;
    creditsButton = GetNode<InteractableUIButton>("MainOptionsContainer/CreditsButton");
    exitToMainMenuButton = GetNode<InteractableUIButton>("MainOptionsContainer/ExitToMainMenuButton");
    exitGameButton = GetNode<InteractableUIButton>("MainOptionsContainer/ExitGameButton");

    //exit game panel
    ExitGameConfirmationPanel = GetNode<Panel>("%ExitGameConfirmationPanel");
    YesExitGameButton = GetNode<InteractableUIButton>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer/YesExitGameButton");
    NoExitGameButton = GetNode<InteractableUIButton>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer/NoExitGameButton");
    WantToQuitGameLabel = GetNode<RichTextLabel>("ExitGameConfirmationPanel/VBoxContainer/MarginContainer/WantToQuitGameLabel");
    YesNoButtonsHBoxContainer = GetNode<HBoxContainer>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer");

    //exit to main menu panel
    ExitToMainMenuPanel = GetNode<Panel>("ExitToMainMenuPanel");
    YesExitToMainMenuButton = GetNode<InteractableUIButton>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer/YesExitToMainMenuButton");
    NoExitToMainMenuButton = GetNode<InteractableUIButton>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer/NoExitToMainMenuButton");
    WantToQuitToMainMenuLabel = GetNode<RichTextLabel>("ExitToMainMenuPanel/VBoxContainer/MarginContainer/WantToExitToMainMenuLabel");
    YesNoExitToMenuButtonsHBoxContainer = GetNode<HBoxContainer>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer");

    //languages panel
    LanguageOptionsContainer = GetNode<VBoxContainer>("LanguageOptionsContainer");
    englishButton = GetNode<InteractableUIButton>("LanguageOptionsContainer/EnglishButton");
    frenchButton = GetNode<InteractableUIButton>("LanguageOptionsContainer/FrenchButton");
    catalanButton = GetNode<InteractableUIButton>("LanguageOptionsContainer/CatalanButton");
    languagesGoBackButton = GetNode<InteractableUIButton>("LanguageOptionsContainer/GoBackButton");

    //credits
    creditsConfirmationDialog = GetNode<ConfirmationDialog>("CreditsConfirmationDialog");

    //main menu background image
    mainMenuBackgroundImage = GetNode<TextureRect>("BackgroundImage");
  }

  private void SetupButtonEvents() {

    //main buttons events
    startNewGameButton.Pressed += () => _ = OnStartNewGameButtonPressed();
    continueGameButton.Pressed += () => _ = OnContinueButtonPressed();
    saveGameButton.Pressed += () => _ = OnSaveGameButtonPressed();
    loadGameButton.Pressed += () => _ = OnLoadGameButtonPressed();
    languageButton.Pressed += () => _ = OnLanguageButtonPressed();
    creditsButton.Pressed += () => _ = OnCreditsButtonPressed();
    exitToMainMenuButton.Pressed += OnExitToMainMenuButtonPressed;
    exitGameButton.Pressed += () => _ = OnExitGameButtonPressed();

    //exit to main menu events
    YesExitToMainMenuButton.Pressed += () => _ = OnExitToMainMenuConfirmButtonPressed();
    NoExitToMainMenuButton.Pressed += () => _ = OnExitToMainMenuCancelButtonPressed();

    //exit game events
    NoExitGameButton.Pressed += () => _ = OnExitGameCancelButtonPressed();
    YesExitGameButton.Pressed += () => _ = OnExitGameConfirmButtonPressed();

    //credits events
    creditsConfirmationDialog.Canceled += () => _ = OnCreditsCancelOrConfirmButtonPressed();
    creditsConfirmationDialog.Confirmed += () => _ = OnCreditsCancelOrConfirmButtonPressed();

    //languages events
    englishButton.Pressed += OnEnglishButtonPressed;
    frenchButton.Pressed += OnFrenchButtonPressed;
    catalanButton.Pressed += OnCatalanButtonPressed;
    languagesGoBackButton.Pressed += () => _ = OnLanguagesGoBackButtonPressed();
  }

  private void ApplyCustomStyles() {

    SetFullRect();
    EnableRichTextLabels();
    StyleHBoxContainers();
    ApplyCustomStyleToAllButtons();
    StylePanelsAndDialogs();
  }

  private void SetFullRect() {
    AnchorRight = 1;
    AnchorBottom = 1;
    OffsetRight = 0;
    OffsetBottom = 0;
  }

  private void EnableRichTextLabels() {
    WantToQuitGameLabel.BbcodeEnabled = true;
    WantToQuitToMainMenuLabel.BbcodeEnabled = true;
  }

  private void StyleHBoxContainers() {
    StyleHBoxContainer(YesNoButtonsHBoxContainer);
    StyleHBoxContainer(YesNoExitToMenuButtonsHBoxContainer);
  }

  private void StyleHBoxContainer(HBoxContainer container) {
    container.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
    container.AddThemeConstantOverride("margin_left", 20);
    container.AddThemeConstantOverride("margin_right", 20);
    container.AddThemeConstantOverride("margin_bottom", 50);
    container.AddThemeConstantOverride("separation", 20);
  }

  private void ApplyCustomStyleToAllButtons() {
    //apply a default blue style with white rounded edges for panels and buttons
    ApplyCustomStyleToButtonsInContainer(MainOptionsContainer);
    ApplyCustomStyleToButtonsInContainer(LanguageOptionsContainer);
    ApplyCustomStyleToButtonsInContainer(ExitToMainMenuPanel);
    ApplyCustomStyleToButtonsInContainer(ExitGameConfirmationPanel);
  }

  private void StylePanelsAndDialogs() {
    UIThemeHelper.ApplyCustomStyleToPanel(ExitToMainMenuPanel);
    UIThemeHelper.ApplyCustomStyleToPanel(ExitGameConfirmationPanel);
    UIThemeHelper.ApplyCustomStyleToWindowDialog(creditsConfirmationDialog);
  }

  private void ApplyCustomStyleToButtonsInContainer(Control container) {
    foreach (var child in container.GetChildren()) {
      if (child is InteractableUIButton button) {
        UIThemeHelper.ApplyCustomStyleToButton(button);
      } else if (child is Control) {
        // Recursively apply style to children of this control
        ApplyCustomStyleToButtonsInContainer(child as Control);
      }
    }
  }

  //when user selects language on main menu, this is called
  public async Task UpdateTextsBasedOnLocale(string language) {

    TranslationServer.SetLocale(language);
    //put language buttons in grey
    await SetLanguageButtonsVisualStateAsync(false);

    //then disable them
    foreach (InteractableUIButton button in LanguageOptionsContainer.GetChildren()) {
      SetButtonActiveState(button, false);
    }

    //if there is a dialogue on screen, translate it as soon as language is changed
    if (UIManager.Instance.dialogueBoxUI.IsVisibleInTree()) {
      DialogueManager.Instance.DisplayDialogue(DialogueManager.Instance.currentDialogueObject);
      await WaitForDialogueCompletion();
    }

    //if there are player choices on screen, translate them as soon as language is changed
    if (UIManager.Instance.playerChoicesBoxUI.IsVisibleInTree()) {
      UIManager.Instance.playerChoicesBoxUI.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, TranslationServer.GetLocale());
      await WaitForPlayerChoiceCompletion();
    }

    //enable language buttons again
    foreach (InteractableUIButton button in LanguageOptionsContainer.GetChildren()) {
      SetButtonActiveState(button, true);
    }

    //language buttons to to default color
    await SetLanguageButtonsVisualStateAsync(true);
  }

  //while dialogue is still being displayed to the new language, wait
  private async Task WaitForDialogueCompletion() {
    while (DialogueManager.Instance.isDialogueBeingPrinted) {
      await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
  }

  //while player choices is still being displayed to the new language, wait
  private async Task WaitForPlayerChoiceCompletion() {
    while (DialogueManager.Instance.IsPlayerChoiceBeingPrinted) {
      await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
  }

  private void SetLanguageButtonsVisualState(bool enabled) {
    foreach (Button button in LanguageOptionsContainer.GetChildren()) {
      button.Modulate = enabled ? Colors.White : Colors.Gray;
    }
  }

  private async Task SetLanguageButtonsVisualStateAsync(bool enabled) {
    SetLanguageButtonsVisualState(enabled);
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
  }


  //ENABLE-DISABLE BUTTONS METHODS
  //when the user clicks on any menu button, let's disable all buttons
  //to prevent race conditions and breaking the state machine.
  public void DisableAllButtons() => SetAllButtonsState(false);
  private void EnableAllButtons() => SetAllButtonsState(true);

  private void SetAllButtonsState(bool enabled) {
    SetButtonActiveState(continueGameButton, enabled);
    SetButtonActiveState(loadGameButton, enabled);
    SetButtonActiveState(saveGameButton, enabled);
    SetButtonActiveState(startNewGameButton, enabled);
    SetButtonActiveState(languageButton, enabled);
    SetButtonActiveState(exitGameButton, enabled);
    SetButtonActiveState(creditsButton, enabled);
    SetButtonActiveState(exitToMainMenuButton, enabled);
    SetButtonActiveState(settingsButton, enabled);
  }


  private void SetAllButtonsVisibility(bool enabled) {
    SetButtonVisibiityState(continueGameButton, enabled);
    SetButtonVisibiityState(loadGameButton, enabled);
    SetButtonVisibiityState(saveGameButton, enabled);
    SetButtonVisibiityState(startNewGameButton, enabled);
    SetButtonVisibiityState(languageButton, enabled);
    SetButtonVisibiityState(exitGameButton, enabled);
    SetButtonVisibiityState(creditsButton, enabled);
    SetButtonVisibiityState(exitToMainMenuButton, enabled);
    SetButtonVisibiityState(settingsButton, enabled);
  }



  private void SetButtonVisibiityState(InteractableUIButton button, bool enable) {


    button.Visible = enable;
  }


  private void SetButtonActiveState(InteractableUIButton button, bool enable) {
    button.SetProcessInput(enable);
    button.MouseFilter = enable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    button.FocusMode = enable ? FocusModeEnum.All : FocusModeEnum.None;
    //button.Visible = enable;
  }

  public void SetContainerButtonsVisibility(Control container, bool isButtonsVisible) {

    foreach (var child in container.GetChildren()) {
      if (child is InteractableUIButton button) {
        button.Visible = isButtonsVisible;
        GD.Print($"{button.Name} is now Visible:{button.Visible}");
      } else if (child is Control control)
        SetContainerButtonsVisibility(control, isButtonsVisible);
    }
  }


  public async Task DisplayMainMenuContainerOnly() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      //this calls to pause the game timer, more sense on ingame menu
      //but can be used to autosave game while user exits current game
      SetupForMainMenu();
      await UIFadeHelper.FadeInControl(MainOptionsContainer);
      EnableAllButtons();
      MainMenuOpened?.Invoke();
      GameStateManager.Instance.Fire(Trigger.MAIN_MENU_DISPLAYED);
    });
  }

  public async Task DisplayMainMenu() {

    DisableAllButtons();
    SetupMainMenuBackground();
    SetupForMainMenu();
    await FadeInMainMenu();
    //EnableAllButtons(); WE WERE ENABLING SOME HIDDEN BUTTONS AND THIS AFFECTED THE INPUT MANAGER READING HOW MANY BUTTONS WERE AVAILABLE!
    //this calls to pause the game timer, more sense on ingame menu
    //but can be used to autosave game while user exits current game

    GameStateManager.Instance.Fire(Trigger.MAIN_MENU_DISPLAYED);
    MainMenuOpened?.Invoke();
  }

  private void SetupForMainMenu() {
    SetContainerButtonsVisibility(LanguageOptionsContainer, false);
    SetContainerButtonsVisibility(ExitGameConfirmationPanel, false);
    SetContainerButtonsVisibility(ExitToMainMenuPanel, false);
    SetContainerButtonsVisibility(MainOptionsContainer, true);

    SetupMainMenuButtonVisibility();

    SetDialogueUIElementsTopLevel(false);

    ClearPendingInputEvents();

    //only displayed in dialogue mode, or future game modes
    UIManager.Instance.inGameMenuButton.Hide();
    //a mask to avoid clicking on the dialoguebox/player choices
    //when ingame menu is open. If in main menu, disable it.
    UIManager.Instance.menuOverlay.Visible = false;
    SetMainMenuUIMainComponentsVisibility();
  }

  private void SetMainMenuUIMainComponentsVisibility() {
    MainOptionsContainer.TopLevel = true;
    MainOptionsContainer.Show();
    mainMenuBackgroundImage.Show();
    Show();
  }

  private void SetDialogueUIElementsTopLevel(bool isTopLevel) {
    //put interfering UI elements behind
    //Dialogue Mode UI elements
    UIManager.Instance.dialogueBoxUI.TopLevel = false;
    UIManager.Instance.playerChoicesBoxUI.TopLevel = false;
  }

  private void ClearPendingInputEvents() {
    GetViewport().SetInputAsHandled();
  }

  private void SetupMainMenuBackground() {
    //if we come back from a current play, remove the dialogue mode image
    VisualManager.Instance.RemoveImage();
    //then load the main menu own image
    mainMenuBackgroundImage.Texture = GD.Load<Texture2D>("res://Visuals/splash screen the dragon riddle.png");
    mainMenuBackgroundImage.Modulate = new Color(1, 1, 1, 1);  // This sets it to ffffff (fully opaque)
    mainMenuBackgroundImage.SetAnchorsPreset(LayoutPreset.FullRect);
    mainMenuBackgroundImage.Visible = true;
  }

  private void SetupMainMenuButtonVisibility() {
    //specific main menu buttons, not ingame menu
    startNewGameButton.Show();
    loadGameButton.Show();
    languageButton.Show();
    settingsButton.Show();
    creditsButton.Show();
    exitGameButton.Show();

    saveGameButton.Hide();
    continueGameButton.Hide();
    exitToMainMenuButton.Hide();
  }



  public async Task CloseMainMenu() {
    DisableAllButtons();
    MainOptionsContainer.SetProcessInput(false);
    await FadeOutMainMenu();
    //this calls to resume the game timer
    MainMenuClosed?.Invoke();
  }

  public async Task FadeInMainMenu() {
    await Task.WhenAll(
        UIFadeHelper.FadeInControl(MainOptionsContainer, 1.0f),
        UIFadeHelper.FadeInControl(mainMenuBackgroundImage, 1.0f));
  }

  private async Task FadeOutMainMenu() {
    await Task.WhenAll(
        UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.7f),
        UIFadeHelper.FadeOutControl(mainMenuBackgroundImage, 0.7f));
  }

  public async Task DisplayInGameMenu() {

    await inputBlocker.BlockNewInput(async () => {

      SetContainerButtonsVisibility(LanguageOptionsContainer, false);
      SetContainerButtonsVisibility(ExitGameConfirmationPanel, false);
      SetContainerButtonsVisibility(ExitToMainMenuPanel, false);
      SetContainerButtonsVisibility(MainOptionsContainer, false);

      SetupIngameMenuButtonVisibility();

      DisableAllButtons();
      SetupUIForInGameMenu();
      MainOptionsContainer.Show();
      MainOptionsContainer.Modulate = new Color(1, 1, 1, 1);  // Full opacity
      MainOptionsContainer.TopLevel = true;
      await UIFadeHelper.FadeInControl(MainOptionsContainer, 0.6f);
      MainOptionsContainer.SetProcessInput(true);
      EnableAllButtons();
      ShowIngameMenuIcon();
      GameStateManager.Instance.Fire(Trigger.INGAME_MENU_DISPLAYED);
      InGameMenuOpened?.Invoke();
    });
  }


  private void SetupIngameMenuButtonVisibility() {
    //specific main menu buttons, not ingame menu

    continueGameButton.Show();
    saveGameButton.Show();
    loadGameButton.Show();
    languageButton.Show();
    settingsButton.Show();
    exitToMainMenuButton.Show();

    startNewGameButton.Hide();
    creditsButton.Hide();
    exitGameButton.Hide();

  }

  private void SetupUIForInGameMenu() {

    SetDialogueUIElementsTopLevel(false);
    MainOptionsContainer.SetProcessInput(false);
    Show();
    SetInGameMenuButtonVisibility();
    mainMenuBackgroundImage.Texture = null;
    //put overlay to prevent reading input from other UI elements behind this mask
    UIManager.Instance.menuOverlay.Visible = true;
  }

  private void SetInGameMenuButtonVisibility() {
    saveGameButton.Visible = true;
    continueGameButton.Visible = true;
    exitToMainMenuButton.Visible = true;
    startNewGameButton.Visible = false;
    exitGameButton.Visible = false;
  }

  public void HideIngameMenuIcon() => UIManager.Instance.inGameMenuButton.Visible = false;
  public void ShowIngameMenuIcon() => UIManager.Instance.inGameMenuButton.Visible = true;


  public async Task CloseInGameMenu() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      MainOptionsContainer.SetProcessInput(false);
      UIManager.Instance.menuOverlay.Visible = false;
      await UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.6f);
      MainOptionsContainer.Visible = false;
      InGameMenuClosed?.Invoke();
    });

  }

  //Start new game
  private async Task OnStartNewGameButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      HideIngameMenuIcon();
      await FadeOutMainMenu();
      StartNewGameButtonPressed.Invoke();
      GameStateManager.Instance.Fire(Trigger.START_NEW_GAME);
    });
  }

  //Continue game
  private async Task OnContinueButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      ShowIngameMenuIcon();
      await CloseInGameMenu();
      GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
      SetButtonActiveState(continueGameButton, true);
    });
  }

  //Save game
  private async Task OnSaveGameButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      HideIngameMenuIcon();
      await CloseInGameMenu();
      GD.Print("In OnSaveGameButtonPressed triggering INITIALIZE_SAVE_SCREEN ");
      GameStateManager.Instance.Fire(Trigger.INITIALIZE_SAVE_SCREEN);
    });
    GD.Print("We quit OnSaveGameButtonPressed");
  }

  //Load Game
  private async Task OnLoadGameButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      HideIngameMenuIcon();
      DisableAllButtons();
      GD.Print("In OnLoadGameButtonPressed triggering INITIALIZE_LOAD_SCREEN ");
      GameStateManager.Instance.Fire(Trigger.INITIALIZE_LOAD_SCREEN);
    });
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
     await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
     await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    await ToSignal(GetTree(), "process_frame");
    GD.Print("We quit OnLoadGameButtonPressed");
  }

  //Language options
  private async Task OnLanguageButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      HideIngameMenuIcon();
      DisableAllButtons();
      await UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.6f);
      SetContainerButtonsVisibility(MainOptionsContainer, false);
      SetContainerButtonsVisibility(LanguageOptionsContainer, true);
      GameStateManager.Instance.Fire(Trigger.DISPLAY_LANGUAGE_MENU);
      EnableAllButtons();
    });
  }

  public async Task DisplayLanguageMenu() {
    await inputBlocker.BlockNewInput(async () => {
      LanguageOptionsContainer.Visible = true;
      LanguageOptionsContainer.TopLevel = true;
      await UIFadeHelper.FadeInControl(LanguageOptionsContainer);
    });
  }

  public async Task OnLanguagesGoBackButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      if (GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed)
        ShowIngameMenuIcon();

      SetButtonActiveState(languagesGoBackButton, false);
      GetTree().CallGroup("popups", "close_all");
      await UIFadeHelper.FadeOutControl(LanguageOptionsContainer, 0.6f);

      SetButtonActiveState(languagesGoBackButton, true);
      DisableAllButtons();

      if (GameStateManager.Instance.CurrentState == State.MainMenuDisplayed)
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
      else if (GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed)
        GameStateManager.Instance.Fire(Trigger.DISPLAY_INGAME_MENU);
      //EnableAllButtons();
    });
  }

  private async void OnEnglishButtonPressed() => await UpdateTextsBasedOnLocale("en");
  private async void OnFrenchButtonPressed() => await UpdateTextsBasedOnLocale("fr");
  private async void OnCatalanButtonPressed() => await UpdateTextsBasedOnLocale("ca");

  //Credits
  private async Task OnCreditsButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      HideIngameMenuIcon();
      DisableAllButtons();
      await UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f);
      GameStateManager.Instance.Fire(Trigger.DISPLAY_CREDITS);
      creditsConfirmationDialog.Show();
      await UIFadeHelper.FadeInWindow(creditsConfirmationDialog, 0.4f);
    });
  }

  private async Task OnCreditsCancelOrConfirmButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      await UIFadeHelper.FadeOutWindow(creditsConfirmationDialog, 0.2f);
      //EnableAllButtons();
      GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
    });
  }

  //Exit to Main Menu
  public async void OnExitToMainMenuButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      HideIngameMenuIcon();
      await ShowExitToMainMenuConfirmationPopup();
    });
  }

  public async Task ShowExitToMainMenuConfirmationPopup() {
    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      SetButtonActiveState(NoExitToMainMenuButton, false);
      SetContainerButtonsVisibility(MainOptionsContainer, false);
      await UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f);
      SetButtonActiveState(YesExitToMainMenuButton, false);
      ExitToMainMenuPanel.Visible = true;
      ExitToMainMenuPanel.TopLevel = true;
      SetContainerButtonsVisibility(YesNoExitToMenuButtonsHBoxContainer, true);
      await UIFadeHelper.FadeInControl(ExitToMainMenuPanel, 0.5f);
      SetButtonActiveState(NoExitToMainMenuButton, true);
      SetButtonActiveState(YesExitToMainMenuButton, true);
      GameStateManager.Instance.Fire(Trigger.DISPLAY_EXIT_TO_MAIN_MENU_CONFIRMATION_POPUP);
    });
  }

  private async Task OnExitToMainMenuCancelButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      SetButtonActiveState(NoExitToMainMenuButton, false);
      SetButtonActiveState(YesExitToMainMenuButton, false);
      await UIFadeHelper.FadeOutControl(ExitToMainMenuPanel, 0.6f);
      ShowIngameMenuIcon();
      SetButtonActiveState(NoExitToMainMenuButton, false);
      SetButtonActiveState(YesExitToMainMenuButton, false);
      GameStateManager.Instance.Fire(Trigger.DISPLAY_INGAME_MENU);
      EnableAllButtons();
    });
  }

  private async Task OnExitToMainMenuConfirmButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      SetButtonActiveState(NoExitToMainMenuButton, false);
      SetButtonActiveState(YesExitToMainMenuButton, false);
      HideIngameMenuIcon();
      await UIFadeHelper.FadeOutControl(ExitToMainMenuPanel, 0.5f);

      GD.Print($"Last Game mode before exit to Main Menu: {GameStateManager.Instance.LastGameMode}");

      if (GameStateManager.Instance.LastGameMode == State.InDialogueMode) {
        await HandleExitFromDialogueMode();
      }

      EnableAllButtons();
      GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
    });
  }

  private async Task HandleExitFromDialogueMode() {
    await inputBlocker.BlockNewInput(async () => {
      UIManager.Instance.fadeOverlay.TopLevel = true;
      await UIManager.Instance.FadeInScreenOverlay(1.5f);
      UIManager.Instance.dialogueBoxUI.Hide();
      UIManager.Instance.playerChoicesBoxUI.Hide();
      VisualManager.Instance.RemoveImage();
    });
  }


  //Exit game
  public async Task OnExitGameButtonPressed() {
    await inputBlocker.BlockNewInput(async () => {
      SetButtonActiveState(exitGameButton, false);
      DisableAllButtons();
      HideIngameMenuIcon();
      SetContainerButtonsVisibility(ExitGameConfirmationPanel, true);
      SetButtonActiveState(NoExitGameButton, true);
      SetButtonActiveState(YesExitGameButton, true);
      await ShowExitGameConfirmationPopup();
      SetAllButtonsVisibility(false);
      //EnableAllButtons();
      // SetButtonActiveState(exitGameButton, true);
    });
  }

  public async Task ShowExitGameConfirmationPopup() {

    await inputBlocker.BlockNewInput(async () => {
      DisableAllButtons();
      await UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f);
      SetContainerButtonsVisibility(MainOptionsContainer, false);
      SetContainerButtonsVisibility(ExitGameConfirmationPanel, true);
      ExitGameConfirmationPanel.Visible = true;
      ExitGameConfirmationPanel.TopLevel = true;
      SetButtonActiveState(NoExitGameButton, false);
      SetButtonActiveState(YesExitGameButton, false);
      await UIFadeHelper.FadeInControl(ExitGameConfirmationPanel, 0.6f);
      SetButtonActiveState(NoExitGameButton, true);
      SetButtonActiveState(YesExitGameButton, true);
      GameStateManager.Instance.Fire(Trigger.DISPLAY_EXIT_GAME_MENU_CONFIRMATION_POPUP);
    });
  }

  private async Task OnExitGameConfirmButtonPressed() {

    await inputBlocker.BlockNewInput(async () => {
      SetButtonActiveState(NoExitGameButton, false);
      SetButtonActiveState(YesExitGameButton, false);
      HideIngameMenuIcon();
      await UIFadeHelper.FadeOutControl(ExitGameConfirmationPanel, 1.2f);
      GameStateManager.Instance.Fire(Trigger.EXIT_GAME);
    });
  }


  private bool isTransitioning = false;

  public async Task OnExitGameCancelButtonPressed() {

    await inputBlocker.BlockNewInput(async () => {
      SetButtonActiveState(NoExitGameButton, false);
      SetButtonActiveState(YesExitGameButton, false);
      DisableAllButtons();
      await UIFadeHelper.FadeOutControl(ExitGameConfirmationPanel, 0.6f);

      //await UIFadeHelper.FadeInControl(MainOptionsContainer, 1.0f);
      // SetButtonActiveState(NoExitGameButton, true);
      // SetButtonActiveState(YesExitGameButton, true);

      GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
      //EnableAllButtons();
    });

  }

}






