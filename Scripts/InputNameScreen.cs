using Godot;
using System;
using static GameStateMachine;
using UIHelpers;

public partial class InputNameScreen : Control {
    private RichTextLabel questionLabel;
    private LineEdit nameInput;
    private ConfirmationDialog confirmationDialog;
    private string username;
    private RichTextLabel richTextLabel;
    private MarginContainer marginContainer;
    private bool isNameConfirmed = false;

    private UIFadeIn fadeIn;
    private UIFadeOut fadeOut;

    [Export] public float FadeDuration { get; set; } = 0.5f;

    private void SetupConfirmationDialogTheme() {
        confirmationDialog.Confirmed += OnConfirmName;
        confirmationDialog.Canceled += OnCancelConfirmation;
        confirmationDialog.Visible = false; // Ensure the dialog is initially hidden
        // Prevent the dialog from closing itself, we'll handle that
        // confirmationDialog.GetOkButton().Pressed += () => confirmationDialog.Visible = false;
        // confirmationDialog.GetCancelButton().Pressed += () => confirmationDialog.Visible = false;
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

    public override void _Ready() {

        marginContainer = GetNode<MarginContainer>("MarginContainer");
        var vBoxContainer = marginContainer.GetNode<VBoxContainer>("MarginContainer1/VBoxContainer");
        questionLabel = vBoxContainer.GetNode<RichTextLabel>("RichTextLabel");
        nameInput = vBoxContainer.GetNode<LineEdit>("LineEdit");
        confirmationDialog = marginContainer.GetNode<ConfirmationDialog>("MarginContainer2/ConfirmationDialog");

        ListenForNameConfirmation();
        SetupConfirmationDialogTheme();

        this.Visible = false;
    }

    public void Show() {
        base.Show();
        ResetNameInputScreen();
        CallDeferred(nameof(SetInitialFocus));
        FadeIn();
    }

    private void FadeIn() {
        fadeIn = new UIFadeIn(this);
        fadeIn.FadeDuration = FadeDuration;
        fadeIn.FadeIn();
    }

    private void FadeOut() {
        fadeOut = new UIFadeOut(this);
        fadeOut.FadeDuration = FadeDuration;
        fadeOut.FadeOut(OnFadeOutFinished);
    }

    private void ResetNameInputScreen() {
        isNameConfirmed = false;
        //process again
        //confirmationDialog.ProcessMode = Node.ProcessModeEnum.Inherit;
        UIInputHelper.EnableParentChildrenInput(this);
        nameInput.Text = "";
        nameInput.Editable = true;
        nameInput.FocusMode = Control.FocusModeEnum.All;
        confirmationDialog.ProcessMode = Node.ProcessModeEnum.Inherit;
        nameInput.ProcessMode = confirmationDialog.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private void SetInitialFocus() {
        nameInput.GrabFocus();
    }

    private void ListenForNameConfirmation() {
        // nameInput.PlaceholderText = "Enter your name";
        //player confirms by hitting the OK button on the LineEdit textobx or hitting the Enter key
        nameInput.TextSubmitted += OnNameSubmitted;
        nameInput.FocusEntered += () => GD.Print("LineEdit focus entered");
        nameInput.FocusExited += () => GD.Print("LineEdit focus exited");
        nameInput.GuiInput += OnNameInputGuiInput;
    }

    //_GuiInput is called for GUI events that are not consumed by child controls,
    //that's why we listen specifically to 'nameInput' GuiInput, the box to type in the name.
    private void OnNameInputGuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
            GD.Print("Mouse clicked on LineEdit");
            nameInput.GrabFocus();
            GetViewport().SetInputAsHandled();
        }
    }


    public override void _UnhandledKeyInput(InputEvent @event) {
        if (isNameConfirmed) return;
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Enter) {
            ShowConfirmationDialog();
            GetViewport().SetInputAsHandled(); //I DON'T KNOW WHY WE NEED THIS
        }
    }

    public override void _Process(double delta) {
        if (IsVisibleInTree() && !isNameConfirmed) {
            if (!nameInput.HasFocus()) {
                GD.Print("LineEdit lost focus, attempting to regrab");
                nameInput.GrabFocus();
            }
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
        if (isNameConfirmed) return; // Prevent multiple confirmations
        isNameConfirmed = true;
        GD.Print($"Name confirmed: {username}");
        confirmationDialog.ProcessMode = Node.ProcessModeEnum.Disabled;
        nameInput.ProcessMode = confirmationDialog.ProcessMode = Node.ProcessModeEnum.Disabled;
        UIInputHelper.DisableParentChildrenInput(this);
        // Hide the confirmation dialog
        confirmationDialog.Visible = false;
        FadeOut();
    }


    private void OnCancelConfirmation() {
        confirmationDialog.Visible = false; // Hide the dialog when cancelled
        nameInput.GrabFocus();
    }

    private void OnFadeOutFinished() {
        GD.Print("Fade out complete (now black)");
        GameStateManager.Instance.Fire(Trigger.DISPLAY_NEW_GAME_DIALOGUES);
        Hide();
    }
}