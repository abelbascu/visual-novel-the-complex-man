using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static GameStateMachine;

public partial class MainMenu : Control {

    //UIElements
    Panel ExitGameConfirmationPanel;
    Panel ExitToMainMenuPanel;
    ConfirmationDialog creditsConfirmationDialog;
    public VBoxContainer MainOptionsContainer;
    public VBoxContainer LanguageOptionsContainer;
    public TextureRect mainMenuBackgroundImage;

    //Buttons
    Button startNewGameButton;
    Button saveGameButton;
    Button continueGameButton;
    Button loadGameButton;
    Button languageButton;
    Button creditsButton;
    Button exitGameButton;
    Button exitToMainMenuButton;
    Button settingsButton;
    Button languagesGoBackButton;
    Button YesExitGameButton;
    Button NoExitGameButton;
    Button YesExitToMainMenuButton;
    Button NoExitToMainMenuButton;
    Button englishButton;
    Button frenchButton;
    Button catalanButton;

    //Button containers
    private HBoxContainer YesNoButtonsHBoxContainer;
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


    public override void _Ready() {

        GetUINodes();
        SetupButtonEvents();
        ApplyCustomStyles();

        //assign the loc keys so each time the language changes the text property is updated
        WantToQuitGameLabel.Text = wantToQuitGameTRANSLATE;
        WantToQuitToMainMenuLabel.Text = wantToQuitToMainMenuTRANSLATE;
    }

    private void GetUINodes() {

        //main buttons container
        MainOptionsContainer = GetNode<VBoxContainer>("MainOptionsContainer");

        //main buttons
        startNewGameButton = GetNode<Button>("MainOptionsContainer/StartNewGameButton");
        saveGameButton = GetNode<Button>("MainOptionsContainer/SaveGameButton"); ;
        continueGameButton = GetNode<Button>("MainOptionsContainer/ContinueButton"); ;
        loadGameButton = GetNode<Button>("MainOptionsContainer/LoadGameButton");
        languageButton = GetNode<Button>("MainOptionsContainer/LanguageButton");
        settingsButton = GetNode<Button>("MainOptionsContainer/SettingsButton"); ;
        creditsButton = GetNode<Button>("MainOptionsContainer/CreditsButton");
        exitToMainMenuButton = GetNode<Button>("MainOptionsContainer/ExitToMainMenuButton");
        exitGameButton = GetNode<Button>("MainOptionsContainer/ExitGameButton");

        //exit game panel
        ExitGameConfirmationPanel = GetNode<Panel>("%ExitGameConfirmationPanel");
        YesExitGameButton = GetNode<Button>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer/YesExitGameButton");
        NoExitGameButton = GetNode<Button>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer/NoExitGameButton");
        WantToQuitGameLabel = GetNode<RichTextLabel>("ExitGameConfirmationPanel/VBoxContainer/MarginContainer/WantToQuitGameLabel");
        YesNoButtonsHBoxContainer = GetNode<HBoxContainer>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer");

        //exit to main menu panel
        ExitToMainMenuPanel = GetNode<Panel>("ExitToMainMenuPanel");
        YesExitToMainMenuButton = GetNode<Button>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer/YesExitToMainMenuButton");
        NoExitToMainMenuButton = GetNode<Button>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer/NoExitToMainMenuButton");
        WantToQuitToMainMenuLabel = GetNode<RichTextLabel>("ExitToMainMenuPanel/VBoxContainer/MarginContainer/WantToExitToMainMenuLabel");
        YesNoExitToMenuButtonsHBoxContainer = GetNode<HBoxContainer>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer");

        //languages panel
        LanguageOptionsContainer = GetNode<VBoxContainer>("LanguageOptionsContainer");
        englishButton = GetNode<Button>("LanguageOptionsContainer/EnglishButton");
        frenchButton = GetNode<Button>("LanguageOptionsContainer/FrenchButton");
        catalanButton = GetNode<Button>("LanguageOptionsContainer/CatalanButton");
        languagesGoBackButton = GetNode<Button>("LanguageOptionsContainer/GoBackButton");

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

        //put main menu to full rect
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetRight = 0;
        OffsetBottom = 0;

        WantToQuitGameLabel.BbcodeEnabled = true;
        WantToQuitToMainMenuLabel.BbcodeEnabled = true;

        YesNoButtonsHBoxContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        // Add margins to prevent buttons touching the edges of panel
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("margin_left", 20);
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("margin_right", 20);
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("margin_bottom", 50);
        // Add space between buttons
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("separation", 20);

        YesNoExitToMenuButtonsHBoxContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("margin_left", 20);
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("margin_right", 20);
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("margin_bottom", 50);
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("separation", 20);

        //apply a default blue style with white rounded edges for panels and buttons
        ApplyCustomStyleToButtonsInContainer(MainOptionsContainer);
        ApplyCustomStyleToButtonsInContainer(LanguageOptionsContainer);
        UIThemeHelper.ApplyCustomStyleToPanel(ExitToMainMenuPanel);
        ApplyCustomStyleToButtonsInContainer(ExitToMainMenuPanel);
        UIThemeHelper.ApplyCustomStyleToPanel(ExitGameConfirmationPanel);
        ApplyCustomStyleToButtonsInContainer(ExitGameConfirmationPanel);
        UIThemeHelper.ApplyCustomStyleToWindowDialog(creditsConfirmationDialog);
    }

