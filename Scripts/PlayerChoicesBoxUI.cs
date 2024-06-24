using Godot;
using System;

public partial class PlayerChoicesBoxUI : VBoxContainer {
    private string playerChoiceToDisplay = "";
    public Action FinishedDisplayingPlayerChoice;
    public VBoxContainer dialogueChoicesContainer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Show(); //make the dialogue box visible
        dialogueChoicesContainer = GetNode<VBoxContainer>("GlobalMarginContainer/PlayerChoicesMarginContainer");
    }

    public void DisplayPlayerChoice(DialogueObject playerChoiceObject, string languageCode) {
        this.playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);
        //when we create the button, we also pass the dialogueObject in the constructor 
        //we'll need it when the player clicks the button to show a next dialogue or player choices.
        PlayerChoiceButton dialogueChoice = new (playerChoiceObject);
        dialogueChoicesContainer.AddChild(dialogueChoice);
        dialogueChoice.Text = "";
        dialogueChoice.Text = playerChoiceToDisplay;
        FinishedDisplayingPlayerChoice.Invoke();

    }

    public string GetLocalePlayerChoice(DialogueObject playerChoiceObj, string locale) {
        string localeCurrentDialogue = locale switch {
            "fr" => playerChoiceObj.FrenchText,
            "ca" => playerChoiceObj.CatalanText,
            // Add more cases as needed for other locales
            _ => playerChoiceObj.DialogueTextDefault  // Default to the default text field
        };
        return localeCurrentDialogue;
    }

}
