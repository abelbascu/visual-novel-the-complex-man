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

    private Button actionButton;
    private MarginContainer marginContainer;
    private int slotNumber;
    private string saveFilePath;
    private bool isMouseOver = false;
    private const int ACTION_BUTTON_FONT_SIZE = 30;

    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Stop;

        titleLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer/GameSaveTitle");
        dateLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer/GameSaveDate");
        timePlayedLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer2/TimePlayed");
        gameCompletedPercentageLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer2/GameCompletedPercent");
        thumbnailTextureRect = GetNode<TextureRect>("MarginContainer2/HBoxContainer/TextureRect");
        actionButton = GetNode<Button>("MarginContainer2/HBoxContainer/Button");
        marginContainer = GetNode<MarginContainer>("MarginContainer");

        actionButton.Pressed += OnActionButtonPressed;

        var normalStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
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
        actionButton.AddThemeStyleboxOverride("normal", normalStyle);

        Color customBlue = new Color(
            0f / 255f,  // Red component
            71f / 255f,  // Green component
            171f / 255f   // Blue component
        );

        // Hover state
        var hoverStyle = new StyleBoxFlat {
            BgColor = customBlue,
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

        actionButton.AddThemeStyleboxOverride("hover", hoverStyle);


        // Pressed state
        var pressedStyle = new StyleBoxFlat {
            BgColor = Colors.DarkBlue,
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
        actionButton.AddThemeStyleboxOverride("pressed", pressedStyle);
        actionButton.AddThemeFontSizeOverride("font_size", ACTION_BUTTON_FONT_SIZE);

    }

    public void SetLoadSlotData(GameStateManager.GameState gameState, int number, bool isLoadScreen) {
        slotNumber = number;
        string prefix = gameState.IsAutosave ? "autosave_" : "save_";
        saveFilePath = Path.Combine(OS.GetUserDataDir(), "saves", $"save_{slotNumber:D3}.sav");

        // Normalize the path to ensure consistent slash direction
        saveFilePath = Path.GetFullPath(saveFilePath);

        string autosaveLabel = gameState.IsAutosave ? " (Autosaved)" : "";
        titleLabel.Text = $"Game Save {slotNumber}{autosaveLabel}";
        dateLabel.Text = gameState.SaveTime.ToString("MMM d, yyyy - h:mm:ss tt");
        timePlayedLabel.Text = $"Time Played: {FormatTimeSpan(gameState.TimePlayed)}";
        gameCompletedPercentageLabel.Text = $"Dialogues: {gameState.DialoguesVisitedPercentage:F1}%";

        if (gameState.VisualPath != null) {
            Image image = Image.LoadFromFile(gameState.VisualPath);
            //without Lanczos interpolation, the thumbnail is very pixelated
            image.Resize((int)ThumbnailSize.X, (int)ThumbnailSize.Y, Image.Interpolation.Lanczos);
            ImageTexture texture = ImageTexture.CreateFromImage(image);
            thumbnailTextureRect.Texture = texture;
        } else {
            thumbnailTextureRect.Texture = null;
        }

        actionButton.Text = isLoadScreen ? "Load Game" : "Save Game";
        actionButton.Visible = isLoadScreen || gameState == null;
    }

    public void SetSaveEmptySlot(int number) {
        slotNumber = number;
        titleLabel.Text = $"Empty Slot {slotNumber}";
        dateLabel.Text = "";
        timePlayedLabel.Text = "";
        gameCompletedPercentageLabel.Text = "";
        thumbnailTextureRect.Texture = null;
        actionButton.Text = "Save Game";
    }

    private void OnActionButtonPressed() {
        if (actionButton.Text == "Save Game") {
            SaveRequested.Invoke(slotNumber);
        } else {
            LoadRequested.Invoke(saveFilePath);
            //UIManager.Instance.saveGameScreen.Hide();
            //UIManager.Instance.menuOverlay.Visible = false;
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
