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
    private Dictionary<Button, string> buttonLocalizationKeys = new();
    private Dictionary<Button, string> buttonLanguageKeys = new();

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

        CallDeferred("DisableInput");
        GetUINodes();
        SetupButtonEvents();
        ApplyCustomStyles();
        // GetButtonLocalizationKeys();

        //assign the loc keys so each time the language changes the text property is updated
        WantToQuitGameLabel.Text = wantToQuitGameTRANSLATE;
        WantToQuitToMainMenuLabel.Text = wantToQuitToMainMenuTRANSLATE;
    }

    private void GetUINodes() {

        MainOptionsContainer = GetNode<VBoxContainer>("MainOptionsContainer");
        startNewGameButton = GetNode<Button>("MainOptionsContainer/StartNewGameButton");
        saveGameButton = GetNode<Button>("MainOptionsContainer/SaveGameButton"); ;
        continueGameButton = GetNode<Button>("MainOptionsContainer/ContinueButton"); ;
        loadGameButton = GetNode<Button>("MainOptionsContainer/LoadGameButton");
        languageButton = GetNode<Button>("MainOptionsContainer/LanguageButton");
        settingsButton = GetNode<Button>("MainOptionsContainer/SettingsButton"); ;
        creditsButton = GetNode<Button>("MainOptionsContainer/CreditsButton");
        exitToMainMenuButton = GetNode<Button>("MainOptionsContainer/ExitToMainMenuButton");
        exitGameButton = GetNode<Button>("MainOptionsContainer/ExitGameButton");
        ExitGameConfirmationPanel = GetNode<Panel>("%ExitGameConfirmationPanel");
        YesExitGameButton = GetNode<Button>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer/YesExitGameButton");
        NoExitGameButton = GetNode<Button>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer/NoExitGameButton");
        WantToQuitGameLabel = GetNode<RichTextLabel>("ExitGameConfirmationPanel/VBoxContainer/MarginContainer/WantToQuitGameLabel");
        YesNoButtonsHBoxContainer = GetNode<HBoxContainer>("ExitGameConfirmationPanel/VBoxContainer/YesNoButtonsHBoxContainer");

        ExitToMainMenuPanel = GetNode<Panel>("ExitToMainMenuPanel");
        YesExitToMainMenuButton = GetNode<Button>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer/YesExitToMainMenuButton");
        NoExitToMainMenuButton = GetNode<Button>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer/NoExitToMainMenuButton");
        WantToQuitToMainMenuLabel = GetNode<RichTextLabel>("ExitToMainMenuPanel/VBoxContainer/MarginContainer/WantToExitToMainMenuLabel");
        YesNoExitToMenuButtonsHBoxContainer = GetNode<HBoxContainer>("ExitToMainMenuPanel/VBoxContainer/YesNoExitToMenuButtonsHBoxContainer");

        LanguageOptionsContainer = GetNode<VBoxContainer>("LanguageOptionsContainer");
        englishButton = GetNode<Button>("LanguageOptionsContainer/EnglishButton");
        frenchButton = GetNode<Button>("LanguageOptionsContainer/FrenchButton");
        catalanButton = GetNode<Button>("LanguageOptionsContainer/CatalanButton");
        languagesGoBackButton = GetNode<Button>("LanguageOptionsContainer/GoBackButton");
        creditsConfirmationDialog = GetNode<ConfirmationDialog>("CreditsConfirmationDialog");

        mainMenuBackgroundImage = GetNode<TextureRect>("BackgroundImage");
    }

    private void SetupButtonEvents() {

        //trigger events depending on the option clicked
        startNewGameButton.Pressed += () => _ = OnStartNewGameButtonPressed();
        continueGameButton.Pressed += () => _ = OnContinueButtonPressed();
        saveGameButton.Pressed += () => _ = OnSaveGameButtonPressed();
        loadGameButton.Pressed += () => _ = OnLoadGameButtonPressed();
        languageButton.Pressed += () => _ = OnLanguageButtonPressed();
        creditsButton.Pressed += () => _ = OnCreditsButtonPressed();
        exitToMainMenuButton.Pressed += OnExitToMainMenuButtonPressed;
        YesExitToMainMenuButton.Pressed += () => _ = OnExitToMainMenuConfirmButtonPressed();
        NoExitToMainMenuButton.Pressed += () => _ = OnExitToMainMenuCancelButtonPressed();
        exitGameButton.Pressed += () => _ = OnExitGameButtonPressed();
        NoExitGameButton.Pressed += () => _ = OnExitGameCancelButtonPressed();
        YesExitGameButton.Pressed += () => _ = OnExitGameConfirmButtonPressed();
        creditsConfirmationDialog.Canceled += () => _ = OnCreditsCancelOrConfirmButtonPressed();
        creditsConfirmationDialog.Confirmed += () => _ = OnCreditsCancelOrConfirmButtonPressed();
        englishButton.Pressed += OnEnglishButtonPressed;
        frenchButton.Pressed += OnFrenchButtonPressed;
        catalanButton.Pressed += OnCatalanButtonPressed;
        languagesGoBackButton.Pressed += () => _ = OnLanguagesGoBackButtonPressed();
    }

    private void ApplyCustomStyles() {

        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetRight = 0;
        OffsetBottom = 0;

        WantToQuitGameLabel.BbcodeEnabled = true;
        WantToQuitToMainMenuLabel.BbcodeEnabled = true;

        YesNoButtonsHBoxContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        // Add margins
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("margin_left", 20);
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("margin_right", 20);
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("margin_bottom", 50);
        // Add space between buttons
        YesNoButtonsHBoxContainer.AddThemeConstantOverride("separation", 20);

        YesNoExitToMenuButtonsHBoxContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        // Add margins
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("margin_left", 20);
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("margin_right", 20);
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("margin_bottom", 50);
        // Add space between buttons
        YesNoExitToMenuButtonsHBoxContainer.AddThemeConstantOverride("separation", 20);

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

    public async Task UpdateTextsBasedOnLocale(string language) {

        TranslationServer.SetLocale(language);

        if (UIManager.Instance.dialogueBoxUI.IsVisibleInTree()) {
            DialogueManager.Instance.DisplayDialogue(DialogueManager.Instance.currentDialogueObject);
            await WaitForDialogueCompletion();
        }

        if (UIManager.Instance.playerChoicesBoxUI.IsVisibleInTree()) {
            UIManager.Instance.playerChoicesBoxUI.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, TranslationServer.GetLocale());
            await WaitForPlayerChoiceCompletion();
        }
    }

    private async Task WaitForDialogueCompletion() {
        while (DialogueManager.Instance.isDialogueBeingPrinted) {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task WaitForPlayerChoiceCompletion() {
        while (DialogueManager.Instance.IsPlayerChoiceBeingPrinted) {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void SetButtonsVisualState(bool enabled) {
        foreach (KeyValuePair<Button, string> pair in buttonLanguageKeys) {
            pair.Key.Modulate = enabled ? Colors.White : Colors.Gray;
        }
    }

    private async Task SetButtonsVisualStateAsync(bool enabled) {
        SetButtonsVisualState(enabled);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    public async Task DisplayMainMenu() {

        DisableButtonsInput();
        UIManager.Instance.dialogueBoxUI.TopLevel = false;
        UIManager.Instance.playerChoicesBoxUI.TopLevel = false;

        // Clear any pending input events
        GetViewport().SetInputAsHandled();

        UIManager.Instance.HideAllUIElements();

        VisualManager.Instance.RemoveImage();
        mainMenuBackgroundImage.Texture = GD.Load<Texture2D>("res://Visuals/splash screen the dragon riddle.png");
        mainMenuBackgroundImage.Modulate = new Color(1, 1, 1, 1);  // This sets it to ffffff (fully opaque)

        mainMenuBackgroundImage.TopLevel = true;
        mainMenuBackgroundImage.SetAnchorsPreset(LayoutPreset.FullRect);

        startNewGameButton.Show();
        exitGameButton.Show();
        saveGameButton.Hide();
        continueGameButton.Hide();
        exitToMainMenuButton.Hide();
        UIManager.Instance.inGameMenuButton.Hide();
        UIManager.Instance.menuOverlay.Visible = false; //a mask to avoid clicking on the dialoguebox when menus are open

        MainOptionsContainer.TopLevel = true;
        MainOptionsContainer.Show();
        Show();
        mainMenuBackgroundImage.Show();
        mainMenuBackgroundImage.Visible = true;
        await FadeInMainMenu();
        CallDeferred(nameof(EnableButtonsInput));
        SetProcessInput(true);
        UIInputHelper.EnableParentChildrenInput(MainOptionsContainer);

        MainMenuOpened?.Invoke();
    }

    public async Task FadeInMainMenu() {
        await Task.WhenAll(
            UIFadeHelper.FadeInControl(MainOptionsContainer, 1.0f),
            UIFadeHelper.FadeInControl(mainMenuBackgroundImage, 1.0f));
    }

    private async Task FadeOutMainMenu() {
        await Task.WhenAll(
            UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f),
            UIFadeHelper.FadeOutControl(mainMenuBackgroundImage, 1.0f));
        this.Visible = false;
    }

    private async Task FadeInInGamenMenu() => await UIFadeHelper.FadeInControl(MainOptionsContainer, 1.0f);   
    private async Task FadeOutInGameMenu() => await UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f);  
    private void EnableInput() => SetProcessInput(true);
    public void DisableInput() => SetProcessInput(false);
    private void SetInputHandled() => GetViewport().SetInputAsHandled();
    public void HideIngameMenuIcon() => UIManager.Instance.inGameMenuButton.Visible = false;
    public void ShowIngameMenuIcon() => UIManager.Instance.inGameMenuButton.Visible = true;
    
    private void DisableButtonsInput() {
        DisableButtonInput(loadGameButton);
        DisableButtonInput(saveGameButton);
        DisableButtonInput(startNewGameButton);
        DisableButtonInput(languageButton);
        DisableButtonInput(exitGameButton);
        DisableButtonInput(creditsButton);
    }

    private void EnableButtonsInput() {
        EnableButtonInput(loadGameButton);
        EnableButtonInput(saveGameButton);
        EnableButtonInput(startNewGameButton);
        EnableButtonInput(languageButton);
        EnableButtonInput(exitGameButton);
        EnableButtonInput(creditsButton);
    }

    private void DisableButtonInput(Button button) {
        button.SetProcessInput(false);
        button.MouseFilter = MouseFilterEnum.Ignore;
    }

    private void EnableButtonInput(Button button) {
        button.SetProcessInput(true);
        button.MouseFilter = MouseFilterEnum.Stop;
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
                                                       // Reset the opacity of the shared content (optionsMenuContainer)
        MainOptionsContainer.Show();
        MainOptionsContainer.Modulate = new Color(1, 1, 1, 1);  // Full opacity
        MainOptionsContainer.TopLevel = true;
        await UIFadeHelper.FadeInControl(MainOptionsContainer, 0.6f);
        MainOptionsContainer.SetProcessInput(true);
        EnableButtonsInput();
        UIInputHelper.EnableParentChildrenInput(MainOptionsContainer);
        ShowIngameMenuIcon();
        InGameMenuOpened?.Invoke();
    }

    public async Task CloseInGameMenu() {
        UIInputHelper.DisableParentChildrenInput(MainOptionsContainer);

        MainOptionsContainer.SetProcessInput(false);
        UIManager.Instance.menuOverlay.Visible = false;
        MainOptionsContainer.SetProcessInput(false);
        await UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.6f);
        MainOptionsContainer.Visible = false;
        InGameMenuClosed?.Invoke();
    }

    public async Task CloseMainMenu() {

        UIInputHelper.DisableParentChildrenInput(MainOptionsContainer);

        // MainOptionsContainer.TopLevel = false;
        // mainMenuBackgroundImage.TopLevel = false;
        MainOptionsContainer.SetProcessInput(false);
        await FadeOutMainMenu();
        MainOptionsContainer.Visible = false;
        mainMenuBackgroundImage.Visible = false;

        MainMenuClosed?.Invoke();
        // MainOptionsContainer.TopLevel = false;
        // mainMenuBackgroundImage.TopLevel = true;
    }

    private async Task OnStartNewGameButtonPressed() {
        DisableButtonInput(startNewGameButton);
        HideIngameMenuIcon();
        await FadeOutMainMenu();
        StartNewGameButtonPressed.Invoke();
        GameStateManager.Instance.Fire(Trigger.START_NEW_GAME);
    }

    private async Task OnContinueButtonPressed() {
        ShowIngameMenuIcon();
        DisableButtonsInput();
        await CloseInGameMenu();

        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        EnableButtonInput(continueGameButton);
    }

    private async Task OnSaveGameButtonPressed() {
        DisableButtonsInput();
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
    private async void OnFrenchButtonPressed() =>  await UpdateTextsBasedOnLocale("fr");
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
        // EnableButtonsInput();
        // EnableButtonInput(exitToMainMenuButton);
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
        await UIManager.Instance.FadeInScreenOverlay(1.5f);
        VisualManager.Instance.RemoveImage();
        ExitToMainMenuPanel.Visible = false;
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
    }

    private async Task OnExitToMainMenuCancelButtonPressed() {
        DisableButtonInput(NoExitToMainMenuButton);
        DisableButtonInput(YesExitToMainMenuButton);
        await UIFadeHelper.FadeOutControl(ExitToMainMenuPanel, 0.6f);
        ExitToMainMenuPanel.Visible = false;
        ShowIngameMenuIcon();
        //await FadeInInGamenMenu();
        EnableButtonInput(NoExitToMainMenuButton);
        EnableButtonInput(YesExitToMainMenuButton);
        //EnableButtonInput(exitToMainMenuButton);
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
        ExitGameConfirmationPanel.Visible = false;
        GameStateManager.Instance.Fire(Trigger.EXIT_GAME);
    }

    //triggered by confirmationDialog.Canceled event
    private async Task OnExitGameCancelButtonPressed() {
        DisableButtonInput(NoExitGameButton);
        DisableButtonInput(YesExitGameButton);
        DisableButtonsInput();
        await UIFadeHelper.FadeOutControl(ExitGameConfirmationPanel, 0.6f);
        ExitGameConfirmationPanel.Visible = false;
        ShowIngameMenuIcon();
        await FadeInInGamenMenu();
        EnableButtonInput(NoExitGameButton);
        EnableButtonInput(YesExitGameButton);
        // Close the confirmation popup
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
        EnableButtonsInput();
    }


    private async Task OnLanguagesGoBackButtonPressed() {
        ShowIngameMenuIcon();
        DisableButtonInput(languagesGoBackButton);
        GetTree().CallGroup("popups", "close_all");
        await UIFadeHelper.FadeOutControl(LanguageOptionsContainer, 0.6f);
        LanguageOptionsContainer.Visible = false;
        EnableButtonInput(languagesGoBackButton);
        DisableButtonsInput();
        await UIFadeHelper.FadeInControl(MainOptionsContainer, 0.6f);
        EnableButtonsInput();

        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }
}





