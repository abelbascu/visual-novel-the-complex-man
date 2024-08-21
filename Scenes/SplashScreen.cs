using Godot;
using System;
using static GameStateMachine;

public partial class SplashScreen : Control
{
    private TextureRect backgroundTexture;
    private RichTextLabel pressAnyKeyLabel;

    public override void _Ready()
    {
        backgroundTexture = GetNode<TextureRect>("TextureRect");
        pressAnyKeyLabel = GetNode<RichTextLabel>("MarginContainer/RichTextLabel");

        backgroundTexture.GuiInput += OnBackgroundGuiInput;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("action_key"))
        {
           
            TransitionToMainMenu();
        }
    }

    public void OnBackgroundGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
           
            TransitionToMainMenu();
        }
    }

    public void TransitionToMainMenu()
    {
        pressAnyKeyLabel.Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);
    }
}
