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
    public DialogueBoxAndSpeakerTag dialogueBoxUI; //the graphical rectangle container to display the text over
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
    public InputNameScreen inputNameScreen;
    public SplashScreen splashScreen;

    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Ignore;

        splashScreen = GetNode<SplashScreen>("SplashScreen");

        mainMenu = GetNode<MainMenu>("MainMenu");
        mainMenu.Hide();

        inGameMenuButton = GetNode<InGameMenuButton>("InGameMenuButton");
        inGameMenuButton.Hide();

        saveGameScreen = GetNode<SaveGameScreen>("SaveGameScreen");
        saveGameScreen.Hide();

        inputNameScreen = GetNode<InputNameScreen>("InputNameScreen");
        inputNameScreen.Hide();

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
        dialogueBoxUIScene = ResourceLoader.Load<PackedScene>("res://Scenes/DialogueBoxAndSpeakerTag.tscn");
        dialogueBoxUI = dialogueBoxUIScene.Instantiate<DialogueBoxAndSpeakerTag>();
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

    public void ShowSplashScreen() {
        splashScreen.Show();
    }

    public void HideSplashScreen() {
        splashScreen.Hide();
    }

    public void HideAllUIElements() {
        var uiManager = this;
        foreach (Control child in uiManager.GetChildren()) {
            if (child != this && child is Control controlNode) {
                controlNode.Visible = false;
            }
        }
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

    public DialogueBoxAndSpeakerTag GetDialogueBoxUI() {
        return dialogueBoxUI;
    }

    public PlayerChoicesBoxUI GetPlayerChoicesBoxUI() {
        return playerChoicesBoxUI;
    }


    public void SetupCustomConfirmationDialog(ConfirmationDialog dialog) {
        // Create a new theme for the entire dialog
        var customTheme = new Theme();
        dialog.Theme = customTheme;

        // Style for the main panel (affects the entire dialog including top bar)
        var panelStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        customTheme.SetStylebox("panel", "Window", panelStyle);

        // Style for the close button (top-right X)
        var closeButtonStyle = new StyleBoxFlat {
            BgColor = Colors.Transparent
        };
        customTheme.SetStylebox("close", "Window", closeButtonStyle);

        // Style for the OK button with added padding
        var buttonStyle = new StyleBoxFlat {
            BgColor = Colors.DarkSlateBlue,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            BorderColor = Colors.White,
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        customTheme.SetStylebox("normal", "Button", buttonStyle);

        // Set text color for the entire dialog
        customTheme.SetColor("font_color", "Label", Colors.White);
        customTheme.SetColor("font_color", "Button", Colors.White);

        // Create and set larger font for dialog text and button
        customTheme.SetFontSize("font_size", "Label", 35);
        customTheme.SetFontSize("font_size", "Button", 35);

        // Apply theme to the dialog and its children
        ApplyThemeRecursively(dialog, customTheme);

        // Center the OK button
        var buttonContainer = dialog.GetOkButton().GetParent() as BoxContainer;
        if (buttonContainer != null) {
            buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;

            // Remove all children except the OK button
            foreach (var child in buttonContainer.GetChildren()) {
                if (child != dialog.GetOkButton()) {
                    buttonContainer.RemoveChild(child);
                    child.QueueFree();
                }
            }
        }

        // Ensure the OK button is using the correct style
        dialog.GetOkButton().AddThemeStyleboxOverride("normal", buttonStyle);
    }

    private void ApplyThemeRecursively(Node node, Theme theme) {
        if (node is Control control) {
            control.Theme = theme;
        }

        foreach (var child in node.GetChildren()) {
            ApplyThemeRecursively(child, theme);
        }
    }



    public void ApplyCustomStyleToButton(Button button) {
        var normalStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        button.AddThemeStyleboxOverride("normal", normalStyle);

        Color customBlue = new Color(
            0f / 255f,  // Red component
            71f / 255f,  // Green component
            171f / 255f   // Blue component
        );

        // Hover state
        var hoverStyle = new StyleBoxFlat {
            BgColor = customBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };

        button.AddThemeStyleboxOverride("hover", hoverStyle);


        // Pressed state
        var pressedStyle = new StyleBoxFlat {
            BgColor = Colors.DarkBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };

        button.AddThemeStyleboxOverride("pressed", pressedStyle);

        // Set font size
        button.AddThemeFontSizeOverride("font_size", 40);
    }
}
