using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public interface IInteractableUI
{
   Task Interact();

}

// public abstract partial class InteractableUIControl : Control, IInteractableUI
// {
//     public abstract async Task Interact();
  
// }
