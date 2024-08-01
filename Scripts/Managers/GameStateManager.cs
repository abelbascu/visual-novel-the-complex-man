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

    public class GameState {
        public DialogueObject CurrentDialogueObject {get; set;}
        public int CurrentDialogueObjectID { get; set; }
        public int CurrentConversationID { get; set; }
        public string LanguageCode { get; set; }
        public List<int> PlayerChoicesList { get; set; }
        public DateTime SaveTime { get; set; }
        public TimeSpan TimePlayed { get; set; }
        public float DialoguesVisitedPercentage { get; set; }
        public Image Screenshot { get; set; }
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

    public void SaveGame() {
        var gameState = new GameState {
            CurrentDialogueObject = DialogueManager.Instance.currentDialogueObject,
            CurrentDialogueObjectID = DialogueManager.Instance.currentDialogueObject.ID,
            CurrentConversationID = DialogueManager.Instance.currentConversationID,
            LanguageCode = DialogueManager.languageCode,
            PlayerChoicesList = DialogueManager.Instance.playerChoicesList.Select(d => d.ID).ToList(),
            SaveTime = DateTime.Now,
            TimePlayed = GetCurrentPlayTime(),
            DialoguesVisitedPercentage = CalculateDialoguesVisitedPercentage(),
            Screenshot = CaptureScreenshot()
        };
        string saveFilePath = GetNextSaveFilePath();
        SaveGameState(gameState, saveFilePath);
        UpdatePersistentData(gameState);
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
        string saveDirectoryPath = Path.Combine(OS.GetUserDataDir(), SaveDirectory);
        int saveNumber = 1;
        string filePath;
        do {
            filePath = Path.Combine(saveDirectoryPath, $"save_{saveNumber:DS}{SaveFileExtension}");
            saveNumber++;
        } while (File.Exists(filePath));

        return filePath;
    }


    public List<GameState> GetSavedGames() {
        var savedGames = new List<GameState>();
        string saveDirectoryPath = Path.Combine(OS.GetUserDataDir(), SaveDirectory);
        foreach (string filePath in Directory.GetFiles(saveDirectoryPath, $"*{SaveFileExtension}")) {
            var gameState = LoadGameState(filePath);
            if (gameState != null) {
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
        if (File.Exists(filePath)) {
            var encryptedData = File.ReadAllBytes(filePath);
            var json = DecryptData(encryptedData);
            return JsonSerializer.Deserialize<GameState>(json);
        }
        return null;
    }

    private void ApplyGameState(GameState gameState) {
        DialogueManager.Instance.currentDialogueObject = DialogueManager.Instance.GetDialogueObject(gameState.CurrentConversationID, gameState.CurrentDialogueObjectID);
        DialogueManager.Instance.currentConversationID = gameState.CurrentConversationID;
        DialogueManager.languageCode = gameState.LanguageCode;
        DialogueManager.Instance.playerChoicesList = gameState.PlayerChoicesList.Select(id => DialogueManager.Instance.GetDialogueObject(gameState.CurrentConversationID, id)).ToList();
        VisualManager.Instance.DisplayImage(gameState.CurrentDialogueObject.VisualPath);

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






