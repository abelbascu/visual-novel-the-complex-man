using Godot;
using System;

public partial class InputNameScreen : Control {
    private RichTextLabel questionLabel;
    private LineEdit nameInput;
    private ConfirmationDialog confirmationDialog;
    private string username;
    private ColorRect fadeRect;
    //private AnimationPlayer animationPlayer;
    private RichTextLabel richTextLabel;
    private MarginContainer marginContainer;

    [Export] public float FadeDuration { get; set; } = 2.0f;

    public override void _Ready() {
        // Get references to existing nodes
        marginContainer = GetNode<MarginContainer>("MarginContainer");
        var vBoxContainer = marginContainer.GetNode<VBoxContainer>("MarginContainer1/VBoxContainer");
        questionLabel = vBoxContainer.GetNode<RichTextLabel>("RichTextLabel");
        nameInput = vBoxContainer.GetNode<LineEdit>("LineEdit");
        confirmationDialog = marginContainer.GetNode<ConfirmationDialog>("MarginContainer2/ConfirmationDialog");
        // confirmationDialog.DialogText = $"[center]Are you sure that this is your final name?[/center]\n[center]You won't be able to change it during this current play![/center]";

        // Set up the nodes
        //SetupQuestionLabel();
        SetupNameInput();
        SetupConfirmationDialog();
        SetupFadeEffect();

        Hide();
    }

    public void Show() {
        base.Show();
        CallDeferred(nameof(SetInitialFocus));
        FadeIn();
    }

    private void SetInitialFocus() {
        nameInput.GrabFocus();
    }

    private void SetupQuestionLabel() {
        // questionLabel.Text = "What's your name, traveller?";
    }

    private void SetupNameInput() {
        // nameInput.PlaceholderText = "Enter your name";
        nameInput.TextSubmitted += OnNameSubmitted;
    }

    private void SetupConfirmationDialog() {
        confirmationDialog.Confirmed += OnConfirmName;
        confirmationDialog.Canceled += OnCancelConfirmation;
        confirmationDialog.Visible = false; // Ensure the dialog is initially hidden
                                            // Center the text in the confirmation dialog 

        // Create a MarginContainer to hold the RichTextLabel
        var marginContainer = new MarginContainer();
        marginContainer.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
        marginContainer.AddThemeConstantOverride("margin_top", 50);
        marginContainer.AddThemeConstantOverride("margin_bottom", 50);
        confirmationDialog.AddChild(marginContainer);
        confirmationDialog.MoveChild(marginContainer, 1); // Move it just after the title

        richTextLabel = new RichTextLabel();
        richTextLabel.BbcodeEnabled = true;
        richTextLabel.FitContent = true;
        richTextLabel.AnchorsPreset = (int)Control.LayoutPreset.FullRect;

        // Add some vertical margin to move the text away from the title
        richTextLabel.AddThemeConstantOverride("margin_top", 150);
        richTextLabel.AddThemeConstantOverride("margin_bottom", 150);

        marginContainer.AddChild(richTextLabel);
    }


    private void SetupFadeEffect() {
        fadeRect = new ColorRect();
        fadeRect.Color = Colors.Black;
        fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        //fadeRect.Color = new Color(0, 0, 0, 0); // Start fully transparent
        fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(fadeRect);
    }

