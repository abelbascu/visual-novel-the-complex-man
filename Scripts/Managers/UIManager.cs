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
    public MainMenu mainMenu;
    public InGameMenuButton inGameMenuButton;
    public Control menuOverlay;
    public SaveGameScreen saveGameScreen;


    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Ignore;

        mainMenu = GetNode<MainMenu>("MainMenu");
        mainMenu.Hide();

        inGameMenuButton = GetNode<InGameMenuButton>("InGameMenuButton");
        inGameMenuButton.Hide();

        saveGameScreen = GetNode<SaveGameScreen>("SaveGameScreen");
        saveGameScreen.Hide();

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

        // Overlay mask to filter out input when inggame menu is open
        menuOverlay = new Control {
            Name = "MenuOverlay",
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
            AnchorRight = 1,
            AnchorBottom = 1,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both
        };

        CallDeferred(nameof(SetupNodeOrder));
        CallDeferred(nameof(EnsureOverlayOnTop));

        // Set up input handling for the overlay
        menuOverlay.Connect("gui_input", new Callable(this, nameof(OnOverlayGuiInput)));

        AddChild(menuOverlay);
    }

    private void SetupNodeOrder() {
        // Ensure VisualsManager is below UI elements in the scene tree
        var visualManager = GetNode<VisualManager>("../VisualManager");
        GetParent().MoveChild(visualManager, 0);
        // Move this UIManager to be the last child (top layer)
        GetParent().MoveChild(this, GetParent().GetChildCount() - 1);
    }

    private void OnOverlayGuiInput(InputEvent @event) {
        // Consume all input events to ensure no other UI elements are clickable
        // this is for when the ingame menu is open
        GetViewport().SetInputAsHandled();
    }

    private void EnsureOverlayOnTop() {
        // Move the overlay to be the last child (on top of other UI elements)
        MoveChild(menuOverlay, GetChildCount() - 1);
        // Ensure the ingame menu and the ingame menu icon are on top of the overlay
        MoveChild(mainMenu, GetChildCount() - 1);
        //for whatever reason i need to put this line last for the ingame menu button to work
        //if i put it between the menuOverlay and mainMenu lines, it doesn't work.
        MoveChild(inGameMenuButton, GetChildCount() - 1);
        MoveChild(saveGameScreen, GetChildCount() - 1);

    }

    // Call this method whenever you show or hide UI elements
    public void UpdateUILayout() {
        EnsureOverlayOnTop();
    }

    public void DisplayMainMenu() {
        mainMenu.Show();
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
