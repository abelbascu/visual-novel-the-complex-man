using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Reflection.Metadata;

public partial class GameStateManager : Node {

    public static GameStateManager Instance { get; private set; }
    private const string SaveFileExtension = ".sav";
    private const string SaveDirectory = "saves";
    private const string PersistentDataFile = "persistent_data.dat";
    private const string AutosavePrefix = "autosave_";
    private const int AutosaveInterval = 300; // 5 minutes in seconds
    private float timeSinceLastAutosave = 0;
    private float totalTimeElapsedSinceGameStart;
    // private const bool AUTOSAVE_ENABLED = true;
    // private const bool AUTOSAVE_DISABLED = false;
    private bool isAutoSave = true;

    public int DialoguesVisitedID;

    public class GameState {
        public int SlotNumber { get; set; }
        public DialogueObject CurrentDialogueObject { get; set; }
        public int CurrentDialogueObjectID { get; set; }
        public int CurrentConversationID { get; set; }
        public string LanguageCode { get; set; }
        public List<int> PlayerChoicesList { get; set; }
        public DateTime SaveTime { get; set; }
        public TimeSpan TimePlayed { get; set; }
        public float DialoguesVisitedForAllGamesPercentage { get; set; }
        public string VisualPath { get; set; }
        public VisualManager.VisualType VisualType;
        public bool IsAutosave { get; set; }
    }

    public class PersistentData {
        public int GamesPlayed { get; set; }
        public TimeSpan TotalTimePlayed { get; set; }
        public HashSet<int> DialoguesVisitedForAllGames { get; set; }
        public HashSet<int> EndingsSeen { get; set; }
    }

    private PersistentData persistentData;

    private DateTime gameStartTime;
    private TimeSpan totalPlayTime = TimeSpan.Zero;
    private bool isGameActive = false;

    public Action GameLoaded;

    public override void _EnterTree() {
        if (Instance == null) {
            Instance = this;
        } else {
            QueueFree();
        }
    }

    public override void _Ready() {
        string saveDirectoryPath = Path.Combine(OS.GetUserDataDir(), SaveDirectory);
        Directory.CreateDirectory(saveDirectoryPath);
        LoadPersistentData();
        CallDeferred(nameof(SubscribeToEvents));
    }

    private void SubscribeToEvents() {
        DialogueManager.Instance.DialogueVisited += OnDialogueVisited;
        UIManager.Instance.mainMenu.StartNewGameButtonPressed += StartGameTimer;
        GameLoaded += StartGameTimer;
        UIManager.Instance.mainMenu.MainMenuOpened += PauseGameTimer;
        UIManager.Instance.mainMenu.InGameMenuOpened += PauseGameTimer;
        UIManager.Instance.mainMenu.MainMenuClosed += ResumeGameTimer;
        UIManager.Instance.mainMenu.InGameMenuClosed += ResumeGameTimer;
    }

    private void StartGameTimer() {
        gameStartTime = DateTime.Now;
        isGameActive = true;
    }

    private void PauseGameTimer() {
        if (isGameActive) {
            totalPlayTime += DateTime.Now - gameStartTime;
            isGameActive = false;
        }
    }

    private void ResumeGameTimer() {
        if (!isGameActive) {
            gameStartTime = DateTime.Now;
            isGameActive = true;
        }
    }

    private void OnDialogueVisited(int dialogueObjectID) {
        persistentData.DialoguesVisitedForAllGames.Add(dialogueObjectID);
        SavePersistentData();
    }

    public override void _Process(double delta) {
        if (isAutoSave) {
            timeSinceLastAutosave += (float)delta;
            if (timeSinceLastAutosave >= AutosaveInterval) {
                SaveGame(isAutoSave);
                timeSinceLastAutosave = 0;
            }
        }
    }

    public void ToggleAutosave(bool isAutosave) {
        isAutoSave = isAutosave; // I NEED TO REFACTOR THIS LINE I NEED TO REFACTOR THIS LINE I NEED TO REFACTOR THIS LINE I NEED TO REFACTOR THIS LINE
        if (isAutoSave) {
            timeSinceLastAutosave = 0; // Reset the timer when enabling
        }
    }

