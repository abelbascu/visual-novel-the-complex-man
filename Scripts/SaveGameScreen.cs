using Godot;
using System;
using System.Collections.Generic;
using static GameStateMachine;

public partial class SaveGameScreen : MarginContainer {
    private PackedScene saveGameSlotScene;
    private ScrollContainer scrollContainer;
    public VBoxContainer slotsContainer;
    private MarginContainer marginContainer;
    private Button goBackButton;
    private RichTextLabel noSavesLabel;
    private string noSavesTRANSLATE = "NO_SAVES_AVAILABLE";
    private const bool AUTODSAVE_DISABLED = false;
    private const bool AUTOSAVE_ENABLED = true;


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

        string translatedText = $"[center]{TranslationServer.Translate(noSavesTRANSLATE)}[/center]";
  
        noSavesLabel = new RichTextLabel {
            BbcodeEnabled = true,
            Text = translatedText,
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
        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
        //UIManager.Instance.
    }

    public void SetUpSaveOrLoadScreen(bool isLoadScreen) {
        // Clear existing slots
        foreach (Node child in slotsContainer.GetChildren()) {
            child.QueueFree();
        }
        CreateNoSavesAvailableLabel();

        // Populate with appropriate slots
        PopulateSaveOrLoadSlots(isLoadScreen);
        
    }

    public void DisplayLoadScreen()
    {
       Show();
    }

    public void DisplaySaveScreen()
    {
        Show();
    }


    private void PopulateSaveOrLoadSlots(bool isLoadScreen) {
        List<LoadSaveManager.GameState> saveGames = LoadSaveManager.Instance.GetSavedGames();

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

    private void AddSaveOrLoadSlot(LoadSaveManager.GameState gameState, int slotNumber, bool isLoadScreen) {
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

        GameStateManager.Instance.Fire(Trigger.SAVE_GAME, AUTODSAVE_DISABLED);
    }

    private void OnLoadRequested(string saveFilePath) {

        GameStateManager.Instance.Fire(Trigger.LOAD_GAME, saveFilePath);
        //QueueFree();
        Hide();
    }

    public void RefreshSaveSlots() {
        PopulateSaveOrLoadSlots(false);
    }



}
