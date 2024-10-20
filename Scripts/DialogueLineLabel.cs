using Godot;
using System;
using System.Threading.Tasks;

public partial class DialogueLineLabel : RichTextLabel, IInteractableUI {

  public Action Pressed { get; set; }
  private const int LINE_SEPARATION = 2;
  public bool IsInteractable => Visible;

  public override void _Ready() {

    CustomMinimumSize = new Vector2(900, 0);

    MouseFilter = MouseFilterEnum.Stop;
    SetProcessInput(true);
    FocusMode = FocusModeEnum.All;

    Pressed += OnPressed;

    BbcodeEnabled = true;
    FitContent = true;
    ScrollActive = false;

    AddThemeConstantOverride("line_separation", LINE_SEPARATION);
    AddThemeFontSizeOverride("normal_font_size", 55);

    var emptyStyleBox = new StyleBoxEmpty();
    AddThemeStyleboxOverride("focus", emptyStyleBox);
  }

  public Task Interact() {
    Pressed.Invoke();
    return Task.CompletedTask;
  }

  public void OnPressed() {
    DialogueManager.Instance.OnDialogueBoxUIPressed();
  }

  public void SetText(string text) {
    Text = $"[left]{text}[/left]";
  }
}
