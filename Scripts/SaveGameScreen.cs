using Godot;
using System;
using System.Collections.Generic;

public partial class SaveGameScreen : Control {
    private PackedScene saveGameSlotScene;
    private ScrollContainer scrollContainer;
    private VBoxContainer slotsContainer;
    private MarginContainer marginContainer;

    public override void _Ready() {
        saveGameSlotScene = GD.Load<PackedScene>("res://Scenes/SaveGameSlot.tscn");
        scrollContainer = GetNode<ScrollContainer>("MarginContainer/ScrollContainer");
        slotsContainer = GetNode<VBoxContainer>("MarginContainer/ScrollContainer/VBoxContainer");
        marginContainer = GetNode<MarginContainer>("MarginContainer");

        //PopulateSaveSlots();

        // Set size flags to prevent expansion
        marginContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        marginContainer.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
    }


    public void ShowScreen(bool isLoadScreen)
{
    // Clear existing slots
    foreach (Node child in slotsContainer.GetChildren())
    {
        child.QueueFree();
    }

    // Populate with appropriate slots
    PopulateSaveSlots(isLoadScreen);

    // Make the screen visible
    Visible = true;
}

    private void PopulateSaveSlots(bool isLoadScreen) {
        List<GameStateManager.GameState> saveGames = GameStateManager.Instance.GetSavedGames();

        //it's the Save screen, not the Load screen
        if(!isLoadScreen)
        AddSaveSlot(null, saveGames.Count + 1, isLoadScreen);

        for (int i = 0; i < saveGames.Count; i++) {
            AddSaveSlot(saveGames[i], saveGames[i].SlotNumber, isLoadScreen);
        }
    }

    private void AddSaveSlot(GameStateManager.GameState gameState, int slotNumber, bool isLoadScreen) {
        var slotInstance = saveGameSlotScene.Instantiate<SaveGameSlot>();
        slotsContainer.AddChild(slotInstance);

        if (gameState != null) {
            slotInstance.SetLoadSlotData(gameState, slotNumber, isLoadScreen);
        } 
        else if (!isLoadScreen)
        {
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

        //QueueFree();
        Hide();
    }

    private void RefreshSaveSlots() {
        foreach (Node child in slotsContainer.GetChildren()) {
            child.QueueFree();
        }
        PopulateSaveSlots(false);
    }



}
