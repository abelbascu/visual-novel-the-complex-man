using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using static GameStateMachine;

public partial class SaveGameScreen : MarginContainer {
    private PackedScene saveGameSlotScene;
    private ScrollContainer scrollContainer;
    public VBoxContainer slotsContainer;
    private MarginContainer marginContainer;
    private Button goBackButton;
    private RichTextLabel noSavesAvailableLabel;
    public RichTextLabel SaveStatusLabel;
    private string noSavesTRANSLATE = "NO_SAVES_AVAILABLE";
    private string savingGameTRANSLATE = "SAVING_GAME";
    private string gamesavedSuccessTRANSLATE = "GAME_SAVED_SUCCESS";
    private const bool AUTODSAVE_DISABLED_CONST = false;
    private const bool AUTOSAVE_ENABLED_CONST = true;
    private SaveGameSlot saveGameSlot; //this is added at compile time, we get it in AddSaveOrLoadSlot method
    private const int NOTIFY_SAVE_STATUS_LABEL_FONT_SIZE = 40;
    private UITextTweenFadeIn fadeIn;
    private UITextTweenFadeOut fadeOut;


    public override void _Ready() {

        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();

        saveGameSlotScene = GD.Load<PackedScene>("res://Scenes/SaveGameSlot.tscn");
        scrollContainer = GetNode<ScrollContainer>("MarginContainer/ScrollContainer");
        slotsContainer = GetNode<VBoxContainer>("MarginContainer/ScrollContainer/VBoxContainer");
        marginContainer = GetNode<MarginContainer>("MarginContainer");
        SaveStatusLabel = GetNode<RichTextLabel>("MarginContainer3/RichTextLabel");

        //SaveStatusLabel.Hide();

        // Set size flags to prevent expansion
        marginContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        marginContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        goBackButton = GetNode<Button>("MarginContainer2/GoBackButton");
        goBackButton.Pressed += () => _ = OnGoBackButtonPressed();
        goBackButton.SetProcessInput(false);

        goBackButton.AnchorTop = 0;
        goBackButton.AnchorRight = 1;
        goBackButton.AnchorBottom = 0;
        goBackButton.AnchorLeft = 1;
        goBackButton.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        //goBackButton.Position = new Vector2(-10, 10); 

        UIThemeHelper.ApplyCustomStyleToButton(goBackButton);
    }


    public async Task ShowSaveStatusLabel(bool isBeingSaved = false) {

        DisableUserInput();

        SaveStatusLabel.Visible = true;

        // Use CallDeferred to ensure the label is updated in the next frame
        CallDeferred(nameof(UpdateSaveStatusLabel), isBeingSaved ? $"{savingGameTRANSLATE}" : $"{gamesavedSuccessTRANSLATE}");

        await fadeIn.FadeIn(SaveStatusLabel);
        await fadeOut.FadeOut(SaveStatusLabel);

        if (!isBeingSaved) {
            EnableUserInput();  //enable user input until saving is complete 
            RefreshSaveSlots();
            HideSaveStatusLabel();
        }
    }

    private void UpdateSaveStatusLabel(string text) {
        SaveStatusLabel.Text = $"[center]{TranslationServer.Translate(text)}[/center]";
    }

    public void HideSaveStatusLabel() {

        SaveStatusLabel.Visible = false;
    }

    private void DisableUserInput() {

        goBackButton.Disabled = true;
        goBackButton.FocusMode = Control.FocusModeEnum.None;
        goBackButton.MouseFilter = Control.MouseFilterEnum.Ignore;

        if (saveGameSlot != null) {
            saveGameSlot.DisableButton();
        }
    }

    private void EnableUserInput() {

        goBackButton.Disabled = false;
        goBackButton.FocusMode = Control.FocusModeEnum.All;
        goBackButton.MouseFilter = Control.MouseFilterEnum.Stop;
        goBackButton.ProcessMode = Node.ProcessModeEnum.Inherit;

        if (saveGameSlot != null) {
            saveGameSlot.EnableButton();
        }
    }

    private void SaveStatusLabelTheme() {
        var normalStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
    }

    private void CreateNoSavesAvailableLabel() {

        string translatedText = $"[center]{TranslationServer.Translate(noSavesTRANSLATE)}[/center]";

        noSavesAvailableLabel = new RichTextLabel {
            BbcodeEnabled = true,
            Text = translatedText,
            FitContent = false,
            Name = "NoSavesLabel",
            Visible = false
        };

        noSavesAvailableLabel.AddThemeFontSizeOverride("normal_font_size", 40);
        noSavesAvailableLabel.AddThemeColorOverride("default_color", Colors.White);
        noSavesAvailableLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        noSavesAvailableLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        noSavesAvailableLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        noSavesAvailableLabel.CustomMinimumSize = new Vector2(400, 500);
        slotsContainer.AddChild(noSavesAvailableLabel);
    }

