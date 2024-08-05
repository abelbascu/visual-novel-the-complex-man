using Godot;
using System;
using System.IO;

public partial class SaveGameSlot : HBoxContainer {
    public Action<int> SaveRequested;
    public Action<string> LoadRequested;

    private RichTextLabel titleLabel;
    private RichTextLabel dateLabel;
    private RichTextLabel timePlayedLabel;
    private RichTextLabel gameCompletedPercentageLabel;
    private TextureRect screenshotTexture;
    private Button actionButton;
    private MarginContainer marginContainer;
    private int slotNumber;
    private string saveFilePath;

    private bool isMouseOver = false;

    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Stop;

        titleLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer/GameSaveTitle");
        dateLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer/GameSaveDate");
        timePlayedLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer2/TimePlayed");
        gameCompletedPercentageLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer2/GameCompletedPercent");
        screenshotTexture = GetNode<TextureRect>("MarginContainer2/HBoxContainer/TextureRect");
        actionButton = GetNode<Button>("MarginContainer2/HBoxContainer/Button");
        marginContainer = GetNode<MarginContainer>("MarginContainer");

        actionButton.Pressed += OnActionButtonPressed;
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

        if (gameState.Screenshot != null) {
            var imageTexture = ImageTexture.CreateFromImage(gameState.Screenshot);
            screenshotTexture.Texture = imageTexture;
        } else {
            screenshotTexture.Texture = null;
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
        screenshotTexture.Texture = null;
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
