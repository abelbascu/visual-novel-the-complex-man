using Godot;
using System;


public partial class GameManager : Node2D {
    public static GameManager Instance { get; private set; }

    public DialogueManager DialogueManager { get; private set; }
    // public PlayerStateManager PlayerStateManager { get; private set; }
    // public MediaManager MediaManager { get; private set; }
    // public MinigameManager MinigameManager { get; private set; }
    // public AchievementsManager AchievementsManager { get; private set; }

    //private GameStateMachine stateMachine;

    private PackedScene mainMenuPackedScene;
    private Control mainMenuScene;

    public enum GameState {
        MainMenu,
        Dialogue,
        Cinematic,
        Minigame
    }

    public override void _Ready() {
        if (Instance == null) {
            Instance = this;
            InitializeManagers();
            LoadMainMenu();
        } else {
            QueueFree();
        }
    }

    private void InitializeManagers() {
        DialogueManager = GetNode<DialogueManager>("DialogueManager");
        mainMenuPackedScene = GD.Load<PackedScene>("res://Scenes/MainMenu.tscn");

    }

    private void LoadMainMenu() {
        if (mainMenuScene != null) {
            mainMenuScene.QueueFree();
        }

        mainMenuScene = mainMenuPackedScene.Instantiate() as Control;

        if (mainMenuScene != null) {
            var gameNode = GetParent();
            gameNode.CallDeferred(Node.MethodName.AddChild, mainMenuScene);
            GD.Print("Main menu loaded successfully"); // Debug print
        } else {
            GD.PrintErr("Failed to instantiate MainMenu scene"); // Error print
        }
    }


}
// ... other GameManager methods ...
