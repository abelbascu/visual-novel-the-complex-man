using Godot;

public partial class BorderedMarginContainer : MarginContainer
{
    [Export]
    public Color BorderColor { get; set; } = Colors.Red;

    [Export]
    public int BorderWidth { get; set; } = 2;

    public override void _Draw()
    {
       var rect = GetRect();
    
    // Draw only the border lines
    DrawLine(new Vector2(0, 0), new Vector2(rect.Size.X, 0), BorderColor, BorderWidth); // Top
    DrawLine(new Vector2(0, rect.Size.Y), new Vector2(rect.Size.X, rect.Size.Y), BorderColor, BorderWidth); // Bottom
    DrawLine(new Vector2(0, 0), new Vector2(0, rect.Size.Y), BorderColor, BorderWidth); // Left
    DrawLine(new Vector2(rect.Size.X, 0), new Vector2(rect.Size.X, rect.Size.Y), BorderColor, BorderWidth); // Right
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }
}
