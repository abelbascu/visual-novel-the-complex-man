using Godot;

public partial class PlayerChoiceButton : MarginContainer {
    public DialogueObject dialogueObject { get; private set; }
    private RichTextLabel textLabel;
    private Color normalColor = Colors.White;
    private Color hoverColor = Colors.Yellow;
    private ColorRect background;
    private const float BUTTON_WIDTH = 780; // Adjust as needed
    private const float SINGLE_LINE_HEIGHT = 40; // Adjust based on your font size
    private bool isHovered = false;

    public override void _Ready() {
        // Set up the MarginContainer (this)
        SizeFlagsHorizontal = SizeFlags.Fill;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;

        MouseFilter = MouseFilterEnum.Stop;

        // Set up margins
        AddThemeConstantOverride("margin_left", 10);
        AddThemeConstantOverride("margin_right", 10);
        AddThemeConstantOverride("margin_top", 5);
        AddThemeConstantOverride("margin_bottom", 5);

        // Create and add background ColorRect
        background = new ColorRect {
            Color = new Color(0.2f, 0.2f, 0.2f, 0.5f), // Semi-transparent dark gray
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.Fill
        };
        AddChild(background);

        // Create and add RichTextLabel
        textLabel = new RichTextLabel {
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.Fill,
            MouseFilter = MouseFilterEnum.Ignore, // Allow events to pass through
            CustomMinimumSize = new Vector2(BUTTON_WIDTH, 0)
        };
        AddChild(textLabel);

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
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        //GuiInput += OnGuiInput;
    }

    public void SetText(string text) {
        textLabel.Text = text;
        CallDeferred(nameof(UpdateSize));
    }

    private void UpdateSize() {
        float contentHeight = textLabel.GetContentHeight();
        float buttonHeight = contentHeight > SINGLE_LINE_HEIGHT ? contentHeight : SINGLE_LINE_HEIGHT;
        CustomMinimumSize = new Vector2(BUTTON_WIDTH, buttonHeight + GetThemeConstant("margin_top") + GetThemeConstant("margin_bottom"));
    }

    public void SetDialogueObject(DialogueObject dialogObj) {
        this.dialogueObject = dialogObj;
    }

    public bool HasMatchingDialogueObject(DialogueObject otherDialogueObject) {
        return this.dialogueObject.ID == otherDialogueObject.ID;
    }

    private void OnMouseEntered() {
         isHovered = true;
        Scale = new Vector2(1.005f, 1.005f);
        textLabel.AddThemeColorOverride("default_color", hoverColor);
    }

    private void OnMouseExited() {
        isHovered = false;
        Scale = Vector2.One;
        textLabel.AddThemeColorOverride("default_color", normalColor);
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed) {
            OnButtonPressed();
        } else if (@event is InputEventMouseMotion) {
            // Check if the mouse is still inside the button
            var localMousePos = GetLocalMousePosition();
            if (GetRect().HasPoint(localMousePos)) {
                if (!isHovered) {
                    OnMouseEntered();
                }
            } else {
                if (isHovered) {
                    OnMouseExited();
                }
            }
        }
    }

        private void OnButtonPressed() {
            var dialogueManager = DialogueManager.Instance;
            dialogueManager.OnPlayerButtonUIPressed(dialogueObject);
        }
    }