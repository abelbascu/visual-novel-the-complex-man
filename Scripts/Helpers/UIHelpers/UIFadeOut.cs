using Godot;
using System;

namespace UIHelpers
{
    public partial class UIFadeOut : Node
    {
        private ColorRect fadeRect;
        public float FadeDuration { get; set; } = 0.5f;

        public UIFadeOut(Node parent)
        {
            SetupFadeEffect(parent);
        }

        private void SetupFadeEffect(Node parent)
        {
            fadeRect = new ColorRect();
            fadeRect.Color = new Color(0, 0, 0, 0); // Start fully transparent
            fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            parent.AddChild(fadeRect);
            parent.MoveChild(fadeRect, -1); //move to top so the effect is seen
        }

        public void FadeOut(Action onFadeOutFinished = null)
        {
            GD.Print("Starting fade out (transparent to black)");
            fadeRect.Color = new Color(0, 0, 0, 0);
            fadeRect.Visible = true;
            var tween = fadeRect.CreateTween();
            tween.TweenProperty(fadeRect, "color:a", 1.0, FadeDuration);
            tween.Finished += () =>
            {
                OnFadeOutFinished();
                onFadeOutFinished?.Invoke();
                CleanUp();
            };
        }

        private void CleanUp() {
            fadeRect.QueueFree();
            QueueFree();
        }

        private void OnFadeOutFinished()
        {
            GD.Print("Fade out complete (now black)");
        }
    }
}