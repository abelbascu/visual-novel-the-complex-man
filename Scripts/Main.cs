using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.SymbolStore;

public partial class Main : Control {

    [Export] public string language { get; set; } = "";
    private string previousLanguage = "";
    ConfirmationDialog confirmationDialog;
    VBoxContainer MainOptionsContainer;
    private Dictionary<Button, string> buttonLocalizationKeys = new Dictionary<Button, string>();


    public override void _Ready() {

        //displaying the UI boxes with the options
        MainOptionsContainer = GetNode<VBoxContainer>("VBoxContainer");
        Button startNewGameButton = GetNode<Button>("VBoxContainer/StartNewGameButton");
        Button loadGameButton = GetNode<Button>("VBoxContainer/LoadGameButton");
        Button languageButton = GetNode<Button>("VBoxContainer/LanguageButton");
        Button creditsButton = GetNode<Button>("VBoxContainer/CreditsButton");
        Button exitGameButton = GetNode<Button>("VBoxContainer/ExitButton");
        confirmationDialog = GetNode<ConfirmationDialog>("ConfirmationDialog");

        //trigger events depending on the option clicked

        startNewGameButton.Pressed += OnStartNewGameButtonPressed;
        loadGameButton.Pressed += OnLoadGameButtonPressed;
        languageButton.Pressed += OnLanguageButtonPressed;
        creditsButton.Pressed += OnCreditsButtonPressed;
        exitGameButton.Pressed += OnExitButtonPressed;
        confirmationDialog.Canceled += OnCancelButtonPressed;
        confirmationDialog.Confirmed += OnConfirmButtonPressed;

        foreach (Button button in MainOptionsContainer.GetChildren()) {
            string initialText = button.Text;
            buttonLocalizationKeys[button] = initialText;
            GD.Print($"Button: {button.Name}, Key: {initialText}");
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);

        if (language != previousLanguage) {
            previousLanguage = language;
            TranslationServer.SetLocale(language);
            DialogueManager.languageCode = language;
            UpdateButtonTexts();
        }
    }

    private void UpdateButtonTexts() {
        foreach (Button button in buttonLocalizationKeys.Keys) {
            string localizationKey = buttonLocalizationKeys[button];
            string translatedText = TranslationServer.Translate(localizationKey);
            button.Text = translatedText;
            GD.Print($"Button: {translatedText}, Key: {localizationKey}");
        }
    }

    public override void _Notification(int what) {
        if (what == NotificationTranslationChanged) {
            UpdateButtonTexts();
            GD.Print("texts being updated to new language");
        }
    }

    private void OnStartNewGameButtonPressed() {

        DialogueManager.StartButtonPressed.Invoke();
        Hide();
    }

    private void OnLoadGameButtonPressed() {

    }

    private void OnLanguageButtonPressed() {

    }

    private void OnCreditsButtonPressed() {

    }

    private void OnExitButtonPressed() {
        ShowConfirmationPopup();

    }

    public void ShowConfirmationPopup() {
        MainOptionsContainer.Hide();
        confirmationDialog.Show();
    }

    private void OnConfirmButtonPressed() {
        GetTree().Quit(); // Exit the game
    }

    private void OnCancelButtonPressed() {
        // Close the confirmation popup
        GetTree().CallGroup("popups", "close_all");
        MainOptionsContainer.Show();
    }

}