    public void SaveGame(bool isAutosave) {
        //as soon as the ingame menu is open we have already set autosave to false in MainMenu.DisplayInGameMenu()
        if (isAutosave == false) 
            PauseGameTimer();
        //if the ingame menu is closed and the game is active, autosave will jump to this line
        else
        {
            totalPlayTime += DateTime.Now - gameStartTime;
            gameStartTime = DateTime.Now;
        }

        var gameState = CreateGameState();
        gameState.IsAutosave = isAutosave;
        string prefix = isAutosave ? AutosavePrefix : "save_";
        string saveFilePath = GetNextFilePath(prefix);
        gameState.SlotNumber = int.Parse(Path.GetFileNameWithoutExtension(saveFilePath).Substring(prefix.Length));
        SaveGameState(gameState, saveFilePath);
        UpdatePersistentData(gameState);
        if (isAutosave) {
            GD.Print("Autosave completed: " + saveFilePath);
        } else
            GD.Print("Manual save completed: " + saveFilePath);
    }

    private void SaveGameState(GameState gameState, string filePath) {
        var json = JsonSerializer.Serialize(gameState);
        var encryptedData = EncryptData(json);
        File.WriteAllBytes(filePath, encryptedData);
    }

    public void LoadPersistentData() {
        string persistentDataPath = Path.Combine(OS.GetUserDataDir(), PersistentDataFile);
        if (File.Exists(persistentDataPath)) {
            string json = File.ReadAllText(persistentDataPath);
            persistentData = JsonSerializer.Deserialize<PersistentData>(json);
        } else {
            persistentData = new PersistentData {
                GamesPlayed = 0,
                TotalTimePlayed = TimeSpan.Zero,
                DialoguesVisitedForAllGames = new HashSet<int>(),
                EndingsSeen = new HashSet<int>()
            };
        }
    }

    private GameState CreateGameState() {
        return new GameState {
            CurrentDialogueObject = DialogueManager.Instance.currentDialogueObject,
            CurrentDialogueObjectID = DialogueManager.Instance.currentDialogueObject.ID,
            CurrentConversationID = DialogueManager.Instance.currentConversationID,
            LanguageCode = TranslationServer.GetLocale(),
            PlayerChoicesList = DialogueManager.Instance.playerChoicesList.Select(d => d.ID).ToList(),
            SaveTime = DateTime.Now,
            TimePlayed = GetCurrentPlayTime(),
            DialoguesVisitedForAllGamesPercentage = CalculateDialoguesVisiteForAllGamesdPercentage(),
            VisualPath = VisualManager.Instance.VisualPath,
            VisualType = VisualManager.Instance.visualType
        };
    }

    private void UpdatePersistentData(GameState gameState) {
        persistentData.GamesPlayed++;
        SavePersistentData();
    }

    private void SavePersistentData() {
        string persistentDataPath = Path.Combine(OS.GetUserDataDir(), PersistentDataFile);
        string json = JsonSerializer.Serialize(persistentData);
        File.WriteAllText(persistentDataPath, json);
    }

    private string GetNextFilePath(string prefix) {
        string saveDirectoryPath = Path.Combine(OS.GetUserDataDir(), SaveDirectory);
        var allSaves = Directory.GetFiles(saveDirectoryPath, $"*{SaveFileExtension}");
        int highestNumber = 0;

        foreach (var save in allSaves) {
            string fileName = Path.GetFileNameWithoutExtension(save);
            if (int.TryParse(fileName.Substring(fileName.LastIndexOf('_') + 1), out int number)) {
                highestNumber = Math.Max(highestNumber, number);
            }
        }

        int nextNumber = highestNumber + 1;
        return Path.Combine(saveDirectoryPath, $"{prefix}{nextNumber:D3}{SaveFileExtension}");
    }

