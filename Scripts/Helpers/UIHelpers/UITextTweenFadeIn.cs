using Godot;
using System.Threading.Tasks;

public partial class UITextTweenFadeIn : Node
{
    public async Task FadeIn(Control control, float duration = 2.5f)
    {
        control.Modulate = new Color(control.Modulate, 0);
        control.Visible = true;

        var tween = control.CreateTween();
        tween.TweenProperty(control, "modulate:a", 1.0f, duration);

        await ToSignal(tween, "finished");
    }
}
