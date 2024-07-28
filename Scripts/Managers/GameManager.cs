using Godot;
using System;


public partial class GameManager : Control {
    public static GameManager Instance { get; private set; }
    public DialogueManager DialogueManager { get; private set; }
    public UIManager UIManager { get; private set; }
    //public static Action StartButtonPressed;

    //public PlayerStateManager PlayerStateManager { get; private set; }
    // public MediaManager MediaManager { get; private set; }
    // public MinigameManager MinigameManager { get; private set; }
    // public AchievementsManager AchievementsManager { get; private set; }

    //private GameStateMachine stateMachine;

    private PackedScene mainMenuPackedScene;
    private Control mainMenuScene;
    private MainMenu mainMenu;

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
        // Note: Despite being displayed as "UIManager" in the Godot editor,
        // the actual node name is "UiManager" (with a lowercase 'i'). This seems to be a Godot bug.
        UIManager = GetNodeOrNull<UIManager>("UiManager");
        DialogueManager = GetNodeOrNull<DialogueManager>("DialogueManager");
        mainMenu = UIManager.GetNodeOrNull<MainMenu>("MainMenu");
        mainMenu.StartButtonPressed += OnStartButtonPressed;
    }

    public void OnStartButtonPressed() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject(DialogueManager.Instance.currentConversationID, DialogueManager.Instance.currentDialogueID);
        DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);
    }
}
