using Godot;
using System;


public partial class GameManager : Control {
    public static GameManager Instance { get; private set; }
    public DialogueManager DialogueManager { get; private set; }
    public UIManager UIManager {get; private set; }
    
    //public PlayerStateManager PlayerStateManager { get; private set; }
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

        // Make GameManager fill its parent
        AnchorRight = 1;
        AnchorBottom = 1;

        // Ignore mouse input if it doesn't need to interact directly
        MouseFilter = MouseFilterEnum.Ignore;

        if (Instance == null) {
            Instance = this;
            InitializeManagers();
        } else {
            QueueFree();
        }
    }

    private void InitializeManagers() {
        UIManager = GetNode<UIManager>("UIManager");
        DialogueManager = GetNode<DialogueManager>("DialogueManager");
    }
}
