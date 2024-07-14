using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerChoicesBoxUI : VBoxContainer {
    private string playerChoiceToDisplay = "";
    public Action FinishedDisplayingPlayerChoice;
    public VBoxContainer playerChoicesContainer;
    private PackedScene playerChoiceButtonScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Show(); //make the dialogue box visible
        playerChoicesContainer = GetNode<VBoxContainer>("GlobalMarginContainer/PlayerChoicesMarginContainer");
        playerChoiceButtonScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoiceButton.tscn");

        // Anchor to bottom
        AnchorTop = 1;
        AnchorBottom = 1;
        GrowVertical = GrowDirection.Begin;

        // Set a maximum height (adjust as needed)
        CustomMinimumSize = new Vector2(0, 400);


    }

    public void DisplayPlayerChoice(DialogueObject playerChoiceObject, string languageCode) {
        this.playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);
        //when we create the button, we also pass the dialogueObject in the constructor 
        //we'll need it when the player clicks the button to show a next dialogue or player choices.

        // Check if a button with this DialogueObject already exists
        PlayerChoiceButton existingButton = FindExistingButton(playerChoiceObject);

        if (existingButton != null) {
            // Move existing button to the top
            playerChoicesContainer.MoveChild(existingButton, 0);
            existingButton.SetText(playerChoiceToDisplay);

        } else {
            // Create new button and add it at the top
            //PlayerChoiceButton dialogueChoice = new(playerChoiceObject);
            PlayerChoiceButton playerChoiceButton = playerChoiceButtonScene.Instantiate<PlayerChoiceButton>();
            playerChoiceButton.SetDialogueObject(playerChoiceObject);

            playerChoiceButton.SetText(playerChoiceToDisplay);
            playerChoicesContainer.AddChild(playerChoiceButton);

            playerChoicesContainer.MoveChild(playerChoiceButton, 0);



            //playerChoiceButton.SetText(playerChoiceToDisplay);

            // CustomizeButtonAppearance(playerChoiceButton);
            // Ensure proper vertical layout
            playerChoicesContainer.CustomMinimumSize = new Vector2(playerChoicesContainer.CustomMinimumSize.X, 0);
            playerChoicesContainer.SizeFlagsVertical = SizeFlags.ShrinkEnd;

        }

        FinishedDisplayingPlayerChoice?.Invoke();
    }


    //     private void CustomizeButtonAppearance(PlayerChoiceButton button)
    // {
    //     // Remove background
    //     button.Flat = true;
    //     // Add hover effect
    //     button.MouseEntered += () => button.Scale = new Vector2(1.1f, 1.1f);
    //     button.MouseExited += () => button.Scale = Vector2.One;
    // }

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
        foreach (var child in playerChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton button && button.HasMatchingDialogueObject(dialogueObject)) {
                return button;
            }
        }
        return null;
    }

    public void RewmoveAllNoGroupChildrenWithSameOriginID(DialogueObject dialogueObject) {

        List<PlayerChoiceButton> buttonsToRemove = new List<PlayerChoiceButton>();

        foreach (PlayerChoiceButton child in playerChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton button) {
                if (child.dialogueObject.NoGroupParentID == dialogueObject.NoGroupParentID)
                    buttonsToRemove.Add(child);
            }
        }

        foreach (var button in buttonsToRemove) {
            playerChoicesContainer.RemoveChild(button);
            button.QueueFree();
        }
    }

    public void RemoveAllPlayerChoiceButtons() {
        foreach (PlayerChoiceButton child in playerChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton button)
                playerChoicesContainer.RemoveChild(child);
        }
    }

}



