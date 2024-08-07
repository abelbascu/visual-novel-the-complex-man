using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

public partial class GameStateManager : Node {

    public static GameStateManager Instance { get; private set; }
    private const string SaveFileExtension = ".sav";
    private const string SaveDirectory = "saves";
    private const string PersistentDataFile = "persistent_data.dat";
    private const string AutosavePrefix = "autosave_";
    private const int AutosaveInterval = 300; // 5 minutes in seconds
    private float timeSinceLastAutosave = 0;
    private bool autosaveEnabled = true;

    public class GameState {
        public int SlotNumber { get; set; }
        public DialogueObject CurrentDialogueObject { get; set; }
        public int CurrentDialogueObjectID { get; set; }
        public int CurrentConversationID { get; set; }
        public string LanguageCode { get; set; }
        public List<int> PlayerChoicesList { get; set; }
        public DateTime SaveTime { get; set; }
        public TimeSpan TimePlayed { get; set; }
        public float DialoguesVisitedPercentage { get; set; }
        //public Image Screenshot { get; set; }
        public string VisualPath { get; set; }
        public VisualManager.VisualType VisualType;
        public bool IsAutosave { get; set; }
    }

    public class PersistentData {
        public int GamesPlayed { get; set; }
        public TimeSpan TotalTimePlayed { get; set; }
        public HashSet<int> DialoguesVisited { get; set; }
        public HashSet<int> EndingsSeen { get; set; }
    }

    private PersistentData persistentData;

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
    }

    public override void _Process(double delta) {
        if (autosaveEnabled) {
            timeSinceLastAutosave += (float)delta;
            if (timeSinceLastAutosave >= AutosaveInterval) {
                Autosave();
                timeSinceLastAutosave = 0;
            }
        }
    }

    public void ToggleAutosave(bool enable) {
        autosaveEnabled = enable;
        if (enable) {
            timeSinceLastAutosave = 0; // Reset the timer when enabling
        }
    }

    private void Autosave() {
        var gameState = CreateGameState();
        gameState.IsAutosave = true;
        string autosavePath = GetNextAutosaveFilePath();
        SaveGameState(gameState, autosavePath);
        GD.Print("Autosave completed: " + autosavePath);
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
                DialoguesVisited = new HashSet<int>(),
                EndingsSeen = new HashSet<int>()
            };
        }
    }

    private int GetNextSaveNumber() {
        string saveDirectoryPath = Path.Combine(OS.GetUserDataDir(), SaveDirectory);
        var allSaves = Directory.GetFiles(saveDirectoryPath, $"*{SaveFileExtension}");
        return allSaves.Length + 1;
    }

    public void SaveGame(bool isAutosave = false) {
        var gameState = CreateGameState();
        gameState.IsAutosave = isAutosave;
        string prefix = isAutosave ? AutosavePrefix : "save_";
        string saveFilePath = GetNextFilePath(prefix);
        gameState.SlotNumber = int.Parse(Path.GetFileNameWithoutExtension(saveFilePath).Substring(prefix.Length));
        SaveGameState(gameState, saveFilePath);
        UpdatePersistentData(gameState);
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
            DialoguesVisitedPercentage = CalculateDialoguesVisitedPercentage(),
            //Screenshot = CaptureScreenshot(),
            VisualPath = VisualManager.Instance.VisualPath,
            VisualType = VisualManager.Instance.visualType
        };
    }

    private void SaveGameState(GameState gameState, string filePath) {
        var json = JsonSerializer.Serialize(gameState);
        var encryptedData = EncryptData(json);
        File.WriteAllBytes(filePath, encryptedData);
    }

    private void UpdatePersistentData(GameState gameState) {
        persistentData.GamesPlayed++;
        persistentData.TotalTimePlayed += gameState.TimePlayed;
        persistentData.DialoguesVisited.Add(gameState.CurrentDialogueObjectID);

        SavePersistentData();
    }

    private void SavePersistentData() {
        string persistentDataPath = Path.Combine(OS.GetUserDataDir(), PersistentDataFile);
        string json = JsonSerializer.Serialize(persistentData);
        File.WriteAllText(persistentDataPath, json);
    }

    private string GetNextSaveFilePath() {
        return GetNextFilePath("save_");
    }

    private string GetNextAutosaveFilePath() {
        return GetNextFilePath(AutosavePrefix);
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
        DialogueManager.languageCode = gameState.LanguageCode;
        DialogueManager.Instance.playerChoicesList = gameState.PlayerChoicesList.Select(id => DialogueManager.Instance.GetDialogueObject(gameState.CurrentConversationID, id)).ToList();
        //VisualManager.Instance.DisplayImage(DialogueManager.Instance.currentDialogueObject.VisualPath);
        UIManager.Instance.inGameMenuButton.Show();
        VisualManager.Instance.VisualPath = gameState.VisualPath;
        VisualManager.Instance.visualType = gameState.VisualType;
        if (DialogueManager.Instance.playerChoicesList != null && DialogueManager.Instance.currentDialogueObject.Actor == "1") //if the current dialogue object it's a single player choice
        {
            DialogueManager.Instance.DisplayPlayerChoices(DialogueManager.Instance.playerChoicesList, DialogueManager.Instance.SetIsPlayerChoiceBeingPrinted);
            VisualManager.Instance.DisplayVisual(gameState.VisualPath, gameState.VisualType);
        } else
            DialogueManager.Instance.DisplayDialogueOrPlayerChoice(DialogueManager.Instance.currentDialogueObject);

        SetCurrentPlayTime(gameState.TimePlayed);
    }

    private void SetCurrentPlayTime(TimeSpan time) {

    }
    private byte[] EncryptData(string data) {
        return Encoding.UTF8.GetBytes(data);
    }

    private string DecryptData(byte[] encryptedData) {
        return Encoding.UTF8.GetString(encryptedData);
    }

    private TimeSpan GetCurrentPlayTime() {
        return TimeSpan.Zero;
    }

    private float CalculateDialoguesVisitedPercentage() {
        int totalDialogues = DialogueManager.Instance.conversationDialogues.Values.Sum(list => list.Count);
        int visitedDialogues = persistentData.DialoguesVisited.Count;
        return (float)visitedDialogues / totalDialogues * 100;
    }

    private Image CaptureScreenshot() {
        return null;
    }
}






