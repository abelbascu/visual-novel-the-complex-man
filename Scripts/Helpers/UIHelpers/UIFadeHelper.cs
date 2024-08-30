using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;  // This is needed for the Select method

public static class UIFadeHelper {
    public static async Task FadeInControl(Control control, float duration = 0.6f) {
        GD.Print($"FadeInControl started for {control.Name}");
        await FadeControl(control, 0f, 1f, duration);
        GD.Print($"FadeInControl completed for {control.Name}");
    }

    public static async Task FadeOutControl(Control control, float duration = 0.6f) {
        await FadeControl(control, 1f, 0f, duration);
    }

    private static async Task FadeControl(Control control, float fromAlpha, float toAlpha, float duration) {
        GD.Print($"FadeControl started for {control.Name}: from {fromAlpha} to {toAlpha}");
        control.Modulate = new Color(control.Modulate.R, control.Modulate.G, control.Modulate.B, fromAlpha);
        control.Visible = true;

        Tween tween = control.GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);



        tween.TweenProperty(control, "modulate:a", toAlpha, duration);

        await control.ToSignal(tween, Tween.SignalName.Finished);
        GD.Print($"FadeControl completed for {control.Name}");

        if (toAlpha == 0f) {
            control.Visible = false;
        }
    }

    // New method to fade multiple controls simultaneously
    public static async Task FadeInAllControlsInSync(IEnumerable<Control> controls, float duration = 0.6f) {
        var tasks = controls.Select(control => FadeInControl(control, duration));
        await Task.WhenAll(tasks);
    }

    // New method to fade multiple controls simultaneously
    public static async Task FadeOutAllControsInSync(IEnumerable<Control> controls, float duration = 0.6f) {
        var tasks = controls.Select(control => FadeOutControl(control, duration));
        await Task.WhenAll(tasks);
    }

    public static async Task FadeInWindow(Window window, float duration = 0.6f) {
        var overlay = new ColorRect {
            Color = new Color(0, 0, 0, 1),
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        window.AddChild(overlay);
        window.Visible = true;

        var tween = window.CreateTween();
        tween.TweenProperty(overlay, "color:a", 0.0f, duration);

        await window.ToSignal(tween, Tween.SignalName.Finished);
        overlay.QueueFree();
    }

    public static async Task FadeOutWindow(Window window, float duration = 0.6f) {
        var overlay = new ColorRect {
            Color = new Color(0, 0, 0, 0),
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        window.AddChild(overlay);

        var tween = window.CreateTween();
        tween.TweenProperty(overlay, "color:a", 0.6f, duration);

        await window.ToSignal(tween, Tween.SignalName.Finished);
        window.Visible = false;
        overlay.QueueFree();
    }
}