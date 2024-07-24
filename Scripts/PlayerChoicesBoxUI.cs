using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerChoicesBoxUI : MarginContainer {
    public Action<Vector2> SizeChanged;
    public Action FinishedDisplayingPlayerChoice;
    public VBoxContainer playerChoicesContainer;
    private PackedScene playerChoiceButtonScene;

    public override void _Ready() {
        Show();
        playerChoicesContainer = GetNode<VBoxContainer>("PlayerChoicesMarginContainer");
        playerChoiceButtonScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoiceButton.tscn");

        SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);

        //Set anchors to allow the container to grow
        AnchorTop = 1;
        AnchorBottom = 1;
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;

        //Set offsets to define the initial size
        OffsetLeft = -800;  // Half of the desired width
        OffsetRight = 800;  // Half of the desired width
        OffsetTop = -200;   // Initial height, will grow as needed

        // Ensure buttons are aligned to the top
        playerChoicesContainer.Alignment = BoxContainer.AlignmentMode.Begin;
        //playerChoicesContainer.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        playerChoicesContainer.SizeFlagsHorizontal = SizeFlags.Fill;
        playerChoicesContainer.SizeFlagsVertical = SizeFlags.ShrinkEnd;
        playerChoicesContainer.AddThemeConstantOverride("separation", 20);

        SizeFlagsVertical = SizeFlags.ShrinkEnd;

        GrowVertical = GrowDirection.Begin;


        // Ensure the PlayerChoicesBoxUI can grow
        //SizeFlagsVertical = SizeFlags.ShrinkEnd;

        // Ensure the content starts from the top of this control
        // Add margins to the PlayerChoicesBoxUI
        AddThemeConstantOverride("margin_left", 5);
        AddThemeConstantOverride("margin_right", 5);
        AddThemeConstantOverride("margin_top", 5);
        AddThemeConstantOverride("margin_bottom", 5);

        // // Ensure content starts from the top
        // var globalMarginContainer = GetNode<MarginContainer>("GlobalMarginContainer");
        // globalMarginContainer.AddThemeConstantOverride("margin_top", 0);

        Resized += () => OnResized();
    }

    public void OnResized() {
        SizeChanged.Invoke(Size);
    }

    public void DisplayPlayerChoices(List<DialogueObject> playerChoices, string languageCode) {

        RemoveAllPlayerChoiceButtons();

        foreach (var playerChoiceObject in playerChoices) {
            if (!ButtonExistsForPlayerChoice(playerChoiceObject)) {
                string playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);
                PlayerChoiceButton playerChoiceButton = playerChoiceButtonScene.Instantiate<PlayerChoiceButton>();
                playerChoiceButton.SetDialogueObject(playerChoiceObject);
                playerChoicesContainer.AddChild(playerChoiceButton);
                playerChoiceButton.SetText(playerChoiceToDisplay);

                SizeChanged += playerChoiceButton.OnParentSizeChanged;
            }
        }

        FinishedDisplayingPlayerChoice?.Invoke();
    }

    public bool ButtonExistsForPlayerChoice(DialogueObject playerChoiceObject) {
        var existingButtons = playerChoicesContainer.GetChildren()
            .OfType<PlayerChoiceButton>()
            .ToList();

        return existingButtons.Any(button => button.HasMatchingDialogueObject(playerChoiceObject));
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
            if (child is PlayerChoiceButton button) {
                SizeChanged -= button.OnParentSizeChanged;
                playerChoicesContainer.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}