using Godot;
using System;

public partial class Main : Control {

    ConfirmationDialog confirmationDialog;
    VBoxContainer MainOptionsContainer;

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





