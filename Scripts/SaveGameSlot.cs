using Godot;
using System;
using System.IO;

public partial class SaveGameSlot : HBoxContainer {

  [Export]
  private Vector2 ThumbnailSize = new Vector2(120, 67); // 16:9 aspect ratio
  private TextureRect thumbnailTextureRect;

  public Action<int> SaveRequested;
  public Action<string> LoadRequested;

  private RichTextLabel titleLabel;
  private RichTextLabel dateLabel;
  private RichTextLabel timePlayedLabel;
  private RichTextLabel gameCompletedPercentageLabel;

  public InteractableUIButton actionButton;
  private MarginContainer marginContainer;
  private MarginContainer buttonContainer;
  private int slotNumber;
  private string saveFilePath;
  private bool isMouseOver = false;
  private const int ACTION_BUTTON_FONT_SIZE = 30;
  private string autosavedPrefixLabelTRANSLATE = "AUTOSAVE_PREFIX_TITLE"; // (Autosaved)
  private string timePlayedLabelTRANSLATE = "TIME_PLAYED"; //Time Played
  private string dialoguesVisitedLabelTRANSLATE = "DIALOGUES_VISITED"; //Dialogues visited
  private string loadGameTRANSLATE = "LOAD_GAME";
  private string saveGameTRANSLATE = "SAVE_GAME";

  private StyleBoxFlat disabledStyle = new StyleBoxFlat {
    BgColor = Colors.DarkRed,
    CornerRadiusTopLeft = 10,
    CornerRadiusTopRight = 10,
    CornerRadiusBottomLeft = 10,
    CornerRadiusBottomRight = 10,
    BorderColor = Colors.White,
    BorderWidthBottom = 2,
    BorderWidthTop = 2,
    BorderWidthLeft = 2,
    BorderWidthRight = 2
  };

  private StyleBoxFlat normalStyle = new StyleBoxFlat {
    BgColor = Colors.Black,
    CornerRadiusTopLeft = 10,
    CornerRadiusTopRight = 10,
    CornerRadiusBottomLeft = 10,
    CornerRadiusBottomRight = 10,
    BorderColor = Colors.White,
    BorderWidthBottom = 2,
    BorderWidthTop = 2,
    BorderWidthLeft = 2,
    BorderWidthRight = 2
  };

  // Hover state
  private StyleBoxFlat hoverStyle = new StyleBoxFlat {
    BgColor = Colors.DarkRed,
    CornerRadiusTopLeft = 10,
    CornerRadiusTopRight = 10,
    CornerRadiusBottomLeft = 10,
    CornerRadiusBottomRight = 10,
    BorderColor = Colors.White,
    BorderWidthBottom = 2,
    BorderWidthTop = 2,
    BorderWidthLeft = 2,
    BorderWidthRight = 2
  };

  // Pressed state
  private StyleBoxFlat pressedStyle = new StyleBoxFlat {
    BgColor = Colors.DarkRed,
    CornerRadiusTopLeft = 10,
    CornerRadiusTopRight = 10,
    CornerRadiusBottomLeft = 10,
    CornerRadiusBottomRight = 10,
    BorderColor = Colors.White,
    BorderWidthBottom = 2,
    BorderWidthTop = 2,
    BorderWidthLeft = 2,
    BorderWidthRight = 2
  };

  private static Color customBlue = new Color(
    0f / 255f,  // Red component
    71f / 255f,  // Green component
    171f / 255f   // Blue component
);

  public override void _Ready() {

    MouseFilter = MouseFilterEnum.Stop;

    titleLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer/GameSaveTitle");
    dateLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer/GameSaveDate");
    timePlayedLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer2/TimePlayed");
    gameCompletedPercentageLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer2/GameCompletedPercent");
    thumbnailTextureRect = GetNode<TextureRect>("MarginContainer2/HBoxContainer/TextureRect");
    buttonContainer = GetNode<MarginContainer>("MarginContainer2");
    actionButton = GetNode<InteractableUIButton>("MarginContainer2/HBoxContainer/Button");
    marginContainer = GetNode<MarginContainer>("MarginContainer");

    actionButton.Pressed += OnActionButtonPressed;
    actionButton.AddThemeStyleboxOverride("normal", normalStyle);
    actionButton.AddThemeStyleboxOverride("hover", hoverStyle);
    actionButton.AddThemeStyleboxOverride("pressed", pressedStyle);
    actionButton.AddThemeStyleboxOverride("disabled", disabledStyle);
    actionButton.AddThemeStyleboxOverride("disabled_pressed", disabledStyle);
    actionButton.AddThemeStyleboxOverride("disabled_hover", disabledStyle);
    actionButton.AddThemeFontSizeOverride("font_size", ACTION_BUTTON_FONT_SIZE);
  }

