using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class InteractableUITextureButton : TextureButton, IInteractableUI {

  public async Task Interact() {
    EmitSignal(SignalName.Pressed);
    await Task.CompletedTask;
  }
}