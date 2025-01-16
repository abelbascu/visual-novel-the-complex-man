using Godot;
using System;

public partial class DynamicCursorScaler : Node {
  private const string CursorTexturePath = "res://Visuals/UI/cursor pixel cutre.png";
  private const float MinScale = 0.2f;
  private const float MaxScale = 1.0f;
  private readonly Vector2 OriginalSize = new Vector2(128, 128);

  private Texture2D _originalCursorTexture;
  private Image _originalCursorImage;

  public override void _Ready() {
    _originalCursorTexture = ResourceLoader.Load<Texture2D>(CursorTexturePath);
    if (_originalCursorTexture == null) {
      GD.PrintErr($"Failed to load cursor texture from {CursorTexturePath}");
      return;
    }

    _originalCursorImage = _originalCursorTexture.GetImage();
    GetTree().Root.Connect("size_changed", new Callable(this, nameof(UpdateCursorScale)));
    UpdateCursorScale();
  }

  public void UpdateCursorScale() {
    Vector2I windowSize = DisplayServer.WindowGetSize();
    float scaleFactor = Mathf.Min(windowSize.X / 1920f, windowSize.Y / 1080f);
    scaleFactor = Mathf.Clamp(scaleFactor, MinScale, MaxScale);

    Vector2I newSize = new Vector2I(
        (int)(OriginalSize.X * scaleFactor),
        (int)(OriginalSize.Y * scaleFactor)
    );

    Image scaledImage = _originalCursorImage.Duplicate() as Image;
    if (scaledImage != null) {
      scaledImage.Resize(newSize.X, newSize.Y, Image.Interpolation.Bilinear);
      ImageTexture scaledTexture = ImageTexture.CreateFromImage(scaledImage);
      Input.SetCustomMouseCursor(scaledTexture);
    } else {
      GD.PrintErr("Failed to duplicate cursor image.");
    }
  }
}