using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public partial class PlayerChoiceButton : MarginContainer, IInteractableUI {
  public DialogueObject dialogueObject { get; private set; }

  private InteractableUIButton button;
  private RichTextLabel textLabel;
  private Color normalColor = Colors.White;
  private Color hoverColor = Colors.Yellow;
  private const float BUTTON_WIDTH = 1000; // Adjust as needed
  private const float SINGLE_LINE_HEIGHT = 40; // Adjust based on your font size
  private const int LINE_SEPARATION = 5;
  public StyleBoxFlat normalStyleBox;
  public StyleBoxFlat hoverStyleBox;
  public InteractableUIButton Button => button;


  // i think that if we select a player choices and hit ui_accept on keyboard or gamepad
  //Interact() is triggered, BUT if it's clicked with the mouse, it directly calls the
  //todo "pressed" signal, i need to do a test

  public async Task Interact() {
    OnButtonPressed();
    await Task.CompletedTask;
  }

  public override void _EnterTree() {
    base._EnterTree();
    GD.Print($"PlayerChoiceButton {GetInstanceId()} entered scene tree");
  }

  public override void _ExitTree() {
    base._ExitTree();
    button.MouseEntered -= OnMouseEntered;
    button.MouseExited -= OnMouseExited;
    button.Pressed -= OnButtonPressed;
    GD.Print($"PlayerChoiceButton {GetInstanceId()} events unsubscribed");
    GD.Print($"PlayerChoiceButton {GetInstanceId()} exited scene tree");
  }


  public override void _Ready() {

    GD.Print($"PlayerChoiceButton {GetInstanceId()} setup started");

    // Set up the MarginContainer (this)
    // SizeFlagsHorizontal = SizeFlags.Fill;
    // SizeFlagsVertical = SizeFlags.ExpandFill;

    // Set up margins
    AddThemeConstantOverride("margin_left", 10);
    AddThemeConstantOverride("margin_right", 10);
    // AddThemeConstantOverride("margin_top", 5);
    // AddThemeConstantOverride("margin_bottom", 5);

    // Create and add TextureButton
    button = new InteractableUIButton {
      SizeFlagsHorizontal = SizeFlags.Fill,
      SizeFlagsVertical = SizeFlags.ExpandFill,
      //CustomMinimumSize = new Vector2(BUTTON_WIDTH, SINGLE_LINE_HEIGHT) // Set initial minimum size

    };
    AddChild(button);

    normalStyleBox = new StyleBoxFlat {
      BgColor = new Color(0, 0, 0, 0), // Fully transparent
      //DO NOT DELETE, BorderColor is for debug purposes only
      BorderColor = Colors.Red,
      BorderWidthBottom = 2,
      BorderWidthTop = 2,
      BorderWidthLeft = 2,
      BorderWidthRight = 2,
      CornerRadiusTopLeft = 5,
      CornerRadiusTopRight = 5,
      CornerRadiusBottomRight = 5,
      CornerRadiusBottomLeft = 5
    };

    hoverStyleBox = new StyleBoxFlat {
      BgColor = new Color(0f, 0.3f, 0.3f, 0.3f),
      //DO NOT DELETE, BorderColor is for debug purposes only
      BorderColor = Colors.Red,
      BorderWidthBottom = 2,
      BorderWidthTop = 2,
      BorderWidthLeft = 2,
      BorderWidthRight = 2, // Lighter, more opaque on hover
      CornerRadiusTopLeft = 5,
      CornerRadiusTopRight = 5,
      CornerRadiusBottomRight = 5,
      CornerRadiusBottomLeft = 5
    };

    button.AddThemeStyleboxOverride("normal", normalStyleBox);
    button.AddThemeStyleboxOverride("hover", hoverStyleBox);

    // Create and add RichTextLabel
    textLabel = new RichTextLabel {
      FitContent = true,
      AutowrapMode = TextServer.AutowrapMode.WordSmart,
      SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
      SizeFlagsVertical = SizeFlags.ShrinkCenter,
      MouseFilter = MouseFilterEnum.Ignore, // Allow events to pass through
      //CustomMinimumSize = new Vector2(BUTTON_WIDTH, 0)
    };
    button.AddChild(textLabel);

    // Set font and size
    var fontName = "Open Sans";
    var fontSize = 28;
    var font = new SystemFont();
    font.FontNames = new string[] { fontName };
    font.Oversampling = 1.0f;
    textLabel.AddThemeFontOverride("normal_font", font);
    // textLabel.AddThemeFontSizeOverride("normal_font_size", fontSize);
    textLabel.AddThemeFontSizeOverride("normal_font_size", 55);
    // Set initial text color
    textLabel.AddThemeColorOverride("default_color", normalColor);
    textLabel.AddThemeConstantOverride("line_separation", LINE_SEPARATION);

    // Set up the button-like behavior
    button.MouseEntered += OnMouseEntered;
    button.MouseExited += OnMouseExited;
    button.Pressed += OnButtonPressed;

    GD.Print($"PlayerChoiceButton {GetInstanceId()} setup completed");
  }


  public void ApplyStyle(bool isHighlighted) {
    var styleBox = isHighlighted ? hoverStyleBox : normalStyleBox;
    button.AddThemeStyleboxOverride("normal", styleBox);
    button.AddThemeStyleboxOverride("hover", styleBox);
    button.AddThemeStyleboxOverride("focus", styleBox);

    textLabel.AddThemeColorOverride("default_color", isHighlighted ? hoverColor : normalColor);
    Scale = isHighlighted ? new Vector2(1.005f, 1.005f) : Vector2.One;
  }

  public void SetText(string text) {
    textLabel.Text = text;
    CallDeferred(nameof(UpdateSize));
    UpdateSize();
  }

  private void UpdateSize() {

    // We update the RichTextLabel width to match the parent-s texture button. There seems to be a bug where its customMinimumSize 
    //gets preference over its size so we need to update the customMinimumSize to the new size of the parent TextureButton, 
    //that autosizes each time that the PlayerChoicesBoxUI needs to display a different number of PlayerChoiceButtons.

    textLabel.CustomMinimumSize = new Vector2(BUTTON_WIDTH - GetThemeConstant("margin_left") - GetThemeConstant("margin_right"), 0);

    textLabel.Size = Vector2.Zero;
    var contentSize = textLabel.GetMinimumSize();

    float buttonHeight = Math.Max(contentSize.Y, SINGLE_LINE_HEIGHT);
    CustomMinimumSize = new Vector2(BUTTON_WIDTH, buttonHeight + GetThemeConstant("margin_top") + GetThemeConstant("margin_bottom"));

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
  }

  private void OnMouseExited() {
    Scale = Vector2.One;
    textLabel.AddThemeColorOverride("default_color", normalColor);
  }

  private void OnButtonPressed() {
    var dialogueManager = DialogueManager.Instance;
    dialogueManager.OnPlayerChoiceButtonUIPressed(dialogueObject);
  }
}