    private void ApplyCustomStyleToButtonsInContainer(Control container) {
        foreach (var child in container.GetChildren()) {
            if (child is Button button) {
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
        foreach (Button button in LanguageOptionsContainer.GetChildren()) {
            DisableButtonInput(button);
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
        foreach (Button button in LanguageOptionsContainer.GetChildren()) {
            EnableButtonInput(button);
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

    //when the user clicks on any menu button, let's disable all buttons
    //to prevent race conditions and breaking the state machine.
    private void DisableButtonsInput() {
        DisableButtonInput(continueGameButton);
        DisableButtonInput(loadGameButton);
        DisableButtonInput(saveGameButton);
        DisableButtonInput(startNewGameButton);
        DisableButtonInput(languageButton);
        DisableButtonInput(exitGameButton);
        DisableButtonInput(creditsButton);
        DisableButtonInput(exitToMainMenuButton);
    }

    private void EnableButtonsInput() {
        EnableButtonInput(continueGameButton);
        EnableButtonInput(loadGameButton);
        EnableButtonInput(saveGameButton);
        EnableButtonInput(startNewGameButton);
        EnableButtonInput(languageButton);
        EnableButtonInput(exitGameButton);
        EnableButtonInput(creditsButton);
        EnableButtonInput(exitToMainMenuButton);
    }

    private void DisableButtonInput(Button button) {
        button.SetProcessInput(false);
        button.MouseFilter = MouseFilterEnum.Ignore;
        button.FocusMode = FocusModeEnum.None;
    }

    private void EnableButtonInput(Button button) {
        button.SetProcessInput(true);
        button.MouseFilter = MouseFilterEnum.Stop;
        button.FocusMode = FocusModeEnum.All;
    }

    public async Task DisplayMainMenu() {

        DisableButtonsInput();

        //put interfering UI elements behind
        //Dialogue Mode UI elements
        UIManager.Instance.dialogueBoxUI.TopLevel = false;
        UIManager.Instance.playerChoicesBoxUI.TopLevel = false;

        // Clear any pending input events
        GetViewport().SetInputAsHandled();

        //if we come back from a current play, remove the dialogue mode image
        VisualManager.Instance.RemoveImage();
        //then load the main menu own image
        mainMenuBackgroundImage.Texture = GD.Load<Texture2D>("res://Visuals/splash screen the dragon riddle.png");
        mainMenuBackgroundImage.Modulate = new Color(1, 1, 1, 1);  // This sets it to ffffff (fully opaque)
        mainMenuBackgroundImage.SetAnchorsPreset(LayoutPreset.FullRect);
        mainMenuBackgroundImage.Visible = true;

        //specific main menu buttons, not ingame menu
        startNewGameButton.Show();
        exitGameButton.Show();
        saveGameButton.Hide();
        continueGameButton.Hide();
        exitToMainMenuButton.Hide();

        //only displayed in dialogue mode, or future game modes
        UIManager.Instance.inGameMenuButton.Hide();

        //a mask to avoid clicking on the dialoguebox/player choices
        //when ingame menu is open. If in main menu, disable it.
        UIManager.Instance.menuOverlay.Visible = false;

        MainOptionsContainer.TopLevel = true;
        MainOptionsContainer.Show();
        mainMenuBackgroundImage.Show();
        Show();

        await FadeInMainMenu();
        EnableButtonsInput();

        //this calls to pause the game timer, more sense on ingame menu
        //but can be used to autosave game while user exits current game
        MainMenuOpened?.Invoke();
    }

    public async Task CloseMainMenu() {
        DisableButtonsInput();
        MainOptionsContainer.SetProcessInput(false);
        await FadeOutMainMenu();
        //this calls to resume the game timer
        MainMenuClosed?.Invoke();
    }

    public async Task FadeInMainMenu() {
        await Task.WhenAll(
            UIFadeHelper.FadeInControl(MainOptionsContainer, 0.7f),
            UIFadeHelper.FadeInControl(mainMenuBackgroundImage, 0.7f));
    }

    private async Task FadeOutMainMenu() {
        await Task.WhenAll(
            UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.7f),
            UIFadeHelper.FadeOutControl(mainMenuBackgroundImage, 0.7f));
    }

    public async Task DisplayInGameMenu() {
        DisableButtonsInput();
        UIManager.Instance.dialogueBoxUI.TopLevel = false;
        UIManager.Instance.playerChoicesBoxUI.TopLevel = false;
        MainOptionsContainer.SetProcessInput(false);
        Show();
        saveGameButton.Show();
        continueGameButton.Show();
        exitToMainMenuButton.Show();
        startNewGameButton.Hide();
        exitGameButton.Hide();
        mainMenuBackgroundImage.Texture = null;
        UIManager.Instance.menuOverlay.Visible = true; //put overlay to prevent reading input from other UI elements behind this mask
        MainOptionsContainer.Show();
        MainOptionsContainer.Modulate = new Color(1, 1, 1, 1);  // Full opacity
        MainOptionsContainer.TopLevel = true;
        await UIFadeHelper.FadeInControl(MainOptionsContainer, 0.6f);
        MainOptionsContainer.SetProcessInput(true);
        EnableButtonsInput();
        ShowIngameMenuIcon();
        InGameMenuOpened?.Invoke();
    }

    private async Task FadeInInGamenMenu() => await UIFadeHelper.FadeInControl(MainOptionsContainer, 1.0f);
    private async Task FadeOutInGameMenu() => await UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f);

    public void HideIngameMenuIcon() => UIManager.Instance.inGameMenuButton.Visible = false;
    public void ShowIngameMenuIcon() {
        UIManager.Instance.inGameMenuButton.Visible = true;
    }

    public async Task CloseInGameMenu() {
        DisableButtonsInput();
        MainOptionsContainer.SetProcessInput(false);
        UIManager.Instance.menuOverlay.Visible = false;
        MainOptionsContainer.SetProcessInput(false);
        await UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.6f);
        InGameMenuClosed?.Invoke();
    }

    private async Task OnStartNewGameButtonPressed() {
        DisableButtonsInput();
        HideIngameMenuIcon();
        await FadeOutMainMenu();
        StartNewGameButtonPressed.Invoke();
        GameStateManager.Instance.Fire(Trigger.START_NEW_GAME);
    }

    private async Task OnContinueButtonPressed() {
        DisableButtonsInput();
        ShowIngameMenuIcon();
        await CloseInGameMenu();

        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        EnableButtonInput(continueGameButton);
    }

    private async Task OnSaveGameButtonPressed() {
        DisableButtonsInput();
        HideIngameMenuIcon();
        await CloseInGameMenu();
        GameStateManager.Instance.Fire(Trigger.INITIALIZE_SAVE_SCREEN);
    }

    private async Task OnLoadGameButtonPressed() {
        HideIngameMenuIcon();
        DisableButtonsInput();
        // await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.INITIALIZE_LOAD_SCREEN);
    }

    private async Task OnLanguageButtonPressed() {
        HideIngameMenuIcon();
        DisableButtonsInput();
        await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.DISPLAY_LANGUAGE_MENU);
        EnableButtonsInput();
    }

    public async Task DisplayLanguageMenu() {
        LanguageOptionsContainer.Visible = true;
        LanguageOptionsContainer.TopLevel = true;
        await UIFadeHelper.FadeInControl(LanguageOptionsContainer);
    }

    private async void OnEnglishButtonPressed() => await UpdateTextsBasedOnLocale("en");
    private async void OnFrenchButtonPressed() => await UpdateTextsBasedOnLocale("fr");
    private async void OnCatalanButtonPressed() => await UpdateTextsBasedOnLocale("ca");

    private async Task OnCreditsButtonPressed() {
        HideIngameMenuIcon();
        DisableButtonsInput();
        await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.DISPLAY_CREDITS);
        creditsConfirmationDialog.Show();
        // MainOptionsContainer.Hide();
        await UIFadeHelper.FadeInWindow(creditsConfirmationDialog, 0.2f);
    }

