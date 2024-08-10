using Godot;
using System;


public partial class GameManager : Control {
    public static GameManager Instance { get; private set; }
    public DialogueManager DialogueManager { get; private set; }
    public UIManager UIManager { get; private set; }

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
            GameInit();
        } else {
            QueueFree();
        }
    }

    private void GameInit() {
        // Note: Despite being displayed as "UIManager" in the Godot editor, the actual internal
        // node name is "UiManager" (with a lowercase 'i'). This seems to be a Godot bug.
        UIManager = GetNodeOrNull<UIManager>("UiManager");
        DialogueManager = GetNodeOrNull<DialogueManager>("DialogueManager");
        mainMenu = UIManager.GetNodeOrNull<MainMenu>("MainMenu");
        mainMenu.StartNewGameButtonPressed += OnStartButtonPressed;
        //first thing that happens in the game is displaying the Main Menu
        mainMenu.DisplayMainMenu();
        mainMenu.Show();
        GameStateManager.Instance.ToggleAutosave(false);
    }

    public void OnStartButtonPressed() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        UIManager.Instance.inGameMenuButton.Show();
        DialogueManager.Instance.currentDialogueID = DialogueManager.STARTING_DIALOGUE_ID;
        DialogueManager.Instance.currentConversationID = DialogueManager.STARTING_CONVO_ID;
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject
            (DialogueManager.Instance.currentConversationID, DialogueManager.Instance.currentDialogueID);
        DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);
        GameStateManager.Instance.ToggleAutosave(true);
    }
}
