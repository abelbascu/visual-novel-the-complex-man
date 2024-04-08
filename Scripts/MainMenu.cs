using Godot;
using System;

public partial class MainMenu : Control {

    private string language = "ca";
    ConfirmationDialog confirmationDialog;
    VBoxContainer MainOptionsContainer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        MainOptionsContainer = GetNode<VBoxContainer>("VBoxContainer");
        Button startNewGameButton = GetNode<Button>("VBoxContainer/StartNewGameButton");
        Button loadGameButton = GetNode<Button>("VBoxContainer/LoadGameButton");
        Button languageButton = GetNode<Button>("VBoxContainer/LanguageButton");
        Button creditsButton = GetNode<Button>("VBoxContainer/CreditsButton");
        Button exitGameButton = GetNode<Button>("VBoxContainer/ExitButton");
        confirmationDialog = GetNode<ConfirmationDialog>("ConfirmationDialog");

        startNewGameButton.Pressed += OnStartNewGameButtonPressed;
        loadGameButton.Pressed += OnLoadGameButtonPressed;
        languageButton.Pressed += OnLanguageButtonPressed;
        creditsButton.Pressed += OnCreditsButtonPressed;
        exitGameButton.Pressed += OnExitButtonPressed;
        confirmationDialog.Canceled += OnCancelButtonPressed;
        confirmationDialog.Confirmed += OnConfirmButtonPressed;

        TranslationServer.SetLocale(language); //set up language
        GD.Print($"langauage: {language}");
    }

    private void OnStartNewGameButtonPressed() {

        DialogueManager.LanguageLocale = language;
        //now we can start showing the first dialogue
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





