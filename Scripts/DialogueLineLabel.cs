using Godot;
using System;

public partial class DialogueLineLabel : RichTextLabel {

    public Action LabelPressed;
     private const int LINE_SEPARATION = 5;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

        LabelPressed += OnLabelPressed;

        BbcodeEnabled = true;
        FitContent = true;
        ScrollActive = false;

        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        AddThemeConstantOverride("line_separation", LINE_SEPARATION);
        AddThemeFontSizeOverride("normal_font_size", 28);
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed) {
            // Check if the click is within the bounds of the Label
            // if (this.GetGlobalRect().HasPoint(GetGlobalMousePosition())) {
            LabelPressed.Invoke();
            GD.Print("Label area clicked!");
            //}
        }
    }

    public void OnLabelPressed() {
        DialogueManager.Instance.OnDialogueBoxUIPressed();
    }

    public void SetText(string text)
    {
        Text = $"[left]{text}[/left]";
    }
}
