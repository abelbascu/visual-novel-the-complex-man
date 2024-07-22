using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerChoicesBoxUI : MarginContainer {
    public Action FinishedDisplayingPlayerChoice;
    public VBoxContainer playerChoicesContainer;
    private PackedScene playerChoiceButtonScene;

    public override void _Ready() {
        Show();
        playerChoicesContainer = GetNode<VBoxContainer>("GlobalMarginContainer/PlayerChoicesMarginContainer");
        playerChoiceButtonScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoiceButton.tscn");

        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, resizeMode: LayoutPresetMode.KeepWidth);

        // Ensure buttons are aligned to the top
        playerChoicesContainer.Alignment = BoxContainer.AlignmentMode.Begin;
        playerChoicesContainer.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        playerChoicesContainer.AddThemeConstantOverride("separation", 20);

        GrowVertical = GrowDirection.Begin;

        // Ensure the content starts from the top of this control
        AddThemeConstantOverride("margin_top", 0);
        AddThemeConstantOverride("margin_bottom", 0);

        // Ensure content starts from the top
        var globalMarginContainer = GetNode<MarginContainer>("GlobalMarginContainer");
        globalMarginContainer.AddThemeConstantOverride("margin_top", 0);
    }

    public void DisplayPlayerChoice(DialogueObject playerChoiceObject, string languageCode) {
        string playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);

        PlayerChoiceButton existingButton = FindExistingButton(playerChoiceObject);

        if (existingButton != null) {
            playerChoicesContainer.MoveChild(existingButton, 0);
            existingButton.SetText(playerChoiceToDisplay);
        } else {
            PlayerChoiceButton playerChoiceButton = playerChoiceButtonScene.Instantiate<PlayerChoiceButton>();
            playerChoiceButton.SetDialogueObject(playerChoiceObject);
            playerChoicesContainer.AddChild(playerChoiceButton);
            playerChoiceButton.SetText(playerChoiceToDisplay);
        }

        FinishedDisplayingPlayerChoice?.Invoke();
    }


    public string GetLocalePlayerChoice(DialogueObject playerChoiceObj, string locale) {
        return locale switch {
            "fr" => playerChoiceObj.FrenchText,
            "ca" => playerChoiceObj.CatalanText,
            _ => playerChoiceObj.DialogueTextDefault
        };
    }

    private PlayerChoiceButton FindExistingButton(DialogueObject dialogueObject) {
        foreach (var child in playerChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton button && button.HasMatchingDialogueObject(dialogueObject)) {
                return button;
            }
        }
        return null;
    }

    public void RemoveAllNoGroupChildrenWithSameOriginID(DialogueObject dialogueObject) {
        List<PlayerChoiceButton> buttonsToRemove = new List<PlayerChoiceButton>();

        foreach (PlayerChoiceButton child in playerChoicesContainer.GetChildren()) {
            if (child.dialogueObject.NoGroupParentID == dialogueObject.NoGroupParentID) {
                buttonsToRemove.Add(child);
            }
        }

        foreach (var button in buttonsToRemove) {
            playerChoicesContainer.RemoveChild(button);
            button.QueueFree();
        }
    }

    public void RemoveAllPlayerChoiceButtons() {
        foreach (Node child in playerChoicesContainer.GetChildren()) {
            if (child is PlayerChoiceButton) {
                playerChoicesContainer.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}