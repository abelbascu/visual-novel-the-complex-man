using Godot;

public partial class PlayerChoiceButton : Button {

    public DialogueObject dialogueObject {get; private set;}
    private VBoxContainer parentContainer;

    public override void _Ready() {
        Pressed += OnButtonPressed;
        parentContainer = GetParent<VBoxContainer>();
    }

    public PlayerChoiceButton(DialogueObject dialogObj) {
        this.dialogueObject = dialogObj;
    }

    private void OnButtonPressed() {
        var dialogueManager = GetNode<DialogueManager>("/root/DialogueManager");
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
}