    public override void _UnhandledKeyInput(InputEvent @event) {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Enter) {
            ShowConfirmationDialog();
            GetViewport().SetInputAsHandled(); //I DON'T KNOW WHY WE NEED THIS
        }
    }

    private void OnNameSubmitted(string newText) {
        ShowConfirmationDialog();
    }

    private void ShowConfirmationDialog() {
        username = nameInput.Text;
        if (!string.IsNullOrWhiteSpace(username)) {
            //confirmationDialog.DialogText = $"Are you sure that '{username}' is your name? It can be a curse or a blessing...";
            confirmationDialog.CancelButtonText = $"No, {username} is not my name!.\nLet me change it!";
            confirmationDialog.OkButtonText = $"Yes, {username} is my name!.\nLet me enter the tavern!";
            richTextLabel.Text = $"[center]Are you sure that {username} is your final name?[/center]\n[center]You won't be able to change it during this current play![/center]";
            confirmationDialog.Visible = true; // Make sure the dialog is visible
            //confirmationDialog.PopupCentered();
        } else {
            // Optionally, provide feedback if the name is empty
            GD.Print("Please enter a name before confirming.");
        }
    }

    private void OnConfirmName() {
        GD.Print($"Name confirmed: {username}");
        FadeOut();
    }

    private void OnCancelConfirmation() {
        confirmationDialog.Visible = false; // Hide the dialog when cancelled
        nameInput.GrabFocus();
    }

    private void FadeOut() {
        GD.Print("Starting fade out (transparent to black)");
        fadeRect.Color = new Color(0, 0, 0, 0);
        fadeRect.Visible = true;
        var tween = CreateTween();
        tween.Finished += OnFadeOutFinished;
        tween.TweenProperty(fadeRect, "color:a", 1.0, FadeDuration);
    }

    private void OnFadeOutFinished() {
        GD.Print("Fade out complete (now black)");

        SetupGameElements();
        FadeInGameElements();
    }

    private void SetupGameElements() {

        // Ensure the fade rect is still visible and black
        fadeRect.Color = Colors.Black;
        fadeRect.Visible = true;

        // Hide other elements of the InputNameScreen
        foreach (var child in GetChildren()) {
            if (child != fadeRect && child is Control controlChild) {
                controlChild.Visible = false;
            }
        }

        // Set up game elements (but keep them covered by the fade rect)
        

        fadeRect.MouseFilter = MouseFilterEnum.Ignore; //AT THIS POINT WE ALLOW THE USER TO CLICK ON THE DIALOGUE BOX, AS THE FADE IN IS LONGER TO CREATE A BIT OF ZEITGEIST STORYTELLING ANTICIPATION SMOOTHNESS

        // Move the fadeRect to be on top of the new elements
        CallDeferred(nameof(PositionScreenAndStartFadeIn));
        GameStateManager.Instance.DISPLAY_NEW_GAME_DIALOGUES();
    }

    private void PositionScreenAndStartFadeIn() {
        // Get the parent (which should be UIManager)
        var uiManager = GetParent();
        if (uiManager == null) {
            GD.PrintErr("Failed to find parent UIManager. Check the scene hierarchy.");
            return;
        }
        // Move this InputNameScreen to the top of UIManager's children
        uiManager.MoveChild(this, -1);
        // Ensure this InputNameScreen covers the entire UI area
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // Start the fade in to reveal the new elements
        FadeInGameElements();
    }

    private void FadeInGameElements() {
        GD.Print("Starting fade in to reveal game elements");

        var tween = CreateTween();
        GD.Print($"staring value of crrent alpha: {fadeRect.Color.A}");
        tween.TweenProperty(fadeRect, "color:a", 0.0, 3.5f);
        tween.Finished += OnFadeInGameElementsFinished;

        var timer = GetTree().CreateTimer(FadeDuration / 2);
        timer.Timeout += () => GD.Print($"Fade in halfway point. Current alpha: {fadeRect.Color.A}");
    }

    private void OnFadeInGameElementsFinished() {
        GD.Print("Fade in complete, game elements now visible");
        fadeRect.Visible = false;
        //fadeRect.QueueFree();
        Hide();
    }

    public void FadeIn() {

        GD.Print("Starting fade in (black to transparent)");
        fadeRect.Visible = true;
        fadeRect.Color = Colors.Black;
        var tween = CreateTween();
        tween.TweenProperty(fadeRect, "color:a", 0.0, FadeDuration);
        tween.Finished += OnFadeInFinished;
    }

    private void OnFadeInFinished() {
        GD.Print("Fade in complete (now transparent)");
        fadeRect.Visible = false;

    }
}