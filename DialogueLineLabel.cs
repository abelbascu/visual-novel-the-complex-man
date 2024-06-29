using Godot;
using System;

public partial class DialogueLineLabel : Label {

	public Action LabelPressed;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

		LabelPressed += OnLabelPressed;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            mouseEvent.Pressed) {
            // Check if the click is within the bounds of the Label
            if (this.GetGlobalRect().HasPoint(GetGlobalMousePosition())) {
                LabelPressed.Invoke();
                GD.Print("Label area clicked!");
            }
        }
    }

	public void OnLabelPressed() {
		var dialogueManager = DialogueManager.Instance;
        dialogueManager.OnDialogueBoxUIPressed();
	}
}
