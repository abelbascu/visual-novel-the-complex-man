using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class UIManager : Control {

    public static UIManager Instance { get; private set; }
    public PackedScene dialogueBoxUIScene;
    public DialogueBoxUI dialogueBoxUI; //the graphical rectangle container to display the text over
    private VBoxContainer dialogueChoicesMarginContainer;
    //public PlayerChoicesBoxUI playerChoicesBoxUI; //the graphical rectangle VBoxContainer to displayer the branching player choices.
    private const int UI_BOTTOM_POSITION = 200; //starting at the bottom of the screen, we subtract this value to position the Y screen position of the dilaogue box  
    public PackedScene playerChoicesBoxUIScene;
    public PlayerChoicesBoxUI playerChoicesBoxUI;
    private const float ANCHOR_LEFT_PERCENTAGE = 0.08f;
    private const float ANCHOR_RIGHT_PERCENTAGE = 0.925f;
    private const float ANCHOR_TOP_PERCENTAGE = 1f;
    private const float ANCHOR_BOTTOM_PERCENTAGE = 1f;
    private const int OFFSET_LEFT = 0;
    private const int OFFSET_RIGHT = 0;
    private const int OFFSET_TOP = 200;
    private const int OFFSET_BOTTOM = 0;


    public override void _Ready() {
        dialogueBoxUI = GetNode<DialogueBoxUI>("DialogueBoxUI");
        //playerChoicesBoxUI = GetNode<PlayerChoicesBoxUI>("PlayerChoicesBoxUI");
        //playerChoicesBoxUI.Hide();
        CallDeferred(nameof(SetupNodeOrder));
        MouseFilter = MouseFilterEnum.Ignore;
        //dialogueChoicesMarginContainer = playerChoicesBoxUI.GetNode<VBoxContainer>("GlobalMarginContainer/PlayerChoicesMarginContainer");

        //Set up PlayerChoicesBoxUI
        playerChoicesBoxUIScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoicesBoxUI.tscn");
        playerChoicesBoxUI = playerChoicesBoxUIScene.Instantiate<PlayerChoicesBoxUI>();
        AddChild(playerChoicesBoxUI);
        playerChoicesBoxUI.Hide();
        // Set anchors to stretch horizontally
        playerChoicesBoxUI.AnchorLeft = ANCHOR_LEFT_PERCENTAGE;
        playerChoicesBoxUI.AnchorRight = ANCHOR_RIGHT_PERCENTAGE;
        playerChoicesBoxUI.AnchorTop = ANCHOR_TOP_PERCENTAGE;
        playerChoicesBoxUI.AnchorBottom = ANCHOR_BOTTOM_PERCENTAGE;
        // Reset offsets
        playerChoicesBoxUI.OffsetLeft = OFFSET_LEFT;
        playerChoicesBoxUI.OffsetRight = OFFSET_RIGHT;
        playerChoicesBoxUI.OffsetTop = OFFSET_TOP;  // Adjust this value to set the initial height
        playerChoicesBoxUI.OffsetBottom = OFFSET_BOTTOM;

        //Set up DialogueBoxUI
        dialogueBoxUIScene = ResourceLoader.Load<PackedScene>("res://Scenes/DialogueBoxUI.tscn");
        dialogueBoxUI = dialogueBoxUIScene.Instantiate<DialogueBoxUI>();
        AddChild(dialogueBoxUI);
        dialogueBoxUI.Hide();
        // Set anchors to stretch horizontally
        dialogueBoxUI.AnchorLeft = ANCHOR_LEFT_PERCENTAGE;
        dialogueBoxUI.AnchorRight = ANCHOR_RIGHT_PERCENTAGE;
        dialogueBoxUI.AnchorTop = ANCHOR_TOP_PERCENTAGE;
        dialogueBoxUI.AnchorBottom = ANCHOR_BOTTOM_PERCENTAGE;
        // Reset offsets
        dialogueBoxUI.OffsetLeft = OFFSET_LEFT;
        dialogueBoxUI.OffsetRight = OFFSET_RIGHT;
        dialogueBoxUI.OffsetTop = OFFSET_TOP;  // Adjust this value to set the initial height
        dialogueBoxUI.OffsetBottom = OFFSET_BOTTOM;
    }

    private void SetupNodeOrder() {
        // Ensure VisualsManager is below UI elements in the scene tree
        var visualManager = GetNode<VisualManager>("../VisualManager");
        GetParent().MoveChild(visualManager, 0);
        // Move this UIManager to be the last child (top layer)
        GetParent().MoveChild(this, GetParent().GetChildCount() - 1);
    }

    public override void _EnterTree() {
        base._EnterTree();
        if (Instance == null) {
            Instance = this;
        } else {
            Instance.QueueFree();
        }
    }

    public DialogueBoxUI GetDialogueBoxUI() {
            return dialogueBoxUI;
    }

    public PlayerChoicesBoxUI GetPlayerChoicesBoxUI() {
        return playerChoicesBoxUI;
    }
}