    private async Task OnCreditsCancelOrConfirmButtonPressed() {
        await UIFadeHelper.FadeOutWindow(creditsConfirmationDialog, 0.2f);
        await FadeInInGamenMenu();
        EnableButtonsInput();
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }

    public async void OnExitToMainMenuButtonPressed() {
        DisableButtonInput(exitToMainMenuButton);
        DisableButtonsInput();
        HideIngameMenuIcon();
        //await FadeOutInGameMenu();
        await ShowExitToMainMenuConfirmationPopup();
    }

    public async Task ShowExitToMainMenuConfirmationPopup() {
        DisableButtonsInput();
        DisableButtonInput(NoExitToMainMenuButton);
        DisableButtonInput(YesExitToMainMenuButton);
        await FadeOutInGameMenu();
        ExitToMainMenuPanel.Visible = true;
        ExitToMainMenuPanel.TopLevel = true;
        await UIFadeHelper.FadeInControl(ExitToMainMenuPanel, 0.5f);
        EnableButtonInput(NoExitToMainMenuButton);
        EnableButtonInput(YesExitToMainMenuButton);
        GameStateManager.Instance.Fire(Trigger.DISPLAY_EXIT_TO_MAIN_MENU_CONFIRMATION_POPUP);
    }
    private async Task OnExitToMainMenuConfirmButtonPressed() {
        DisableButtonInput(NoExitToMainMenuButton);
        DisableButtonInput(YesExitToMainMenuButton);
        HideIngameMenuIcon();
        await UIFadeHelper.FadeOutControl(ExitToMainMenuPanel, 0.5f);

        GD.Print($"Last Game mode before exit to Main Menu: {GameStateManager.Instance.LastGameMode}");

        if (GameStateManager.Instance.LastGameMode == State.InDialogueMode) {
            UIManager.Instance.fadeOverlay.TopLevel = true;
            await UIManager.Instance.FadeInScreenOverlay(1.5f);
            UIManager.Instance.dialogueBoxUI.Hide();
            UIManager.Instance.playerChoicesBoxUI.Hide();
            VisualManager.Instance.RemoveImage();
        }

        EnableButtonsInput();
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
    }

