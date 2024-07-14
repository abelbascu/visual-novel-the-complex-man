using Godot;

public partial class PlayerChoiceButton : TextureButton {

    public DialogueObject dialogueObject { get; private set; }
    private VBoxContainer parentContainer;
    private RichTextLabel textLabel;

    private Tween currentTween;

    public override void _Ready() {

        Pressed += OnButtonPressed;

        parentContainer = GetParent<VBoxContainer>();

        // Ensure the button can receive input
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;

        // Connect to gui_input instead of Pressed
        GuiInput += OnGuiInput;

        // Create and add RichTextLabel
        textLabel = new RichTextLabel {
            BbcodeEnabled = true,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Pass // Ignore mouse events
        };
        AddChild(textLabel);

        // Remove background
        TextureNormal = null;

        // Add hover effect
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        // Additional customization
        CustomMinimumSize = new Vector2(200, 0); // Set minimum width, let height adjust
    }

    private void OnGuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed) {
            GD.Print("Button clicked");
            OnButtonPressed();
        }
    }

    public void SetDialogueObject(DialogueObject dialogObj) {
        this.dialogueObject = dialogObj;
    }

    public void SetText(string text) {
        textLabel.Text = $"[left]{text}[/left]";
    }

    private void OnButtonPressed() {
        var dialogueManager = DialogueManager.Instance;
        dialogueManager.OnPlayerButtonUIPressed(dialogueObject);
        // Remove the button from its parent container
        CallDeferred("RemoveButton");

    }

    private void RemoveButton() {
        if (parentContainer != null) {
            parentContainer.RemoveChild(this);
            QueueFree();
        }
    }

    public bool HasMatchingDialogueObject(DialogueObject otherDialogueObject) {
        return this.dialogueObject.ID == otherDialogueObject.ID;
    }

    private void OnMouseEntered()
    {
        ScaleTo(new Vector2(1.1f, 1.1f));
    }

    private void OnMouseExited()
    {
        ScaleTo(Vector2.One);
    }

        private void ScaleTo(Vector2 targetScale)
    {
        if (currentTween != null && currentTween.IsValid())
        {
            currentTween.Kill();
        }

        currentTween = CreateTween();
        currentTween.TweenProperty(this, "scale", targetScale, 0.2f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}