  public void DisableButton() {
    actionButton.MouseFilter = MouseFilterEnum.Stop;
    actionButton.SetProcessMode(Node.ProcessModeEnum.Disabled);
    buttonContainer.SetProcessMode(Node.ProcessModeEnum.Disabled);
    actionButton.FocusMode = Control.FocusModeEnum.None;
    actionButton.AddThemeColorOverride("font_disabled_color", Colors.Gray);
    // actionButton.Disabled = true;
    Hide();
  }

  public void EnableButton() {
    actionButton.MouseFilter = MouseFilterEnum.Stop;
    actionButton.SetProcessMode(Node.ProcessModeEnum.Inherit);
    actionButton.Disabled = false;
    actionButton.FocusMode = Control.FocusModeEnum.All;
    buttonContainer.SetProcessMode(Node.ProcessModeEnum.Inherit);
    Show();
  }

  public void SetLoadSlotData(LoadSaveManager.GameState gameState, int number, bool isLoadScreen) {
    slotNumber = number;
    string prefix = gameState.IsAutosave ? "autosave_" : "save_";
    saveFilePath = Path.Combine(OS.GetUserDataDir(), "saves", $"{prefix}{slotNumber:D3}.sav");

    // Normalize the path to ensure consistent slash direction
    saveFilePath = Path.GetFullPath(saveFilePath);

    string autosaveLabel = gameState.IsAutosave ? $"{Tr(autosavedPrefixLabelTRANSLATE)}" : "";
    titleLabel.Text = $"Game Save {slotNumber}{autosaveLabel}";
    dateLabel.Text = gameState.SaveTime.ToString("MMM d, yyyy - h:mm:ss tt");
    timePlayedLabel.Text = $"{Tr(timePlayedLabelTRANSLATE)}: {FormatTimeSpan(gameState.TimePlayed)}";
    gameCompletedPercentageLabel.Text = $"{Tr(dialoguesVisitedLabelTRANSLATE)}: {gameState.DialoguesVisitedForAllGamesPercentage:F1}%";

    if (gameState.VisualPath != null) {
      Texture2D texture = GD.Load<Texture2D>(gameState.VisualPath);
      var image = texture.GetImage();

      var intermediateSize = new Vector2(image.GetWidth() / 4, image.GetHeight() / 4);
      image.Resize((int)intermediateSize.X, (int)intermediateSize.Y, Image.Interpolation.Cubic);
      ImageTexture resizedTexture = ImageTexture.CreateFromImage(image);
      thumbnailTextureRect.Texture = resizedTexture;
    } else {
      thumbnailTextureRect.Texture = null;
    }

    actionButton.Text = isLoadScreen ? $"{loadGameTRANSLATE}" : $"{saveGameTRANSLATE}";
    actionButton.Visible = isLoadScreen || gameState == null;
  }

  public void SetSaveEmptySlot(int number) {
    slotNumber = number;
    titleLabel.Text = $"Empty Slot {slotNumber}";
    dateLabel.Text = "";
    timePlayedLabel.Text = "";
    gameCompletedPercentageLabel.Text = "";
    thumbnailTextureRect.Texture = null;
    actionButton.Text = saveGameTRANSLATE;
  }

  private void OnActionButtonPressed() {
    if (actionButton.Text == saveGameTRANSLATE) {
      SaveRequested.Invoke(slotNumber);
    } else {

      LoadRequested.Invoke(saveFilePath);
    }

  }

  private string FormatTimeSpan(TimeSpan timeSpan) {
    if (timeSpan.Days > 0) {
      return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
    } else if (timeSpan.Hours > 0) {
      return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
    } else {
      return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
    }
  }
}
