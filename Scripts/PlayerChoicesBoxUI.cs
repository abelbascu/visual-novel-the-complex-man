using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerChoicesBoxUI : MarginContainer {
    public Action<Vector2> SizeChanged;
    public Action FinishedDisplayingPlayerChoice;
    private MarginContainer globalMarginContainer;
    public VBoxContainer playerChoicesContainer;
    private PackedScene playerChoiceButtonScene;
    private NinePatchRect backgroundRect;

    public override void _Ready() {
        Show();

        backgroundRect = GetNode<NinePatchRect>("NinePatchRect"); // Adjust the path if needed
        if (backgroundRect != null) {
            // Set the alpha to 0.5 (adjust this value to change transparency)
            backgroundRect.Modulate = new Color(backgroundRect.Modulate, 0.9f);
        }

        // Create and add GlobalMarginContainer
        globalMarginContainer = new MarginContainer();
        AddChild(globalMarginContainer);

        // Set up GlobalMarginContainer with padding
        globalMarginContainer.AddThemeConstantOverride("margin_left", 20);
        globalMarginContainer.AddThemeConstantOverride("margin_right", 20);
        globalMarginContainer.AddThemeConstantOverride("margin_top", 20);
        globalMarginContainer.AddThemeConstantOverride("margin_bottom", 20);

        // Set GlobalMarginContainer to fill the entire PlayerChoicesBoxUI
        globalMarginContainer.AnchorRight = 1;
        globalMarginContainer.AnchorBottom = 1;
        globalMarginContainer.SizeFlagsHorizontal = SizeFlags.Fill;
        globalMarginContainer.SizeFlagsVertical = SizeFlags.Fill;
        globalMarginContainer.SizeFlagsVertical = SizeFlags.ShrinkBegin;

        playerChoicesContainer = new VBoxContainer();
        globalMarginContainer.AddChild(playerChoicesContainer);
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
        playerChoicesContainer.SizeFlagsHorizontal = SizeFlags.Fill;
        playerChoicesContainer.SizeFlagsVertical = SizeFlags.ShrinkEnd;
        playerChoicesContainer.AddThemeConstantOverride("separation", 20);

        SizeFlagsVertical = SizeFlags.ShrinkEnd;
        GrowVertical = GrowDirection.Begin;

        // Add margins to the PlayerChoicesBoxUI
        AddThemeConstantOverride("margin_left", 30);
        AddThemeConstantOverride("margin_right", 30);
        AddThemeConstantOverride("margin_top", 30);
        AddThemeConstantOverride("margin_bottom", 30);

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