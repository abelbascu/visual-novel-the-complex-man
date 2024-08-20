using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.SymbolStore;

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
    public TextureRect mainMenuBackgroundImage;
    public bool LOAD_SCREEN = true;
    public bool SAVE_SCREEN = false;
    public Action MainMenuOpened;
    public Action InGameMenuOpened;
    public Action MainMenuClosed;
    public Action InGameMenuClosed;


    public override void _Ready() {

        mainMenuBackgroundImage = GetNode<TextureRect>("BackgroundImage");

        this.Show();
        //displaying the UI boxes with the options
        MainOptionsContainer = GetNode<VBoxContainer>("MainOptionsContainer");
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

        creditsConfirmationDialog = GetNode<ConfirmationDialog>("MainOptionsContainer/CreditsButton/CreditsConfirmationDialog");

        //trigger events depending on the option clicked
        startNewGameButton.Pressed += OnStartNewGameButtonPressed;
        continueGameButton.Pressed += OnContinueButtonPressed;
        saveGameButton.Pressed += OnSaveGameButtonPressed;
        loadGameButton.Pressed += OnLoadGameButtonPressed;
        languageButton.Pressed += OnLanguageButtonPressed;
        creditsButton.Pressed += OnCreditsButtonPressed;
        exitGameButton.Pressed += OnExitGameButtonPressed;
        exitToMainMenuButton.Pressed += OnExitToMainMenuButtonPressed;
        exitGameConfirmationDialog.Canceled += OnExitGameCancelButtonPressed;
        exitGameConfirmationDialog.Confirmed += OnExitGameConfirmButtonPressed;
        exitToMainMenuConfirmationDialog.Confirmed += OnExitToMainMenuConfirmButtonPressed;
        exitToMainMenuConfirmationDialog.Canceled += OnExitToMainMenuCancelButtonPressed;

        creditsConfirmationDialog.Canceled += OnCreditsCancelOrConfirmButtonPressed;
        creditsConfirmationDialog.Confirmed += OnCreditsCancelOrConfirmButtonPressed;

        LanguageOptionsContainer = GetNode<VBoxContainer>("LanguageOptionsContainer");
        Button englishButton = GetNode<Button>("LanguageOptionsContainer/EnglishButton");
        Button frenchButton = GetNode<Button>("LanguageOptionsContainer/FrenchButton");
        Button catalanButton = GetNode<Button>("LanguageOptionsContainer/CatalanButton");
        Button goBackButton = GetNode<Button>("LanguageOptionsContainer/GoBackButton");

        englishButton.Pressed += OnEnglishButtonPressed;
        frenchButton.Pressed += OnFrenchButtonPressed;
        catalanButton.Pressed += OnCatalanButtonPressed;
        goBackButton.Pressed += OnGoBackButtonPressed;

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
    }

    private void ApplyCustomStyleToButtonsInContainer(VBoxContainer container) {
        foreach (var child in container.GetChildren()) {
            if (child is Button button) {
                UIManager.Instance.ApplyCustomStyleToButton(button);
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

    public void DisplayMainMenu() {
        MainOptionsContainer.Show();
        startNewGameButton.Show();
        exitGameButton.Show();
        saveGameButton.Hide();
        continueGameButton.Hide();
        exitToMainMenuButton.Hide();
        //mainMenuBackgroundImage.Texture = GD.Load<Texture2D>("res://Visuals/DialogueOrPlayerChoice/cosmos ether.png");
        mainMenuBackgroundImage.Texture = null;
        mainMenuBackgroundImage.SetAnchorsPreset(LayoutPreset.FullRect);




        LoadSaveManager.Instance.ToggleAutosave(false);
        UIManager.Instance.inGameMenuButton.Hide();
        UIManager.Instance.menuOverlay.Visible = false; //a mask to avoid clicking on the dialoguebox when menus are open
        MainOptionsContainer.Show();
        Show();
        MainMenuOpened?.Invoke();
    }

    public void DisplayInGameMenu() {
        saveGameButton.Show();
        continueGameButton.Show();
        exitToMainMenuButton.Show();
        startNewGameButton.Hide();
        exitGameButton.Hide();
        mainMenuBackgroundImage.Texture = null;
        UIManager.Instance.menuOverlay.Visible = true; //put overlay to prevent reading input from other UI elements behind this mask
        LoadSaveManager.Instance.ToggleAutosave(false);
        Show();
        InGameMenuOpened?.Invoke();
    }

    public void CloseInGameMenu() {

        //InGameMenuClosed?.Invoke();
        // if (GameStateManager.Instance.CurrentState == InGameMenuOpened) {
        //     GameStateManager.Instance.ENTER_DIALOGUE_MODE(); //PUTTING THIS IN AN IF CLAUSE IS DANGEROUS!!!
        // }
        Hide();
    }

    public void CloseMainMenu() {
        Hide();
        MainMenuClosed?.Invoke();
    }

    private void OnStartNewGameButtonPressed() {
        Hide();
        GameStateManager.Instance.START_NEW_GAME();
    }

    private void OnContinueButtonPressed() {
        GameStateManager.Instance.ENTER_DIALOGUE_MODE();
    }

    private void OnSaveGameButtonPressed() {
        // SaveGameButtonPressed.Invoke();
        GameStateManager.Instance.INITIALIZE_SAVE_SCREEN();
    }

    private void OnLoadGameButtonPressed() {
        // LoadGameButtonPressed.Invoke();
        GameStateManager.Instance.INITIALIZE_LOAD_SCREEN();
        //Hide();
    }

    private void OnLanguageButtonPressed() {
        GameStateManager.Instance.DISPLAY_LANGUAGE_MENU();
    }

    private void OnCreditsButtonPressed() {
        creditsConfirmationDialog.Show();
        MainOptionsContainer.Hide();
    }

    private void OnExitGameButtonPressed() {
        ShowExitGameConfirmationPopup();

    }

    public void OnExitToMainMenuButtonPressed() {
        ShowExitToMainMenuConfirmationPopup();
    }

    public void ShowExitGameConfirmationPopup() {
        MainOptionsContainer.Hide();
        exitGameConfirmationDialog.Show();
    }

    public void ShowExitToMainMenuConfirmationPopup() {
        MainOptionsContainer.Hide();
        exitToMainMenuConfirmationDialog.Show();
    }

    //triggered by confirmationDialog.Confirmed event
    private void OnExitGameConfirmButtonPressed() {
        GetTree().Quit(); // Exit the game
    }

    //triggered by confirmationDialog.Canceled event
    private void OnExitGameCancelButtonPressed() {
        // Close the confirmation popup
        GameStateManager.Instance.GO_BACK_TO_MENU();
    }

    private void OnExitToMainMenuConfirmButtonPressed() {
        GameStateManager.Instance.DISPLAY_MAIN_MENU();
        // CloseInGameMenu();
        // UIManager.Instance.HideAllUIElements();
        // VisualManager.Instance.RemoveImage();
        //HERE WE NEED TO HIDE ANYTHING THAT IS DISPLAYED ON SCREEN //HERE WE NEED TO HIDE ANYTHING THAT IS DISPLAYED ON SCREEN //HERE WE NEED TO HIDE ANYTHING THAT IS DISPLAYED ON SCREEN 
        //DisplayMainMenu();
    }

    private void OnExitToMainMenuCancelButtonPressed() {
        GameStateManager.Instance.GO_BACK_TO_MENU();
    }

    private void OnCreditsCancelOrConfirmButtonPressed() {
        GameStateManager.Instance.GO_BACK_TO_MENU();
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

    private void OnGoBackButtonPressed() {
        LanguageOptionsContainer.Hide();
        GameStateManager.Instance.GO_BACK_TO_MENU();
    }
}





