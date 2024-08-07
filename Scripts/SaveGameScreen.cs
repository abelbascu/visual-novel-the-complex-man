using Godot;
using System;
using System.Collections.Generic;

public partial class SaveGameScreen : MarginContainer {
    private PackedScene saveGameSlotScene;
    private ScrollContainer scrollContainer;
    private VBoxContainer slotsContainer;
    private MarginContainer marginContainer;
    private Button goBackButton;
    private RichTextLabel noSavesLabel;


    public override void _Ready() {
        saveGameSlotScene = GD.Load<PackedScene>("res://Scenes/SaveGameSlot.tscn");
        scrollContainer = GetNode<ScrollContainer>("MarginContainer/ScrollContainer");
        slotsContainer = GetNode<VBoxContainer>("MarginContainer/ScrollContainer/VBoxContainer");
        marginContainer = GetNode<MarginContainer>("MarginContainer");

        // Set size flags to prevent expansion
        marginContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        marginContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        goBackButton = GetNode<Button>("MarginContainer2/GoBackButton");
        goBackButton.Pressed += () => OnGoBackButtonPressed();

        goBackButton.AnchorTop = 0;
        goBackButton.AnchorRight = 1;
        goBackButton.AnchorBottom = 0;
        goBackButton.AnchorLeft = 1;
        goBackButton.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        //goBackButton.Position = new Vector2(-10, 10);  

         var normalStyle = new StyleBoxFlat {
            BgColor = Colors.Blue,
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
        goBackButton.AddThemeStyleboxOverride("normal", normalStyle);

                var hoverStyle = new StyleBoxFlat {
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

        goBackButton.AddThemeStyleboxOverride("hover", hoverStyle);
    }

    private void CreateNoSavesAvailableLabel() {
        noSavesLabel = new RichTextLabel {
            BbcodeEnabled = true,
            Text = "[center]No saved games yet. Please save a game first.[/center]",
            FitContent = false,
            Name = "NoSavesLabel",
            Visible = false
        };

        noSavesLabel.AddThemeFontSizeOverride("normal_font_size", 40);
        noSavesLabel.AddThemeColorOverride("default_color", Colors.White);
        noSavesLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        noSavesLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        noSavesLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        noSavesLabel.CustomMinimumSize = new Vector2(400, 500);
        slotsContainer.AddChild(noSavesLabel);
    }

    private void OnGoBackButtonPressed() {
        Hide();
        //UIManager.Instance.
    }

    public void ShowScreen(bool isLoadScreen) {
        // Clear existing slots
        foreach (Node child in slotsContainer.GetChildren()) {
            child.QueueFree();
        }
        CreateNoSavesAvailableLabel();

        // Populate with appropriate slots
        PopulateSaveOrLoadSlots(isLoadScreen);

        Show();
    }

    private void PopulateSaveOrLoadSlots(bool isLoadScreen) {
        List<GameStateManager.GameState> saveGames = GameStateManager.Instance.GetSavedGames();

        foreach (Node child in slotsContainer.GetChildren()) {
            if (child != noSavesLabel) {
                child.QueueFree();
            }
        }

        if (saveGames.Count == 0 && isLoadScreen) {
            noSavesLabel.Visible = true;

        } else {

            noSavesLabel.Visible = false;
            //it's the Save screen, not the Load screen
            if (!isLoadScreen)
                AddSaveOrLoadSlot(null, saveGames.Count + 1, isLoadScreen);

            for (int i = 0; i < saveGames.Count; i++) {
                AddSaveOrLoadSlot(saveGames[i], saveGames[i].SlotNumber, isLoadScreen);
            }
        }
    }

    private void AddSaveOrLoadSlot(GameStateManager.GameState gameState, int slotNumber, bool isLoadScreen) {
        var slotInstance = saveGameSlotScene.Instantiate<SaveGameSlot>();
        slotsContainer.AddChild(slotInstance);

        if (gameState != null) {
            slotInstance.SetLoadSlotData(gameState, slotNumber, isLoadScreen);
        } else if (!isLoadScreen) {
            slotInstance.SetSaveEmptySlot(slotNumber);
        }

        slotInstance.SaveRequested += OnSaveRequested;
        slotInstance.LoadRequested += OnLoadRequested;
    }

    private void OnSaveRequested(int slotNumber) {
        GameStateManager.Instance.SaveGame();
        RefreshSaveSlots();
    }

    private void OnLoadRequested(string saveFilePath) {
        UIManager.Instance.menuOverlay.Visible = false;
        GameStateManager.Instance.LoadGame(saveFilePath);
        if (UIManager.Instance.mainMenu.IsVisibleInTree()) {
            UIManager.Instance.mainMenu.CloseMainMenu();
            GameStateManager.Instance.ToggleAutosave(true);
        }
        //QueueFree();
        Hide();
    }

    private void RefreshSaveSlots() {
        // foreach (Node child in slotsContainer.GetChildren()) {
        //     child.QueueFree();
        // }
        PopulateSaveOrLoadSlots(false);
    }



}
