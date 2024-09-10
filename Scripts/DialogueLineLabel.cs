using Godot;
using System;

public partial class DialogueLineLabel : RichTextLabel, IInteractableUI {

    public Action Pressed { get; set; }
    private const int LINE_SEPARATION = 5;
    public bool IsInteractable => Visible;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Stop;
        SetProcessInput(true);

        Pressed += OnPressed;

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
            (mouseEvent.ButtonIndex == MouseButton.Left ||
            mouseEvent.ButtonIndex == MouseButton.Right) &&
            mouseEvent.Pressed) {
            // Check if the click is within the bounds of the Label
            // if (this.GetGlobalRect().HasPoint(GetGlobalMousePosition())) {
            Pressed.Invoke();
            GD.Print("Label area clicked!");
            GetViewport().SetInputAsHandled(); // Prevent the click from propagating
            //}
        }
    }

    public void OnPressed() {
        DialogueManager.Instance.OnDialogueBoxUIPressed();
    }

    public void SetText(string text) {
        Text = $"[left]{text}[/left]";
    }
}
