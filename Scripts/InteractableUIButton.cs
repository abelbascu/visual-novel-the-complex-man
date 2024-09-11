using Godot;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

[GlobalClass]
public partial class InteractableUIButton : Button, IInteractableUI {

    public Task Interact() {
    EmitSignal(SignalName.Pressed);
    return  Task.CompletedTask;
  }
}