    private async Task OnGoBackButtonPressed() {
        goBackButton.SetProcessInput(false);
        goBackButton.MouseFilter = MouseFilterEnum.Ignore;
        await UIFadeHelper.FadeOutControl(this, 0.6f);
        goBackButton.SetProcessInput(true);
        goBackButton.MouseFilter = MouseFilterEnum.Stop;
        Hide();

        GameStateManager.Instance.Fire(Trigger.GO_BACK_TO_MENU);
        //UIManager.Instance.
    }

    public void SetUpSaveOrLoadScreen(bool isLoadScreen) {
        // Clear existing slots
        foreach (Node child in slotsContainer.GetChildren()) {
            child.QueueFree();
        }
        CreateNoSavesAvailableLabel();

        // Populate with appropriate slots
        PopulateSaveOrLoadSlots(isLoadScreen);

        if (isLoadScreen)
            GameStateManager.Instance.Fire(Trigger.DISPLAY_LOAD_SCREEN);
        else
            GameStateManager.Instance.Fire(Trigger.DISPLAY_SAVE_SCREEN);
    }


    public async Task DisplaySaveScreen() {

        SetSlotButtonsState(false);
        goBackButton.SetProcessInput(false); //avoid hitting the ga back button repeatedly
        goBackButton.MouseFilter = MouseFilterEnum.Ignore;

        // Ensure the save game screen is fully transparent before showing
        Modulate = new Color(1, 1, 1, 0);
        
        Show();
        await UIFadeHelper.FadeInControl(this, 1.0f);

        SetSlotButtonsState(true);
        goBackButton.SetProcessInput(true);
        goBackButton.MouseFilter = MouseFilterEnum.Stop;
    }

    private void SetSlotButtonsState(bool enabled) {
        foreach (var child in slotsContainer.GetChildren()) {
            if (child is SaveGameSlot slot) {
                var button = slot.GetNode<Button>("MarginContainer2/HBoxContainer/Button");
                if (button != null) {
                    button.SetProcessInput(enabled);
                    button.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
                }
            }
        }
    }


    private void PopulateSaveOrLoadSlots(bool isLoadScreen) {
        List<LoadSaveManager.GameState> saveGames = LoadSaveManager.Instance.GetSavedGames();

        //we first clean the screen of any remaining save or load button.
        foreach (Node child in slotsContainer.GetChildren()) {
            if (child != noSavesAvailableLabel) {
                child.QueueFree();
            }
        }

        //show no saves available text
        if (saveGames.Count == 0 && isLoadScreen) {
            noSavesAvailableLabel.Visible = true;

        } else {

            noSavesAvailableLabel.Visible = false;
            //if it's the Save screen, add a Save button on first row
            if (!isLoadScreen)
                AddSaveOrLoadSlot(null, saveGames.Count + 1, isLoadScreen);

            for (int i = 0; i < saveGames.Count; i++) {
                AddSaveOrLoadSlot(saveGames[i], saveGames[i].SlotNumber, isLoadScreen);
            }
        }
    }


    private void AddSaveOrLoadSlot(LoadSaveManager.GameState gameState, int slotNumber, bool isLoadScreen) {
        var slotInstance = saveGameSlotScene.Instantiate<SaveGameSlot>();

        slotsContainer.AddChild(slotInstance);

        if (gameState != null) {
            slotInstance.SetLoadSlotData(gameState, slotNumber, isLoadScreen);
        } else if (!isLoadScreen) {
            slotInstance.SetSaveEmptySlot(slotNumber);
        }

        slotInstance.SaveRequested += OnSaveRequested;
        slotInstance.LoadRequested += OnLoadRequested;

        saveGameSlot = slotInstance;
    }

    private void OnSaveRequested(int slotNumber) {
        //-------------------TRIGGER GAME STATE CHANGE -----------------------------
        //--------------------------------------------------------------------------

        if (GameStateManager.Instance.IsInState(State.InGameMenuDisplayed, SubState.SaveScreenDisplayed)) {
            GameStateManager.Instance.Fire(Trigger.SAVE_GAME, AUTODSAVE_DISABLED_CONST);
        }
    }

    private void OnLoadRequested(string saveFilePath) {

        GameStateManager.Instance.Fire(Trigger.LOAD_GAME, saveFilePath);
        Hide();
    }

    public void RefreshSaveSlots() {
        PopulateSaveOrLoadSlots(false);
    }
}
