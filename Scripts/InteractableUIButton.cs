using Godot;
using System;
using System.Reflection.Metadata;

[GlobalClass]
public partial class InteractableUIButton : Button, IInteractableUI {

  public bool IsInteractable => Visible;
}