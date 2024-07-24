using Godot;

public partial class PlayerChoiceButton : MarginContainer {
    public DialogueObject dialogueObject { get; private set; }
    private TextureButton button;
    private RichTextLabel textLabel;
    private Color normalColor = Colors.White;
    private Color hoverColor = Colors.Yellow;
    private const float BUTTON_WIDTH = 700; // Adjust as needed
    private const float SINGLE_LINE_HEIGHT = 40; // Adjust based on your font size

    public override void _Ready() {
        // Set up the MarginContainer (this)
        SizeFlagsHorizontal = SizeFlags.Fill;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;

        // Set up margins
        AddThemeConstantOverride("margin_left", 10);
        AddThemeConstantOverride("margin_right", 10);
        AddThemeConstantOverride("margin_top", 5);
        AddThemeConstantOverride("margin_bottom", 5);

        // Create and add TextureButton
        button = new TextureButton {
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(BUTTON_WIDTH, SINGLE_LINE_HEIGHT) // Set initial minimum size

        };
        AddChild(button);

        var styleBox = new StyleBoxFlat {
            BgColor = new Color(0.2f, 0.2f, 0.2f, 1f), // Semi-transparent dark gray
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusBottomLeft = 5
        };

        button.AddThemeStyleboxOverride("normal", styleBox);

        // Create and add RichTextLabel
        textLabel = new RichTextLabel {
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore, // Allow events to pass through
            CustomMinimumSize = new Vector2(BUTTON_WIDTH, 0)
        };
        button.AddChild(textLabel);

        // Set font and size
        var fontName = "Open Sans";
        var fontSize = 28;
        var font = new SystemFont();
        font.FontNames = new string[] { fontName };
        font.Oversampling = 1.0f;
        textLabel.AddThemeFontOverride("normal_font", font);
        textLabel.AddThemeFontSizeOverride("normal_font_size", fontSize);

        // Set initial text color
        textLabel.AddThemeColorOverride("default_color", normalColor);

        // Set up the button-like behavior
        button.MouseEntered += OnMouseEntered;
        button.MouseExited += OnMouseExited;
        button.Pressed += OnButtonPressed;
    }

    public void SetText(string text) {
        textLabel.Text = text;
        CallDeferred(nameof(UpdateSize));
    }

    private void UpdateSize() {
        float contentHeight = textLabel.GetContentHeight();
        float buttonHeight = contentHeight > SINGLE_LINE_HEIGHT ? contentHeight : SINGLE_LINE_HEIGHT;
        CustomMinimumSize = new Vector2(BUTTON_WIDTH, buttonHeight + GetThemeConstant("margin_top") + GetThemeConstant("margin_bottom"));
      // Update the button's size to match the content
        button.CustomMinimumSize = new Vector2(BUTTON_WIDTH, buttonHeight);
    }

    public void SetDialogueObject(DialogueObject dialogObj) {
        this.dialogueObject = dialogObj;
    }

    public bool HasMatchingDialogueObject(DialogueObject otherDialogueObject) {
        return this.dialogueObject.ID == otherDialogueObject.ID;
    }

    private void OnMouseEntered() {
        Scale = new Vector2(1.005f, 1.005f);
        textLabel.AddThemeColorOverride("default_color", hoverColor);
        var styleBox = (StyleBoxFlat)button.GetThemeStylebox("normal");
        styleBox.BgColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    }

    private void OnMouseExited() {
        Scale = Vector2.One;
        textLabel.AddThemeColorOverride("default_color", normalColor);
        var styleBox = (StyleBoxFlat)button.GetThemeStylebox("normal");
        styleBox.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    }

    private void OnButtonPressed() {
        var dialogueManager = DialogueManager.Instance;
        dialogueManager.OnPlayerButtonUIPressed(dialogueObject);
    }
}