using Godot;

public partial class PlayerChoiceButton : Button
{
    public override void _Ready()
    {
        Pressed += OnButtonPressed;
    }

    private void OnButtonPressed()
    {
        GD.Print("Button was pressed!");
    }

    // Add any other methods or properties you need
}