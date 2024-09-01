using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using static GameStateMachine;

public partial class LoadSaveManager : Node {

    public static LoadSaveManager Instance { get; private set; }
    private const string SaveFileExtension = ".sav";
    private const string SaveDirectory = "saves";
    private const string PersistentDataFile = "persistent_data.dat";
    private const string AutosavePrefix = "autosave_";
    private const int AutosaveInterval = 15; // 5 minutes in seconds
    private float timeSinceLastAutosave = 0;
    private float totalTimeElapsedSinceGameStart;
    // private const bool AUTOSAVE_ENABLED = true;
    // private const bool AUTOSAVE_DISABLED = false;
    private RichTextLabel autosaveLabel;
    public MarginContainer autosaveLabelContainer;
    public const bool AUTOSAVING_COMPLETED_CONST = false;
    public const bool SAVING_COMPLETED_CONST = false;
    public const bool CURRENTLY_AUTOSAVING_CONST = true;
    public const bool CURRENTLY_SAVING_CONST = true;
    private UITextTweenFadeIn fadeIn;
    private UITextTweenFadeOut fadeOut;
    private readonly Color paleYellow = new Color(1, 1, 0.8f, 1);

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
        public State lastGameMode;
    }

    public class PersistentData {
        public int GamesPlayed { get; set; }
        public TimeSpan TotalTimePlayed { get; set; }
        public HashSet<int> DialoguesVisitedForAllGames { get; set; }
        public HashSet<int> EndingsSeen { get; set; }
    }

    private PersistentData persistentData;

    private DateTime gameStartTime = DateTime.Now;
    private TimeSpan totalPlayTime = TimeSpan.Zero;

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
        CreateAutoSaveStatusLabel();

        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();
        AddChild(fadeIn);
        AddChild(fadeOut);
    }

    private void CreateAutoSaveStatusLabel() {

        autosaveLabelContainer = new MarginContainer {
            CustomMinimumSize = new Vector2(400, 75),
            Visible = true
        };
        AddChild(autosaveLabelContainer);
        autosaveLabelContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.KeepSize);
        MoveChild(autosaveLabelContainer, -1);  // Move to top of the hierarchy


        // Create and set up the autosave label
        autosaveLabel = new RichTextLabel {
            //CustomMinimumSize = new Vector2(300, 75),
            BbcodeEnabled = true,
            Visible = true, //we set it to true so fade in/out can operate on the text.
        };
        autosaveLabel.AddThemeFontSizeOverride("normal_font_size", 28);
        autosaveLabel.AddThemeColorOverride("default_color", paleYellow);
        autosaveLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterLeft, Control.LayoutPresetMode.KeepSize);
        autosaveLabelContainer.AddChild(autosaveLabel);
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
    }

    private void PauseGameTimer() {
        if (GameStateManager.Instance.PreviousState != State.InDialogueMode) { return; }
        else {
            totalPlayTime += DateTime.Now - gameStartTime;
            gameStartTime = DateTime.Now;
        }
    }

    private void ResumeGameTimer() {
        gameStartTime = DateTime.Now;
    }


    private void OnDialogueVisited(int dialogueObjectID) {
        persistentData.DialoguesVisitedForAllGames.Add(dialogueObjectID);
        SavePersistentData();
    }

    public override async void _Process(double delta) {

        if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
            timeSinceLastAutosave += (float)delta;
            if (timeSinceLastAutosave >= AutosaveInterval) {
                _ = PerformAutosave();
            }
        }
    }

    private async Task PerformAutosave() {

        if (!GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
            return; // Don't start autosave if we're not in the correct state
        }

        GameStateManager.Instance.Fire(Trigger.AUTOSAVE_GAME);
        await SaveGame(true);
        GameStateManager.Instance.Fire(Trigger.AUTOSAVE_COMPLETED);
        GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        timeSinceLastAutosave = 0;
    }

    public async Task ShowAutosaveStatusLabel(bool isSaving) {
        string message = isSaving ? "AUTOSAVING..." : "AUTOSAVE COMPLETED";

        CallDeferred(nameof(UpdateAutosaveLabel), message);
        await fadeIn.FadeIn(autosaveLabel);
        await fadeOut.FadeOut(autosaveLabel);

        if (!isSaving)
            await fadeOut.FadeOut(autosaveLabel); /// where is the fade in first for AUTOSAVE COMPLETED TEXT???
    }

    private void UpdateAutosaveLabel(string text) {

        autosaveLabel.Text = $"[center]{text}[/center]";
    }

    public async Task SaveGame(bool isAutosave) {

        //-------------BEFORE SAVING THE GAME, SHOW THE 'SAVING' TEXT TO THE USER----------------------

        // Show "Saving" message for manual saves
        //as soon as the ingame menu is open we have already set autosave to false in MainMenu.DisplayInGameMenu()
        if (isAutosave) {
            totalPlayTime += DateTime.Now - gameStartTime;
            await ShowAutosaveStatusLabel(CURRENTLY_AUTOSAVING_CONST);
            //is is manual save
        } else {
            //contrary to autosave, we trigger the manual save Fire.Trigger when save button is pressed, no need to call it here
            await UIManager.Instance.saveGameScreen.ShowSaveStatusLabel(CURRENTLY_SAVING_CONST);
        }

        //--------------------------------DO THE ACTUAL SAVING HERE-------------------------------------

        var gameState = CreateGameState();
        gameState.IsAutosave = isAutosave;
        string prefix = isAutosave ? AutosavePrefix : "save_";
        string saveFilePath = GetNextFilePath(prefix);
        gameState.SlotNumber = int.Parse(Path.GetFileNameWithoutExtension(saveFilePath).Substring(prefix.Length));

        SaveGameState(gameState, saveFilePath);
        UpdatePersistentData(gameState);

        GD.Print($"{(isAutosave ? "Autosave" : "Manual save")} completed: {saveFilePath}");

        //-----------TELL THE USER THE SAVE WAS COMPLETE AND TRIGGER GAME STATE CHANGES--------------------------

        if (isAutosave) {
            await ShowAutosaveStatusLabel(AUTOSAVING_COMPLETED_CONST);
            gameStartTime = DateTime.Now;
            //timeSinceLastAutosave = 0;
        } else {
            await UIManager.Instance.saveGameScreen.ShowSaveStatusLabel(SAVING_COMPLETED_CONST);
            GameStateManager.Instance.Fire(Trigger.SAVING_COMPLETED);
            GameStateManager.Instance.Fire(Trigger.DISPLAY_SAVE_SCREEN);
        }
    }

    private void SaveGameState(GameState gameState, string filePath) {
        var json = JsonSerializer.Serialize(gameState);
        var encryptedData = EncryptData(json);
        File.WriteAllBytes(filePath, encryptedData);
    }

    private GameState CreateGameState() {
        return new GameState {
            CurrentDialogueObject = DialogueManager.Instance.currentDialogueObject,
            CurrentDialogueObjectID = DialogueManager.Instance.currentDialogueObject.ID,
            CurrentConversationID = DialogueManager.Instance.currentConversationID,
            //LanguageCode = TranslationServer.GetLocale(),
            PlayerChoicesList = DialogueManager.Instance.playerChoicesList.Select(d => d.ID).ToList(),
            SaveTime = DateTime.Now,
            TimePlayed = GetCurrentPlayTime(),
            DialoguesVisitedForAllGamesPercentage = CalculateDialoguesVisiteForAllGamesdPercentage(),
            VisualPath = VisualManager.Instance.VisualPath,
            VisualType = VisualManager.Instance.visualType
        };
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
            // GameStateManager.Instance.ENTER_LOADING_SUBSTATE();
            ApplyGameState(gameState);
        }
        GameLoaded.Invoke(); //do not remove, we need it to start game timer.
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
        //TranslationServer.SetLocale(gameState.LanguageCode);
        DialogueManager.Instance.playerChoicesList = gameState.PlayerChoicesList.Select(id => DialogueManager.Instance.GetDialogueObject(gameState.CurrentConversationID, id)).ToList();
        VisualManager.Instance.VisualPath = gameState.VisualPath;
        VisualManager.Instance.visualType = gameState.VisualType;

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






