using Godot;
using System;

public partial class VisualManager : Control {
    public static VisualManager Instance { get; private set; }

    private TextureRect fullScreenImage;
    // private VideoStreamPlayer videoPlayer;

    public override void _EnterTree() {
        if (Instance == null) {
            Instance = this;
        } else {
            QueueFree();
        }
    }

    public override void _Ready() {
        AnchorRight = 1;
        AnchorBottom = 1;
       // GrowHorizontal = GrowDirection.Both;
      //  GrowVertical = GrowDirection.Both;

        MouseFilter = MouseFilterEnum.Ignore;

        fullScreenImage = GetNode<TextureRect>("TextureRect");
        SetupBackgroundImage();
        // videoPlayer = GetNode<VideoStreamPlayer>("VideoPlayer");
    }

    private void SetupBackgroundImage() {
        // Ensure the TextureRect fills the entire VisualManager
        fullScreenImage.AnchorRight = 1;
        fullScreenImage.AnchorBottom = 1;
        fullScreenImage.AnchorsPreset = (int)Control.LayoutPreset.FullRect;

        // Set the expansion and stretch modes
      //  fullScreenImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
      //  fullScreenImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
    }



    public void DisplayVisual(string visualPath, VisualType type) {
        HideAllVisuals();

        switch (type) {
            case VisualType.Image:
                DisplayImage(visualPath);
                break;
                // case VisualType.Cutscene:
                //     PlayCutscene(visualPath);
                //     break;
        }
    }

    private void DisplayImage(string imagePath) {
        // var texture = ResourceLoader.Load<Texture2D>(imagePath);
        // fullScreenImage.Texture = texture;
        // fullScreenImage.Show();

        // fullScreenImage.AnchorRight = 1;
        // fullScreenImage.AnchorBottom = 1;
        // fullScreenImage.GrowHorizontal = GrowDirection.Both;
        // fullScreenImage.GrowVertical = GrowDirection.Both;

    }

    // private void PlayCutscene(string videoPath)
    // {
    //     var video = ResourceLoader.Load<VideoStream>(videoPath);
    //     videoPlayer.Stream = video;
    //     videoPlayer.Show();
    //     videoPlayer.Play();
    // }

    private void HideAllVisuals() {
        fullScreenImage.Hide();
        // videoPlayer.Hide();
        // videoPlayer.Stop();
    }

    public enum VisualType {
        Image,
        Cutscene
    }
}