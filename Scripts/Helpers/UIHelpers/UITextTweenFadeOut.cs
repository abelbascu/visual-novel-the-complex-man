using Godot;
using System.Threading.Tasks;

public partial class UITextTweenFadeOut : Node
{
    public async Task FadeOut(Control control, float duration = 2.5f)
    {
        var tween = control.CreateTween();
        tween.TweenProperty(control, "modulate:a", 0.0f, duration);

        await ToSignal(tween, "finished");

        control.Visible = false;
    }
}