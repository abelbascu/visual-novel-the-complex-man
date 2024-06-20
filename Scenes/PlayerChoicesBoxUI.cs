using Godot;
using System;

public partial class PlayerChoicesBoxUI : VBoxContainer {
    private string playerChoiceToDisplay = "";
    public Action FinishedDisplaying;
    public VBoxContainer dialogueChoicesContainer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Show(); //make the dialogue box visible
        dialogueChoicesContainer = GetNode<VBoxContainer>("GlobalMarginContainer/PlayerChoicesMarginContainer");
    }

    public void DisplayPlayerChoice(DialogueObject playerChoiceObject, string languageCode) {
        this.playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);
        PlayerChoiceButton dialogueChoice = new();
        dialogueChoicesContainer.AddChild(dialogueChoice);
        dialogueChoice.Text = "";
        dialogueChoice.Text = playerChoiceToDisplay;
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
