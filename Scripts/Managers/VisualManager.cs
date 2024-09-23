using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

public partial class VisualManager : Control {
  public static VisualManager Instance { get; private set; }

  public TextureRect fullScreenImage;
  public string VisualPath;
  public float visualPreDelay = 0;
  public float visualPostDelay = 0;
  // private VideoStreamPlayer videoPlayer;

  public override void _EnterTree() {
    if (Instance == null) {
      Instance = this;
    } else {
      QueueFree();
    }
  }

  public override void _Ready() {

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



  public async Task DisplayVisual(string visualPath, float visualPreDelay, float visualPostDelay) {
    HideAllVisuals();
    this.VisualPath = visualPath;

    string extension = Path.GetExtension(visualPath).ToLower();
    switch (extension) {
      case ".png":
      case ".jpg":
      case ".jpeg":
        await Task.Delay((int)(visualPreDelay * 1000));
        DisplayImage(visualPath);
        await Task.Delay((int)(visualPostDelay * 1000));
        break;
      case ".tscn":
        //PlayCutscene(visualPath);
        break;
      default:
        GD.PrintErr($"Unsupported file type: {extension}");
        break;

    }
  }

  public void DisplayImage(string imagePath) {
    var texture = ResourceLoader.Load<Texture2D>(imagePath);
    fullScreenImage.Texture = texture;
    fullScreenImage.Show();
  }


  public void RemoveImage() {
    fullScreenImage.Texture = null;
    fullScreenImage.Hide();
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
}