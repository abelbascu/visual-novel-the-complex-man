using Godot;
using System;
using System.Collections.Generic;

public partial class SaveGameScreen : Control
{
	private PackedScene saveGameSlotScene;
	private ScrollContainer scrollContainer;
	private VBoxContainer slotsContainer;

	public override void _Ready()
	{
		saveGameSlotScene = GD.Load<PackedScene>("res://Scenes/SaveGameSlot.tscn");
		scrollContainer = GetNode<ScrollContainer>("MarginContainer/ScrollContainer");
		slotsContainer = GetNode<VBoxContainer>("MarginContainer/ScrollContainer/VBoxContainer");

		PopulateSaveSlots();
	}

	private void PopulateSaveSlots() {
		List<GameStateManager.GameState> saveGames = GameStateManager.Instance.GetSavedGames();
		AddSaveSlot(null, saveGames.Count + 1);

		for(int i = 0; i < saveGames.Count; i++)
		{
			AddSaveSlot(saveGames[i], i + 1);
		}
	}

	private void AddSaveSlot(GameStateManager.GameState gameState, int slotNumber)
	{
		var slotInstance = saveGameSlotScene.Instantiate<SaveGameSlot>();
		slotsContainer.AddChild(slotInstance);

		if(gameState != null)
		{
			slotInstance.SetLoadSlotData(gameState, slotNumber);
		}
		else
		{
			slotInstance.SetSaveEmptySlot(slotNumber);
		}

		slotInstance.SaveRequested += OnSaveRequested;
		slotInstance.LoadRequested += OnLoadRequested;
	}

	private void OnSaveRequested(int slotNumber)
	{
		GameStateManager.Instance.SaveGame();
		RefreshSaveSlots();
	}

	private void OnLoadRequested(string saveFilePath)
	{
		UIManager.Instance.menuOverlay.Visible = false;
		GameStateManager.Instance.LoadGame(saveFilePath);
		//QueueFree();
		Hide();	
	}

	private void RefreshSaveSlots()
	{
		foreach(Node child in slotsContainer.GetChildren())
		{
			child.QueueFree();
		}
		PopulateSaveSlots();
	}



}
