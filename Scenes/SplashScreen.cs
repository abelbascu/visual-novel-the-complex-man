using Godot;
using System;

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

    private void OnBackgroundGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
           
            TransitionToMainMenu();
        }
    }

    private void TransitionToMainMenu()
    {
        pressAnyKeyLabel.Hide();
        ProcessMode = ProcessModeEnum.Disabled;
        GameStateManager.Instance.DISPLAY_MAIN_MENU();
    }
}
