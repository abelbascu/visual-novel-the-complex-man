using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class InteractableUITextureButton : TextureButton, IInteractableUI {

   private bool isHighlighted = false;

  public async Task Interact() {
    EmitSignal(SignalName.Pressed);
    await Task.CompletedTask;
  }

      public void SetHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();
        if (isHighlighted)
        {
            var rect = GetRect();
            DrawRect(rect, new Color(1, 1, 1, 0.5f), false, 4);
        }
    }

}