using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

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

        // Check if a button with this DialogueObject already exists
        PlayerChoiceButton existingButton = FindExistingButton(playerChoiceObject);

        if (existingButton != null) {
            // Move existing button to the top
            dialogueChoicesContainer.MoveChild(existingButton, 0);

        } else {
            // Create new button and add it at the top
            PlayerChoiceButton dialogueChoice = new(playerChoiceObject);
            dialogueChoicesContainer.AddChild(dialogueChoice);
            dialogueChoicesContainer.MoveChild(dialogueChoice, 0);
            dialogueChoice.Text = playerChoiceToDisplay;
        }

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

    private PlayerChoiceButton FindExistingButton(DialogueObject dialogueObject) {
        foreach (var child in dialogueChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton button && button.HasMatchingDialogueObject(dialogueObject)) {
                return button;
            }
        }
        return null;
    }

    public void RewmoveAllNoGroupChildrenWithSameOriginID(DialogueObject dialogueObject) {

        List<PlayerChoiceButton> buttonsToRemove = new List<PlayerChoiceButton>();

        foreach (PlayerChoiceButton child in dialogueChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton button) {
                if (child.dialogueObject.NoGroupParentID == dialogueObject.NoGroupParentID)
                    buttonsToRemove.Add(child);
            }
        }

        foreach (var button in buttonsToRemove) {
            dialogueChoicesContainer.RemoveChild(button);
            button.QueueFree();
        }
    }
}


