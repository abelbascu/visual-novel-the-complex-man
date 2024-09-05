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
    private RichTextLabel autosaveStatusLabel;
    public MarginContainer autosaveStatusLabelContainer;
    public const bool AUTOSAVING_COMPLETED_CONST = false;
    public const bool SAVING_COMPLETED_CONST = false;
    public const bool CURRENTLY_AUTOSAVING_CONST = true;
    public const bool CURRENTLY_SAVING_CONST = true;
    private UITextTweenFadeIn fadeIn;
    private UITextTweenFadeOut fadeOut;
    private readonly Color paleYellow = new Color(1, 1, 0.8f, 1);
    private string autosaving_TRANSLATE = "AUTOSAVING";
    private string autosaveCompletedSuccess_TRANSLATE = "AUTOSAVE_COMPLETED_SUCCESS";
    private string savingGameTRANSLATE = "SAVING_GAME";
    private string gamesavedSuccessTRANSLATE = "GAME_SAVED_SUCCESS";
    private string errorSaveMessageTitleTRANSLATE ="SAVE_ERROR";
    private string errorAutosaveMessageTRANSLATE = "AUTOSAVE_ERROR";
    private string errorManualSaveMessageTRANSLATE = "MANUAL_SAVE_ERROR";

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

        fadeIn = new UITextTweenFadeIn(); //TO REMOVE
        fadeOut = new UITextTweenFadeOut(); //TO REMO
        AddChild(fadeIn);   //TO REMOVE
        AddChild(fadeOut); //TO REMOVE
    }

    private void CreateAutoSaveStatusLabel() {

        autosaveStatusLabelContainer = new MarginContainer {
            CustomMinimumSize = new Vector2(400, 75),
            Visible = true,
            AnchorsPreset = (int)Control.LayoutPreset.TopWide
        };
        AddChild(autosaveStatusLabelContainer);
        MoveChild(autosaveStatusLabelContainer, -1);  // Move to top of the hierarchy

        // Create and set up the autosave label
        autosaveStatusLabel = new RichTextLabel {
            CustomMinimumSize = new Vector2(400, 75),
            BbcodeEnabled = true,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.Off,
            Visible = true, //we set it to true so fade in/out can operate on the text.
        };
        autosaveStatusLabel.CustomMinimumSize = new Vector2(0, 75);
        autosaveStatusLabel.AddThemeFontSizeOverride("normal_font_size", 28);
        autosaveStatusLabel.AddThemeColorOverride("default_color", paleYellow);
        autosaveStatusLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        autosaveStatusLabelContainer.AddThemeConstantOverride("margin_left", 10);
        autosaveStatusLabelContainer.AddThemeConstantOverride("margin_right", 10);
        autosaveStatusLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        autosaveStatusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        autosaveStatusLabelContainer.AddChild(autosaveStatusLabel);
    }

    private void SubscribeToEvents() {
        DialogueManager.Instance.DialogueVisited += OnDialogueVisited;
        UIManager.Instance.mainMenu.StartNewGameButtonPressed += StartGameTimer;
        GameLoaded += StartGameTimer;
    }

    private void StartGameTimer() {
        gameStartTime = DateTime.Now;
    }

    public void PauseGameTimer() {
        totalPlayTime += DateTime.Now - gameStartTime;
        gameStartTime = DateTime.Now;
    }


    public void ResumeGameTimer() {
        gameStartTime = DateTime.Now;
        timeSinceLastAutosave = 0;
    }


    private void OnDialogueVisited(int dialogueObjectID) {
        persistentData.DialoguesVisitedForAllGames.Add(dialogueObjectID);
        SavePersistentData();
    }

    private bool isAutosaving = false;

    public override async void _Process(double delta) {

        if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None) && !isAutosaving) {
            timeSinceLastAutosave += (float)delta;
            if (timeSinceLastAutosave >= AutosaveInterval) {
                isAutosaving = true;
                UIManager.Instance.inGameMenuButton.DisableIngameMenuButton();
                _ = PerformAutosave();
            }
        }
    }

    public async Task PerformAutosave() {

        if (!GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
            isAutosaving = false;
            UIManager.Instance.inGameMenuButton.EnableIngameMenuButton();
            return; // Don't start autosave if we're not in the correct state
        }

        try {
            isAutosaving = true;
            UIManager.Instance.inGameMenuButton.DisableIngameMenuButton();

            GameStateManager.Instance.Fire(Trigger.AUTOSAVE_GAME);

            await ShowSaveStatus();
            await SaveGame(true);

            GameStateManager.Instance.Fire(Trigger.AUTOSAVE_COMPLETED);
            await ShowSaveStatus();

            GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
        } catch (Exception ex) {
            GD.PrintErr($"Error during autosave: {ex.Message}");
            await ShowSaveStatus(true, errorAutosaveMessageTRANSLATE);
        } finally {
            isAutosaving = false;
            timeSinceLastAutosave = 0;

            if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
                UIManager.Instance.inGameMenuButton.EnableIngameMenuButton();
            }
        }
    }

    public async Task PerformManualSave() {
        try {
            //GameStateManager.Instance.Fire(Trigger.SAVE_GAME);
            await ShowSaveStatus();

            await SaveGame(false);

            UIManager.Instance.saveGameScreen.RefreshSaveSlots();
            GameStateManager.Instance.Fire(Trigger.SAVING_COMPLETED);

            await ShowSaveStatus();

            GameStateManager.Instance.Fire(Trigger.DISPLAY_SAVE_SCREEN);

        } catch (Exception ex) {
            GD.PrintErr($"Error during manual save: {ex.Message}");
            await ShowSaveStatus(true, errorManualSaveMessageTRANSLATE);
        }
    }


    public async Task ShowSaveStatus(bool isError = false, string errorMessage = null) {
        string message;
        RichTextLabel label;

        if (isError) {
            message = $"{TranslationServer.Translate(errorSaveMessageTitleTRANSLATE)}: {errorMessage}";
            label = GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed)
                ? UIManager.Instance.saveGameScreen.SaveStatusLabel
                : autosaveStatusLabel;
        } else if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.AutoSaving)) {
            message = TranslationServer.Translate(autosaving_TRANSLATE);
            label = autosaveStatusLabel;
        } else if (GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.AutoSavingCompleted)) {
            message = TranslationServer.Translate(autosaveCompletedSuccess_TRANSLATE);
            label = autosaveStatusLabel;
        } else if (GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.Saving)) {
            message = TranslationServer.Translate(savingGameTRANSLATE);
            label = UIManager.Instance.saveGameScreen.SaveStatusLabel;
        } else if (GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.SavingCompleted)) {
            message = TranslationServer.Translate(gamesavedSuccessTRANSLATE);
            label = UIManager.Instance.saveGameScreen.SaveStatusLabel;
        } else {
            throw new InvalidOperationException("Invalid save state");
        }

        label.Text = $"[center]{message}[/center]";
        label.Visible = true;

        await fadeIn.FadeIn(label);
        await fadeOut.FadeOut(label);
    }


    public async Task SaveGame(bool isAutosave) {

        try {
            if (isAutosave)
                totalPlayTime += DateTime.Now - gameStartTime;
            //is is manual save

            var gameState = CreateGameState();
            gameState.IsAutosave = isAutosave;
            string prefix = isAutosave ? AutosavePrefix : "save_";
            string saveFilePath = GetNextFilePath(prefix);
            gameState.SlotNumber = int.Parse(Path.GetFileNameWithoutExtension(saveFilePath).Substring(prefix.Length));

            await Task.Run(() => SaveGameState(gameState, saveFilePath));
            UpdatePersistentData(gameState);

            GD.Print($"{(isAutosave ? "Autosave" : "Manual save")} completed: {saveFilePath}");

            if (isAutosave)
                gameStartTime = DateTime.Now;

        } catch (Exception ex) {
            string errorMessage = $"Error during {(isAutosave ? "autosave" : "save")}: {ex.Message}";
            GD.PrintErr(errorMessage);
            if (isAutosave) {
                await ShowSaveStatus(true, $"Autosave failed: {ex.Message}");
            } else {
                await ShowSaveStatus(true, $"Save failed: {ex.Message}");
            }
            throw; // Re-throw the exception to be caught in PerformAutosave or the manual save process
        }
    }

    private void SaveGameState(GameState gameState, string filePath) {

        try {
            var json = JsonSerializer.Serialize(gameState);
            var encryptedData = EncryptData(json);
            File.WriteAllBytes(filePath, encryptedData);
        } catch (Exception ex) {
            throw new Exception($"Faile to save game state: {ex.Message}", ex);
        }
    }

    private GameState CreateGameState() {
        return new GameState {
            CurrentDialogueObject = DialogueManager.Instance.currentDialogueObject,
            CurrentDialogueObjectID = DialogueManager.Instance.currentDialogueObject.ID,
            CurrentConversationID = DialogueManager.Instance.currentConversationID,
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
            ApplyGameState(gameState);
        }
        GameLoaded.Invoke(); //do not remove, we need it to start game timer.
    }

    //----------------------------------------------------------------------------------------------------
    //--------------------------------------------------LOAD GAME ----------------------------------------
    //----------------------------------------------------------------------------------------------------

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






