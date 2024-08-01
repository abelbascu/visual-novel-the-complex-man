using Godot;
using System;

public partial class SaveGameSlot : HBoxContainer {
    public Action<int> SaveRequested;
    public Action<string> LoadRequested;

    private RichTextLabel titleLabel;
    private RichTextLabel dateLabel;
    private RichTextLabel timePlayedLabel;
    private RichTextLabel gameCompletedPercentageLabel;
    private TextureRect screenshotTexture;
    private Button actionButton;

    private int slotNumber;
    private string saveFilePath;

    public override void _Ready() {
        titleLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer/GameSaveTitle");
        dateLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer/GameSaveDate");
        timePlayedLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/MarginContainer2/TimePlayed");
        gameCompletedPercentageLabel = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer2/MarginContainer2/GameCompletedPercent");
        screenshotTexture = GetNode<TextureRect>("MarginContainer2/HBoxContainer/TextureRect");
        actionButton = GetNode<Button>("MarginContainer2/HBoxContainer/Button");

        actionButton.Pressed += OnActionButtonPressed;
    }

    public void SetLoadSlotData(GameStateManager.GameState gameState, int number) {
        slotNumber = number;
        saveFilePath = $"user://saves/save_{slotNumber:D}.save";

        titleLabel.Text = $"Game Save {slotNumber}";
        dateLabel.Text = gameState.SaveTime.ToString("MMM d, yyyy - h:mm:ss tt");
        timePlayedLabel.Text = $"Time Played: {FormatTimeSpan(gameState.TimePlayed)}";
        gameCompletedPercentageLabel.Text = $"Dialogues: {gameState.DialoguesVisitedPercentage:F1}%";

        screenshotTexture.Texture = ImageTexture.CreateFromImage(gameState.Screenshot);

        actionButton.Text = "Load Game";
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
