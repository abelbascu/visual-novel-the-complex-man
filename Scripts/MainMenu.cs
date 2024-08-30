using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.SymbolStore;
using static GameStateMachine;
using System.Threading.Tasks;

public partial class MainMenu : Control {

    [Export] public string language { get; set; } = "";
    private string previousLanguage = "";
    ConfirmationDialog exitGameConfirmationDialog;
    ConfirmationDialog exitToMainMenuConfirmationDialog;
    ConfirmationDialog creditsConfirmationDialog;
    public VBoxContainer MainOptionsContainer;
    public VBoxContainer LanguageOptionsContainer;
    private Dictionary<Button, string> buttonLocalizationKeys = new();
    private Dictionary<Button, string> buttonLanguageKeys = new();
    public Action StartNewGameButtonPressed;
    public Action SaveGameButtonPressed;
    public Action LoadGameButtonPressed;
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
    public TextureRect mainMenuBackgroundImage;
    public bool LOAD_SCREEN = true;
    public bool SAVE_SCREEN = false;
    public Action MainMenuOpened;
    public Action InGameMenuOpened;
    public Action MainMenuClosed;
    public Action InGameMenuClosed;
    private UITextTweenFadeIn fadeIn;
    private UITextTweenFadeOut fadeOut;


    public override void _Ready() {

        CallDeferred("DisableInput");

        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();

        mainMenuBackgroundImage = GetNode<TextureRect>("BackgroundImage");

        this.Show();
        //displaying the UI boxes with the options
        MainOptionsContainer = GetNode<VBoxContainer>("MainOptionsContainer");

        MainOptionsContainer.Visible = false;


        startNewGameButton = GetNode<Button>("MainOptionsContainer/StartNewGameButton");
        saveGameButton = GetNode<Button>("MainOptionsContainer/SaveGameButton"); ;
        continueGameButton = GetNode<Button>("MainOptionsContainer/ContinueButton"); ;
        loadGameButton = GetNode<Button>("MainOptionsContainer/LoadGameButton");
        languageButton = GetNode<Button>("MainOptionsContainer/LanguageButton");
        settingsButton = GetNode<Button>("MainOptionsContainer/SettingsButton"); ;
        creditsButton = GetNode<Button>("MainOptionsContainer/CreditsButton");
        exitGameButton = GetNode<Button>("MainOptionsContainer/ExitGameButton");
        exitToMainMenuButton = GetNode<Button>("MainOptionsContainer/ExitToMainMenuButton");

        exitGameConfirmationDialog = GetNode<ConfirmationDialog>("MainOptionsContainer/ExitGameButton/ExitGameConfirmationDialog");
        exitToMainMenuConfirmationDialog = GetNode<ConfirmationDialog>("MainOptionsContainer/ExitToMainMenuButton/ExitToMainMenuConfirmationDialog");

        creditsConfirmationDialog = GetNode<ConfirmationDialog>("CreditsConfirmationDialog");

        //trigger events depending on the option clicked
        startNewGameButton.Pressed += () => _ = OnStartNewGameButtonPressed();
        continueGameButton.Pressed += () => _ = OnContinueButtonPressed();
        saveGameButton.Pressed += () => _ = OnSaveGameButtonPressed();
        loadGameButton.Pressed += () => _ = OnLoadGameButtonPressed();
        languageButton.Pressed += () => _ = OnLanguageButtonPressed();
        creditsButton.Pressed += () => _ = OnCreditsButtonPressed();
        exitGameButton.Pressed += () => _ = OnExitGameButtonPressed();
        exitToMainMenuButton.Pressed += OnExitToMainMenuButtonPressed;
        exitGameConfirmationDialog.Canceled += () => _ = OnExitGameCancelButtonPressed();
        exitGameConfirmationDialog.Confirmed += () => _ = OnExitGameConfirmButtonPressed();
        exitToMainMenuConfirmationDialog.Confirmed += OnExitToMainMenuConfirmButtonPressed;
        exitToMainMenuConfirmationDialog.Canceled += OnExitToMainMenuCancelButtonPressed;

        creditsConfirmationDialog.Canceled += () => _ = OnCreditsCancelOrConfirmButtonPressed();
        creditsConfirmationDialog.Confirmed += () => _ = OnCreditsCancelOrConfirmButtonPressed();

        LanguageOptionsContainer = GetNode<VBoxContainer>("LanguageOptionsContainer");
        Button englishButton = GetNode<Button>("LanguageOptionsContainer/EnglishButton");
        Button frenchButton = GetNode<Button>("LanguageOptionsContainer/FrenchButton");
        Button catalanButton = GetNode<Button>("LanguageOptionsContainer/CatalanButton");
        languagesGoBackButton = GetNode<Button>("LanguageOptionsContainer/GoBackButton");

        englishButton.Pressed += OnEnglishButtonPressed;
        frenchButton.Pressed += OnFrenchButtonPressed;
        catalanButton.Pressed += OnCatalanButtonPressed;
        languagesGoBackButton.Pressed += () => _ = OnLanguagesGoBackButtonPressed();

        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetRight = 0;
        OffsetBottom = 0;

        //***** SET LANGUAGE HERE *****
        //we check what language the user has in his Windows OS
        string currentCultureName = System.Globalization.CultureInfo.CurrentCulture.Name;
        string[] parts = currentCultureName.Split('-');
        language = parts[0];
        TranslationServer.SetLocale(language);
        //for testing purposes, will change the language directly here so we do not have to tinker witn Windows locale settings each time
        language = "en";
        TranslationServer.SetLocale(language);

        //below, we grab the locale keys before they are overwritten by the fallback locale translation,
        //otherwise we wouldn't be able to switch to another locale as the keys would be destroyed.
        //remember the original keys and translations for menu buttons are in a google sheet at https://docs.google.com/spreadsheets/d/1HsAar1VdxVkJbuKUa3ElxSN0yZES2YfNUrk2yA7EeMM/edit?gid=0#gid=0
        //an example START_NEW_GAME, WANT_QUIT_GAME? are keys that have its corresponding translation columns in the google sheet. 
        //This google sheet is exported as csv files and added to Godot's filesystem, then again added to the Localizatoin tab in project settings
        foreach (Button button in MainOptionsContainer.GetChildren()) {
            string initialText = button.Text;
            //[button] is the button name in Godot's editor, the .Text is a key manually added that should have a match with a key in the translations file in the filesystem
            buttonLocalizationKeys[button] = initialText;
            GD.Print($"Button name: {button.Name}, Key: {initialText}, Locale: {TranslationServer.GetLocale()}");
        }

        foreach (Button button in LanguageOptionsContainer.GetChildren()) {
            string initialText = button.Text;
            //[button] is the button name in Godot's editor, the .Text is a key manually added that should have a match with a key in the translations file in the filesystem
            buttonLanguageKeys[button] = initialText;
            GD.Print($"Button name: {button.Name}, Key: {initialText}, Locale: {TranslationServer.GetLocale()}");
        }

        ApplyCustomStyleToButtonsInContainer(MainOptionsContainer);
        ApplyCustomStyleToButtonsInContainer(LanguageOptionsContainer);
        UIThemeHelper.ApplyCustomStyleToWindowDialog(creditsConfirmationDialog);
        UIThemeHelper.ApplyCustomStyleToWindowDialog(exitGameConfirmationDialog);
        UIThemeHelper.ApplyCustomStyleToWindowDialog(exitToMainMenuConfirmationDialog);
    }

