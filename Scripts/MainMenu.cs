using Godot;
using System;

public partial class MainMenu : Control {

    private string language = "fr";
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
        TranslationServer.SetLocale(language);
        TranslationServer.GetLocale();
        //string language = OS.GetLocaleLanguage();
        GD.Print($"langauage: {language}");
    }

    private void OnStartNewGameButtonPressed() {
        //we pass the locale to Dialogue Manager so he knows what translations to load
        DialogueManager.LanguageLocaleChosen.Invoke(language);

        PackedScene gameStartScene = (PackedScene)ResourceLoader.Load("res://Scenes/GameStartScene.tscn");
        Node gameStartNode = gameStartScene.Instantiate();
        GetTree().Root.AddChild(gameStartNode);
        //we wait at the very end before the next frame starts to ensure that gameStartNode was properly added to the tree.
        GetTree().ProcessFrame += DialogueManager.treeChanged;
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
        //GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }

//     public override void _Notification(int what)
// {
//     if (what == NotificationWMCloseRequest)
//          ShowConfirmationPopup();
// }



   public void ShowConfirmationPopup()
    {
        // Show a confirmation popup
        // PopupPanel popUpPanel = GetNode<PopupPanel>("PopUpPanel");
        // popUpPanel.Show();
        // popUpPanel.PopupCentered();

        // // Connect the signals of the confirmation popup
        // Button confirmButton = GetNode<Button>("PopUpPanel/ConfirmButton");
        // confirmButton.Show();
        // confirmButton.Pressed += OnConfirmButtonPressed;
        // Button cancelButton = GetNode<Button>("PopUpPanel/CancelButton");
        // cancelButton.Show();
        // cancelButton.Pressed += OnCancelButtonPressed;

        MainOptionsContainer.Hide();
        confirmationDialog.Show();
       

       
    }

    private void OnConfirmButtonPressed()
    {
        GetTree().Quit(); // Exit the game
    }

    private void OnCancelButtonPressed()
    {
        
        // Close the confirmation popup
        GetTree().CallGroup("popups", "close_all");
        MainOptionsContainer.Show();
    }

}