    private async Task OnExitToMainMenuCancelButtonPressed() {
        DisableButtonInput(NoExitToMainMenuButton);
        DisableButtonInput(YesExitToMainMenuButton);
        await UIFadeHelper.FadeOutControl(ExitToMainMenuPanel, 0.6f);
        ShowIngameMenuIcon();
        EnableButtonInput(NoExitToMainMenuButton);
        EnableButtonInput(YesExitToMainMenuButton);
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
        EnableButtonsInput();
    }

    private async Task OnExitGameButtonPressed() {
        DisableButtonInput(exitGameButton);
        DisableButtonsInput();
        HideIngameMenuIcon();
        await ShowExitGameConfirmationPopup();
        EnableButtonsInput();
        EnableButtonInput(exitGameButton);
    }

    public async Task ShowExitGameConfirmationPopup() {
        DisableButtonsInput();
        await FadeOutInGameMenu();
        ExitGameConfirmationPanel.Visible = true;
        ExitGameConfirmationPanel.TopLevel = true;
        DisableButtonInput(NoExitGameButton);
        DisableButtonInput(YesExitGameButton);
        await UIFadeHelper.FadeInControl(ExitGameConfirmationPanel, 0.6f);
        EnableButtonInput(NoExitGameButton);
        EnableButtonInput(YesExitGameButton);
        GameStateManager.Instance.Fire(Trigger.DISPLAY_EXIT_GAME_MENU_CONFIRMATION_POPUP);
    }
    //triggered by confirmationDialog.Confirmed event
    private async Task OnExitGameConfirmButtonPressed() {
        DisableButtonInput(NoExitGameButton);
        DisableButtonInput(YesExitGameButton);
        HideIngameMenuIcon();
        await UIFadeHelper.FadeOutControl(ExitGameConfirmationPanel, 1.2f);
        GameStateManager.Instance.Fire(Trigger.EXIT_GAME);
    }

    //triggered by confirmationDialog.Canceled event
    private async Task OnExitGameCancelButtonPressed() {
        DisableButtonInput(NoExitGameButton);
        DisableButtonInput(YesExitGameButton);
        DisableButtonsInput();
        await UIFadeHelper.FadeOutControl(ExitGameConfirmationPanel, 0.6f);
        await FadeInInGamenMenu();
        EnableButtonInput(NoExitGameButton);
        EnableButtonInput(YesExitGameButton);
        // Close the confirmation popup
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
        EnableButtonsInput();
    }

    private async Task OnLanguagesGoBackButtonPressed() {
        if (GameStateManager.Instance.CurrentState == State.InGameMenuDisplayed)
            ShowIngameMenuIcon();

        DisableButtonInput(languagesGoBackButton);
        GetTree().CallGroup("popups", "close_all");
        await UIFadeHelper.FadeOutControl(LanguageOptionsContainer, 0.6f);
        EnableButtonInput(languagesGoBackButton);
        DisableButtonsInput();
        await UIFadeHelper.FadeInControl(MainOptionsContainer, 0.6f);
        EnableButtonsInput();
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }
}