    private void ApplyCustomStyleToButtonsInContainer(VBoxContainer container) {
        foreach (var child in container.GetChildren()) {
            if (child is Button button) {
                UIThemeHelper.ApplyCustomStyleToButton(button);
            }
        }
    }

    //we constantly check if the dev (me!) changes the locale via the export variable 'language' in the editor
    public override void _Process(double delta) {
        base._Process(delta);

        if (language != previousLanguage) {
            previousLanguage = language;
            TranslationServer.SetLocale(language);
            GD.Print("new game locale: " + TranslationServer.GetLocale());
            UpdateButtonTexts();
            if (UIManager.Instance.dialogueBoxUI.IsVisibleInTree() == true)
                DialogueManager.Instance.DisplayDialogue(DialogueManager.Instance.currentDialogueObject);
            if (UIManager.Instance.playerChoicesBoxUI.IsVisibleInTree() == true)
                UIManager.Instance.playerChoicesBoxUI.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, TranslationServer.GetLocale());
        }
    }

    private void UpdateButtonTexts() {
        foreach (Button button in buttonLocalizationKeys.Keys) {
            string localizationKey = buttonLocalizationKeys[button];
            //as we changed the locale in the editor and it was detected in _Process(), now we are telling Godot to get the proper translation based on the new locale
            string translatedText = TranslationServer.Translate(localizationKey);
            //and we add the proper translation to the current button that we previously saved in a dictionary in _Ready()
            button.Text = translatedText;
            //GD.Print($"Button Text: {translatedText}, Key: {localizationKey}, Locale: {TranslationServer.GetLocale()}");
        }

        //for when the language options are displayed, if the user changes language, change the language locale of the buttons too
        foreach (Button button in buttonLanguageKeys.Keys) {
            string localizationKey = buttonLanguageKeys[button];
            //as we changed the locale in the editor and it was detected in _Process(), now we are telling Godot to get the proper translation based on the new locale
            string translatedText = TranslationServer.Translate(localizationKey);
            //and we add the proper translation to the current button that we previously saved in a dictionary in _Ready()
            button.Text = translatedText;
            //GD.Print($"Button Text: {translatedText}, Key: {localizationKey}, Locale: {TranslationServer.GetLocale()}");
        }
    }


    private const float InputDelay = 1.0f; // Adjust this value as needed

    public async Task DisplayMainMenu() {
        DisableButtonsInput();
        //we have disabled input with SetProcessInput(false) at Ready(), doing it here has no effect
        //this works well though, but disables input in all nodes, not the parent one only.
        //UIInputHelper.DisableParentChildrenInput(this);

        // Clear any pending input events
        GetViewport().SetInputAsHandled();

        UIManager.Instance.HideAllUIElements();
        //MainOptionsContainer.Hide();

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

        MainMenuOpened?.Invoke();

        await FadeInMainMenu();

        EnableButtonsInput();

        SetProcessInput(true);
        UIInputHelper.EnableParentChildrenInput(MainOptionsContainer);
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

    private async Task FadeInInGamenMenu() {

        await UIFadeHelper.FadeInControl(MainOptionsContainer, 1.0f);
    }

    private async Task FadeOutInGameMenu() {

        await UIFadeHelper.FadeOutControl(MainOptionsContainer, 1.0f);
    }



    private void EnableInput() {
        SetProcessInput(true);
    }

    public void DisableInput() {
        SetProcessInput(false);
    }

    private void SetInputHandled() {
        GetViewport().SetInputAsHandled();
    }


        private void DisableButtonsInput() {

        DisableButtonInput(loadGameButton); //THIS HERE IS VERY HACKY, OR MAYBE WE NEED TO PUT ALL THE ENABLE BUTTON METHODS HERE
        DisableButtonInput(saveGameButton);
        DisableButtonInput(startNewGameButton);
        DisableButtonInput(startNewGameButton);
    }

    private void EnableButtonsInput() {

        EnableButtonInput(loadGameButton); //THIS HERE IS VERY HACKY, OR MAYBE WE NEED TO PUT ALL THE ENABLE BUTTON METHODS HERE
        EnableButtonInput(saveGameButton);
        EnableButtonInput(startNewGameButton);
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
        InGameMenuOpened?.Invoke();
        MainOptionsContainer.TopLevel = true;
        await fadeIn.FadeIn(MainOptionsContainer, 0.6f);
        MainOptionsContainer.SetProcessInput(true);

        EnableButtonsInput();

        UIInputHelper.EnableParentChildrenInput(MainOptionsContainer);
    }

    public async Task CloseInGameMenu() {
        UIInputHelper.DisableParentChildrenInput(MainOptionsContainer);

        MainOptionsContainer.SetProcessInput(false);
        UIManager.Instance.menuOverlay.Visible = false;
        MainOptionsContainer.SetProcessInput(false);
        await UIFadeHelper.FadeOutControl(MainOptionsContainer, 0.6f);
        InGameMenuClosed?.Invoke();
        MainOptionsContainer.Visible = false;
    }

    public async Task CloseMainMenu() {

        UIInputHelper.DisableParentChildrenInput(MainOptionsContainer);

        // MainOptionsContainer.TopLevel = false;
        // mainMenuBackgroundImage.TopLevel = false;
        MainOptionsContainer.SetProcessInput(false);
        await FadeOutMainMenu();
        MainMenuClosed?.Invoke();
        MainOptionsContainer.Visible = false;
        mainMenuBackgroundImage.Visible = false;

        // MainOptionsContainer.TopLevel = false;
        // mainMenuBackgroundImage.TopLevel = true;

    }

    public void HideIngameMenuIcon() {
        UIManager.Instance.inGameMenuButton.Visible = false;
    }

    public void ShowIngameMenuIcon() {
        UIManager.Instance.inGameMenuButton.Visible = true;
    }


    private async Task OnStartNewGameButtonPressed() {
        DisableButtonInput(startNewGameButton);
        await FadeOutMainMenu();
        GameStateManager.Instance.Fire(Trigger.START_NEW_GAME);
    }

    private async Task OnContinueButtonPressed() {
        DisableButtonInput(continueGameButton);
        await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        EnableButtonInput(continueGameButton);
    }

    private async Task OnSaveGameButtonPressed() {
        DisableButtonInput(saveGameButton);
        await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.INITIALIZE_SAVE_SCREEN);
    }

    private async Task OnLoadGameButtonPressed() {
        DisableButtonInput(loadGameButton);
        // await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.INITIALIZE_LOAD_SCREEN);

    }

    private async Task OnLanguageButtonPressed() {
        DisableButtonInput(languageButton);
        await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.DISPLAY_LANGUAGE_MENU);
        EnableButtonInput(languageButton);
    }

    public async Task DisplayLanguageMenu() {
        LanguageOptionsContainer.Visible = true;
        LanguageOptionsContainer.TopLevel = true;
        await UIFadeHelper.FadeInControl(LanguageOptionsContainer);
    }

    private async Task OnCreditsButtonPressed() {
        DisableButtonInput(creditsButton);
        await FadeOutInGameMenu();
        GameStateManager.Instance.Fire(Trigger.DISPLAY_CREDITS);
        creditsConfirmationDialog.Show();
        // MainOptionsContainer.Hide();
        await UIFadeHelper.FadeInWindow(creditsConfirmationDialog, 0.2f);
        EnableButtonInput(creditsButton);
    }

    private async Task OnCreditsCancelOrConfirmButtonPressed() {
        await UIFadeHelper.FadeOutWindow(creditsConfirmationDialog, 0.2f);
        await FadeInInGamenMenu();
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }

    public async void OnExitToMainMenuButtonPressed() {
        DisableButtonInput(exitToMainMenuButton);
        //await FadeOutInGameMenu();
        await ShowExitToMainMenuConfirmationPopup();
        EnableButtonInput(exitToMainMenuButton);
    }

    public async Task ShowExitToMainMenuConfirmationPopup() {
        await FadeOutInGameMenu();
        //MainOptionsContainer.Hide();
        await UIFadeHelper.FadeInWindow(exitToMainMenuConfirmationDialog, 0.5f);
        // exitToMainMenuConfirmationDialog.Show();
        GameStateManager.Instance.Fire(Trigger.DISPLAY_EXIT_TO_MAIN_MENU_CONFIRMATION_POPUP);
    }
    private async void OnExitToMainMenuConfirmButtonPressed() {
        await UIFadeHelper.FadeOutWindow(exitToMainMenuConfirmationDialog, 0.5f);
        //await FadeOutInGameMenu();
        if (GameStateManager.Instance.CurrentState == State.InDialogueMode)
            VisualManager.Instance.RemoveImage();

        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
    }

    private void OnExitToMainMenuCancelButtonPressed() {
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }

    private async Task OnExitGameButtonPressed() {
        DisableButtonInput(exitGameButton);
        await ShowExitGameConfirmationPopup();
        EnableButtonInput(exitGameButton);
    }

    public async Task ShowExitGameConfirmationPopup() {
        await FadeOutInGameMenu();
        await UIFadeHelper.FadeInWindow(exitGameConfirmationDialog, 0.6f);
        GameStateManager.Instance.Fire(Trigger.DISPLAY_EXIT_GAME_MENU_CONFIRMATION_POPUP);
    }
    //triggered by confirmationDialog.Confirmed event
    private async Task OnExitGameConfirmButtonPressed() {
        await FadeOutMainMenu();
        GameStateManager.Instance.Fire(Trigger.EXIT_GAME);
    }

    //triggered by confirmationDialog.Canceled event
    private async Task OnExitGameCancelButtonPressed() {
        await FadeInInGamenMenu();
        // Close the confirmation popup
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }



    private void OnEnglishButtonPressed() {
        language = "en";
    }

    private void OnFrenchButtonPressed() {
        language = "fr";
    }

    private void OnCatalanButtonPressed() {
        language = "ca";
    }

    private async Task OnLanguagesGoBackButtonPressed() {
        DisableButtonInput(languagesGoBackButton);
        GetTree().CallGroup("popups", "close_all");
        if (GameStateManager.Instance.CurrentSubstate == SubState.LanguageMenuDisplayed) {
            await UIFadeHelper.FadeOutControl(LanguageOptionsContainer, 0.6f);
            LanguageOptionsContainer.Visible = false;
        }
        await FadeInInGamenMenu();
        EnableButtonInput(languagesGoBackButton);
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
    }
}





