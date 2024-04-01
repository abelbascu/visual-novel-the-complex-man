using Godot;
using System;

public partial class MainMenu : Control {
    private string language;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Button startNewGameButton = GetNode<Button>("VBoxContainer/StartNewGameButton");
        startNewGameButton.Pressed += ButtonPressed;
        TranslationServer.SetLocale("fr");
        language = TranslationServer.GetLocale();
        //string language = OS.GetLocaleLanguage();
        GD.Print($"langauage: {language}");

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {

    }

    private async void ButtonPressed() {
        //we pass the locale to Dialogue Manager so he knows what translations load
        DialogueManager.LanguageLocaleChosen.Invoke(language);      

        PackedScene gameStartScene = (PackedScene)ResourceLoader.Load("res://Scenes/GameStartScene.tscn");
		Node gameStartNode = gameStartScene.Instantiate();
        GetTree().Root.AddChild(gameStartNode);
		//we wait at the very end before the next frame starts to ensure that gameStartNode was properly added to the tree.
		GetTree().ProcessFrame += DialogueManager.treeChanged;
        //DialogueManager.treeChanged.Invoke();
        Hide();
    }


}
