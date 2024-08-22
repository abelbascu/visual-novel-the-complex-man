using Godot;
using System;

namespace UIHelpers
{
    public partial class UIFadeIn : Node
    {
        private ColorRect fadeRect;
        public float FadeDuration { get; set; } = 0.5f;

        public UIFadeIn(Node parent)
        {
            SetupFadeEffect(parent);
        }

        private void SetupFadeEffect(Node parent)
        {
            fadeRect = new ColorRect();
            fadeRect.Color = Colors.Black;
            fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            parent.AddChild(fadeRect);
            parent.MoveChild(fadeRect, -1);  //move to top so the effect is seen
        }

        public void FadeIn(Action onFadeInFinished = null)
        {
            GD.Print("Starting fade in (black to transparent)");
            fadeRect.Visible = true;
            fadeRect.Color = Colors.Black;
            var tween = fadeRect.CreateTween();
            tween.TweenProperty(fadeRect, "color:a", 0.0, FadeDuration);
            tween.Finished += () =>
            {
                OnFadeInFinished();
                onFadeInFinished?.Invoke(); //notice this is invoking an external action from the caller script
                CleanUp();
            };
        }

        private void OnFadeInFinished()
        {
            GD.Print("Fade in complete (now transparent)");
         
        }

        private void CleanUp() {
            fadeRect.QueueFree();
            QueueFree();
        }
    }
}