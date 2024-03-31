using Godot;
using System;

public partial class MainMenu : Control
{

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Button startNewGameButton = GetNode<Button>("VBoxContainer/StartNewGameButton");
		startNewGameButton.Pressed += ButtonPressed;
		TranslationServer.SetLocale("fr");
		string language = TranslationServer.GetLocale();
		//string language = OS.GetLocaleLanguage();
		GD.Print($"langauage: {language}");
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	private void ButtonPressed()
	{
	   GetTree().ChangeSceneToFile("res://Scenes/GameStartScene.tscn");
	}
}