    public List<GameState> GetSavedGames() {
        var savedGames = new List<GameState>();
        string saveDirectoryPath = Path.Combine(OS.GetUserDataDir(), SaveDirectory);
        foreach (string filePath in Directory.GetFiles(saveDirectoryPath, $"*{SaveFileExtension}")) {
            var gameState = LoadGameState(filePath);
            if (gameState != null) {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                gameState.IsAutosave = fileName.StartsWith(AutosavePrefix);
                gameState.SlotNumber = int.Parse(fileName.Substring(fileName.LastIndexOf('_') + 1));
                savedGames.Add(gameState);
            }
        }

        return savedGames.OrderByDescending(g => g.SaveTime).ToList();
    }

    public void LoadGame(string saveFilePath) {
        var gameState = LoadGameState(saveFilePath);
        if (gameState != null) {
            ApplyGameState(gameState);
        }
        ToggleAutosave(true);
        GameLoaded.Invoke();

    }

    private GameState LoadGameState(string filePath) {

        // Normalize the path to ensure consistent slash direction
        filePath = Path.GetFullPath(filePath);

        if (File.Exists(filePath)) {
            var encryptedData = File.ReadAllBytes(filePath);
            var json = DecryptData(encryptedData);
            return JsonSerializer.Deserialize<GameState>(json);
        }
        GD.Print($"File not found: {filePath}");
        return null;
    }

    private void ApplyGameState(GameState gameState) {
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject(gameState.CurrentConversationID, gameState.CurrentDialogueObjectID);
        DialogueManager.Instance.currentConversationID = gameState.CurrentConversationID;
        TranslationServer.SetLocale(gameState.LanguageCode);
        DialogueManager.Instance.playerChoicesList = gameState.PlayerChoicesList.Select(id => DialogueManager.Instance.GetDialogueObject(gameState.CurrentConversationID, id)).ToList();
        UIManager.Instance.inGameMenuButton.Show();
        VisualManager.Instance.VisualPath = gameState.VisualPath;
        VisualManager.Instance.visualType = gameState.VisualType;
        //A BIT HACKY FIX where if the currentDialogObj is a PlayerChoice, and the user clicked on it and then saved the game, and if its DestinationDialogueID are PlayerChoices,
        //it means that they were all already saved in a List and displayed to the screen, so when loading that saved game it should not display that currentDialogObj, but only its associated playerChoices
        if (DialogueManager.Instance.playerChoicesList != null && DialogueManager.Instance.currentDialogueObject.Actor == "1") //if the current dialogue object it's a single player choice
        {
            //notice that we don't use DisplayDialogueOrPlayerChoice(DialogueObject dialogObj) to avoid displaying the already visited player choice that is still hold in the current dialogue object
            //until the player selects a new player choice. Notice that most times, after an NPC or actor dialogue, a group of player choices may be displayed, but it may also happen that after a 
            //player choice is displayed, more new player chocies are displayed. We are solving this rare case here. 
            DialogueManager.Instance.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, DialogueManager.Instance.SetIsPlayerChoiceBeingPrinted);
            VisualManager.Instance.DisplayVisual(gameState.VisualPath, gameState.VisualType);
        } else
            DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);

        SetCurrentPlayTime(gameState.TimePlayed);
    }

    private void SetCurrentPlayTime(TimeSpan time) {
        totalPlayTime = time;
        gameStartTime = DateTime.Now;
    }
    private byte[] EncryptData(string data) {
        return Encoding.UTF8.GetBytes(data);
    }

    private string DecryptData(byte[] encryptedData) {
        return Encoding.UTF8.GetString(encryptedData);
    }

    private TimeSpan GetCurrentPlayTime() {
        return totalPlayTime;
    }

    private float CalculateDialoguesVisiteForAllGamesdPercentage() {
        int totalDialogues = DialogueManager.Instance.conversationDialogues.Values.Sum(list => list.Count);
        int visitedDialoguesForAllGames = persistentData.DialoguesVisitedForAllGames.Count;
        return (float)visitedDialoguesForAllGames / totalDialogues * 100;
    }
}






