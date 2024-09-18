using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class InteractableUILineEdit : LineEdit, IInteractableUI {

    public Action InteractRequested;

    public async Task Interact() {
        GrabFocus();
        InteractRequested?.Invoke();
        await Task.CompletedTask;
    }
}
