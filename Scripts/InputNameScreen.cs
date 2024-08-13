using Godot;
using System;

public partial class InputNameScreen : Control {
    private RichTextLabel questionLabel;
    private LineEdit nameInput;
    private ConfirmationDialog confirmationDialog;
    private string username;
    private ColorRect fadeRect;
    private AnimationPlayer animationPlayer;

    [Export] public float FadeDuration { get; set; } = 2.0f;

    public override void _Ready() {
        // Get references to existing nodes
        var marginContainer = GetNode<MarginContainer>("MarginContainer");
        var vBoxContainer = marginContainer.GetNode<VBoxContainer>("MarginContainer1/VBoxContainer");
        questionLabel = vBoxContainer.GetNode<RichTextLabel>("RichTextLabel");
        nameInput = vBoxContainer.GetNode<LineEdit>("LineEdit");
        confirmationDialog = marginContainer.GetNode<ConfirmationDialog>("MarginContainer2/ConfirmationDialog");

       // confirmationDialog.DialogText = $"[center]Are you sure that this is your final name?[/center]\n[center]You won't be able to change it during this current play![/center]";
        confirmationDialog.CancelButtonText = "No, this is not my name!.\nLet me change it!";
        confirmationDialog.OkButtonText = "Yes, this is my name!.\nLet me enter the tavern!";

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
        var richTextLabel = new RichTextLabel();
        richTextLabel.BbcodeEnabled = true;
        richTextLabel.FitContent = true;
        richTextLabel.Text = "[center]Are you sure that this is your final name?[/center]\n[center]You won't be able to change it during this current play![/center]";
        
        richTextLabel.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
        
        // Add some vertical margin to move the text away from the title
        richTextLabel.AddThemeConstantOverride("margin_top", 50);
        richTextLabel.AddThemeConstantOverride("margin_bottom", 50);
  
        confirmationDialog.AddChild(richTextLabel);

        //confirmationDialog.CancelButtonText = "No, this is not my name!\nLet me change it!";
        //confirmationDialog.OkButtonText = "Yes, this is my name!\nLet me enter the tavern!";
    }


    private void SetupFadeEffect() {
        fadeRect = new ColorRect();
        fadeRect.Color = new Color(0, 0, 0, 0); // Start fully transparent
        fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(fadeRect);

        animationPlayer = new AnimationPlayer();
        AddChild(animationPlayer);

        var animationLibrary = new AnimationLibrary();

        // Create fade out animation
        var fadeOutAnim = new Animation();
        var fadeOutTrack = fadeOutAnim.AddTrack(Animation.TrackType.Value);
        fadeOutAnim.TrackSetPath(fadeOutTrack, "FadeRect:color:a");
        fadeOutAnim.TrackInsertKey(fadeOutTrack, 0, 0);
        fadeOutAnim.TrackInsertKey(fadeOutTrack, FadeDuration, 1);
        animationLibrary.AddAnimation("fade_out", fadeOutAnim);

        // Create fade in animation
        var fadeInAnim = new Animation();
        var fadeInTrack = fadeInAnim.AddTrack(Animation.TrackType.Value);
        fadeInAnim.TrackSetPath(fadeInTrack, "FadeRect:color:a");
        fadeInAnim.TrackInsertKey(fadeInTrack, 0, 1);
        fadeInAnim.TrackInsertKey(fadeInTrack, FadeDuration, 0);
        animationLibrary.AddAnimation("fade_in", fadeInAnim);

        // Add the library to the AnimationPlayer
        animationPlayer.AddAnimationLibrary("", animationLibrary);
    }

    public override void _UnhandledKeyInput(InputEvent @event) {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Enter) {
            ShowConfirmationDialog();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnNameSubmitted(string newText) {
        ShowConfirmationDialog();
    }

    private void ShowConfirmationDialog() {
        username = nameInput.Text;
        if (!string.IsNullOrWhiteSpace(username)) {
            //confirmationDialog.DialogText = $"Are you sure that '{username}' is your name? It can be a curse or a blessing...";
            confirmationDialog.Visible = true; // Make sure the dialog is visible
            //confirmationDialog.PopupCentered();
        } else {
            // Optionally, provide feedback if the name is empty
            GD.Print("Please enter a name before confirming.");
        }
    }

    private void OnConfirmName() {
        GD.Print($"Name confirmed: {username}");
        nameInput.Text = "";
        confirmationDialog.Visible = false; // Hide the dialog after confirmation
        FadeOut();
        Hide();

        UIManager.Instance.inGameMenuButton.Show();
        DialogueManager.Instance.currentDialogueID = DialogueManager.STARTING_DIALOGUE_ID;
        DialogueManager.Instance.currentConversationID = DialogueManager.STARTING_CONVO_ID;
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject
            (DialogueManager.Instance.currentConversationID, DialogueManager.Instance.currentDialogueID);
        DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);
        GameStateManager.Instance.ToggleAutosave(true);
    }

    private void OnCancelConfirmation() {
        confirmationDialog.Visible = false; // Hide the dialog when cancelled
        nameInput.GrabFocus();
    }

    private void FadeOut() {
        fadeRect.Visible = true;
        animationPlayer.Play("fade_out");
        animationPlayer.AnimationFinished += OnFadeOutFinished;
    }

    private void OnFadeOutFinished(StringName animName) {
        if (animName == "fade_out") {
            animationPlayer.AnimationFinished -= OnFadeOutFinished;
            GD.Print("Fade out complete. Load your new scene or background here.");
            // You would typically change scenes or load new content here
            // For demonstration, we'll just call FadeIn after a short delay
            GetTree().CreateTimer(1.0).Timeout += () => FadeIn();
        }
    }

    public void FadeIn() {
        fadeRect.Visible = true;
        animationPlayer.Play("fade_in");
        animationPlayer.AnimationFinished += OnFadeInFinished;
    }

    private void OnFadeInFinished(StringName animName) {
        if (animName == "fade_in") {
            animationPlayer.AnimationFinished -= OnFadeInFinished;
            fadeRect.Visible = false;
            GD.Print("Fade in complete. New content should now be visible.");
        }
    }
}