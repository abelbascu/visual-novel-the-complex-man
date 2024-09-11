using Godot;
using System;

[GlobalClass]
public partial class InteractableUITextureButton : TextureButton, IInteractableUI {

  public bool IsInteractable => Visible;
}