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
    public static Action StartButtonPressed;
    private const int UI_BOTTOM_POSITION = 200; //starting at the bottom of the screen, we subtract this value to position the Y screen position of the dilaogue box  
    private MainMenu mainMenu;
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
        mainMenu = GetNode<MainMenu>("MainMenu");
        mainMenu.StartButtonPressed += OnStartButtonPressed;
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

    public void DisplayDialogue(DialogueObject currentDialogueObject) {
        if (DialogueManager.Instance.isDialogueBeingPrinted) //is we are currently printing a dialogue in the DialogueBoxUI, do nothing
            return;
        DialogueManager.Instance.isDialogueBeingPrinted = true;
        if (dialogueBoxUI == null) {
            DisplayDialogueBoxUI();
        }
        if (dialogueBoxUI != null)
            dialogueBoxUI.Show();
        if (playerChoicesBoxUI != null)
            playerChoicesBoxUI.Hide();

        dialogueBoxUI.DisplayDialogueLine(currentDialogueObject, DialogueManager.languageCode);
    }

    public void DisplayPlayerChoices(List<DialogueObject> playerChoices, Action<bool> setIsPlayerChoiceBeingPrinted) {
        if (playerChoicesBoxUI == null) {
            //before adding the player choices, we need to create the container VBox
            DisplayPlayerChoicesBoxUI();
        }
        if (playerChoicesBoxUI != null) {
            //ensure the container is visible
            playerChoicesBoxUI.Show();
            //let's hide the dialogue box, that's used to displaye narrator/NPC texts, not the player's
            if (dialogueBoxUI != null)
                dialogueBoxUI.Hide();

            setIsPlayerChoiceBeingPrinted(true);
            playerChoicesBoxUI.DisplayPlayerChoices(playerChoices, DialogueManager.languageCode);
            setIsPlayerChoiceBeingPrinted(false);
        }
    }

    public void DisplayDialogueBoxUI() {
        // Ensure the dialogue box is visible
        dialogueBoxUI.Visible = true;
        //once all chars of the dialogue text are displayed in the container, we can show the next dialogue.
        dialogueBoxUI.FinishedDisplayingDialogueLine += DialogueManager.Instance.OnTextBoxFinishedDisplayingDialogueLine;
    }

    public void DisplayPlayerChoicesBoxUI() {
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoicesBoxUI.tscn");
        Node instance = scene.Instantiate();
        AddChild(instance);
        //VBoxContainer playerChoìces = instance as VBoxContainer;
        playerChoicesBoxUI = instance as PlayerChoicesBoxUI;
        playerChoicesBoxUI.Show();
        //once all chars of the dialogue text are displayed in the container, we can show the next line.
        playerChoicesBoxUI.FinishedDisplayingPlayerChoice += DialogueManager.Instance.OnTextBoxFinishedDisplayingPlayerChoices;
    }

    public void OnStartButtonPressed() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject(DialogueManager.Instance.currentConversationID, DialogueManager.Instance.currentDialogueID);
        DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);
    }
}
