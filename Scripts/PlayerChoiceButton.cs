using Godot;

public partial class PlayerChoiceButton : TextureButton {
    public DialogueObject dialogueObject { get; private set; }
    private RichTextLabel textLabel;
    private Tween currentTween;
    private Color normalColor = Colors.White;
    private Color hoverColor = Colors.Yellow;

    public override void _Ready() {
        textLabel = new RichTextLabel {
            BbcodeEnabled = true,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Pass,
            
        };

        AddChild(textLabel);

        // Set font and font size
        var fontName = "Open Sans";
        var fontSize = 32;
        var font = new SystemFont();
        font.FontNames = new string[] { fontName };
        font.Oversampling = 1.0f; // Adjust if needed for better rendering
        textLabel.AddThemeFontOverride("normal_font", font);
        textLabel.AddThemeFontSizeOverride("normal_font_size", fontSize);

        // Reduce interline space
        textLabel.AddThemeConstantOverride("line_separation", -10); // Adjust this value as needed

         // Set initial text color
        textLabel.AddThemeColorOverride("default_color", normalColor);


        SizeFlagsHorizontal = SizeFlags.Fill;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        Pressed += OnButtonPressed;

        TextureNormal = null; // Remove background
    }

    public void SetDialogueObject(DialogueObject dialogObj) {
        this.dialogueObject = dialogObj;
    }

    public void SetText(string text) {
        textLabel.Text = $"[left]{text}[/left]";
        CustomMinimumSize = new Vector2(20, textLabel.Size.Y);
    }

    private void AdjustSize() {

        CustomMinimumSize = new Vector2(200, textLabel.Size.Y);
        Size = CustomMinimumSize;
    }

    private void OnButtonPressed() {
        var dialogueManager = DialogueManager.Instance;
        dialogueManager.OnPlayerButtonUIPressed(dialogueObject);
        QueueFree();
    }

    public bool HasMatchingDialogueObject(DialogueObject otherDialogueObject) {
        return this.dialogueObject.ID == otherDialogueObject.ID;
    }

    private void OnMouseEntered() {
        ScaleTo(new Vector2(1.005f, 1.005f));
        textLabel.AddThemeColorOverride("default_color", hoverColor);
    }

    private void OnMouseExited() {
        ScaleTo(Vector2.One);
        textLabel.AddThemeColorOverride("default_color", normalColor);
    }

    private void ScaleTo(Vector2 targetScale) {
        if (currentTween != null && currentTween.IsValid()) {
            currentTween.Kill();
        }

        currentTween = CreateTween();
        currentTween.TweenProperty(this, "scale", targetScale, 0.1f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}