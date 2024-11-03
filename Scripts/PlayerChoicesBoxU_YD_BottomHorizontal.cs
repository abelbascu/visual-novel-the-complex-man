using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class PlayerChoicesBoxU_YD_BottomHorizontal : MarginContainer {
  public Action<Vector2> SizeChanged;
  public Action FinishedDisplayingPlayerChoice;
  private MarginContainer globalMarginContainer;
  public VBoxContainer playerChoicesContainer;
  private PackedScene playerChoiceButtonScene;
  private NinePatchRect backgroundRect;

  private Dictionary<ulong, TaskCompletionSource<bool>> buttonReadyTasks = new Dictionary<ulong, TaskCompletionSource<bool>>();
  private Dictionary<ulong, Action> buttonReadyActions = new Dictionary<ulong, Action>();

  private void OnPlayerChoicesContainerDraw() {
    var rect = playerChoicesContainer.GetRect();
    playerChoicesContainer.DrawRect(rect, Colors.Red, false, 10);
  }

  public override void _Ready() {
    Show();

    backgroundRect = GetNode<NinePatchRect>("NinePatchRect"); // Adjust the path if needed
    globalMarginContainer = GetNode<MarginContainer>("GloalMarginContainer");
    playerChoicesContainer = GetNode<VBoxContainer>("GloalMarginContainer/PlayerChoicesContainer");

    playerChoicesContainer.Connect("draw", new Callable(this, nameof(OnPlayerChoicesContainerDraw)));

    if (backgroundRect != null) {
      // adjust this value to change transparency
      backgroundRect.Modulate = new Color(backgroundRect.Modulate, 0.9f);
    }

    //anchors mode
    // LayoutMode = 1;

    CustomMinimumSize = new Vector2(1200, 225);

    SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
    SizeFlagsVertical = SizeFlags.Fill;
    GrowVertical = GrowDirection.Begin;
    AnchorLeft = 0.5f;
    AnchorRight = 0.5f;
    AnchorBottom = 0.98f;

    // Add margins to the PlayerChoicesBoxUI
    AddThemeConstantOverride("margin_left", 40);
    AddThemeConstantOverride("margin_right", 40);


    playerChoiceButtonScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoiceButton.tscn");


    playerChoicesContainer.CustomMinimumSize = new Vector2(
      globalMarginContainer.Size.X - globalMarginContainer.GetThemeConstant("margin_left") - globalMarginContainer.GetThemeConstant("margin_right"),
      100
  );

    //The space between the BoxContainer's elements, in pixels.
    playerChoicesContainer.AddThemeConstantOverride("separation", 20);

    playerChoicesContainer.AddThemeColorOverride("bg_color", new Color(1, 0.5f, 0, 0.2f)); // Orange with some transparency


    Resized += () => OnResized();
  }


  public void OnResized() {
    SizeChanged?.Invoke(Size);
  }

  public async Task DisplayPlayerChoices(List<DialogueObject> playerChoices, string languageCode) {
    RemoveAllPlayerChoiceButtons();
    var tasks = new List<Task>();

    foreach (var playerChoiceObject in playerChoices) {
      if (!ButtonExistsForPlayerChoice(playerChoiceObject)) {
        var tcs = new TaskCompletionSource<bool>();
        tasks.Add(tcs.Task);
        CreateAndAddPlayerChoiceButton(playerChoiceObject, languageCode, tcs);
      }
    }

    //only when all tasks associated to each button are completed 
    //(when all buttons are ready in the scene, we know that as we will connect to the ready signal)
    //only then we willl show the box with all the buttons.
    await Task.WhenAll(tasks);

    CallDeferred(nameof(Show));
    GD.Print("All player choice buttons are ready");
    GD.Print("DisplayPlayerChoices completed");
  }

  private void CreateAndAddPlayerChoiceButton(DialogueObject playerChoiceObject, string languageCode, TaskCompletionSource<bool> tcs) {
    string playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);
    PlayerChoiceButton playerChoiceButton = playerChoiceButtonScene.Instantiate<PlayerChoiceButton>();

    buttonReadyTasks[playerChoiceButton.GetInstanceId()] = tcs;
    //check if the button is ready in the scene, if so we will set  its associated task to true
    var connectionResult = playerChoiceButton.Connect("ready", Callable.From(() => OnPlayerChoiceButtonReady(playerChoiceButton)));
    GD.Print($"Connection result: {connectionResult}");

    playerChoiceButton.SetDialogueObject(playerChoiceObject);
    playerChoicesContainer.AddChild(playerChoiceButton);
    playerChoiceButton.SetText(playerChoiceToDisplay);
  }

  //!what doees this do?
  private void OnPlayerChoiceButtonReady(PlayerChoiceButton button) {
    if (buttonReadyTasks.TryGetValue(button.GetInstanceId(), out var tcs)) {
      tcs.SetResult(true);
      buttonReadyTasks.Remove(button.GetInstanceId());
    }
  }

  //!this is useless we can remove it
  // FinishedDisplayingPlayerChoice?.Invoke();


  public bool ButtonExistsForPlayerChoice(DialogueObject playerChoiceObject) {
    var existingButtons = playerChoicesContainer.GetChildren()
        .OfType<PlayerChoiceButton>()
        .ToList();
    return existingButtons.Any(button => button.HasMatchingDialogueObject(playerChoiceObject));
  }


  public string GetLocalePlayerChoice(DialogueObject playerChoiceObj, string locale) {
    return locale switch {
      "fr" => playerChoiceObj.FrenchText,
      "ca" => playerChoiceObj.CatalanText,
      _ => playerChoiceObj.DialogueTextDefault
    };
  }

  public async Task RemoveAllNoGroupChildrenWithSameOriginID(DialogueObject dialogueObject) {
    List<PlayerChoiceButton> buttonsToRemove = new List<PlayerChoiceButton>();

    foreach (PlayerChoiceButton child in playerChoicesContainer.GetChildren()) {
      if (child.dialogueObject.NoGroupParentID == dialogueObject.NoGroupParentID) {
        buttonsToRemove.Add(child);
      }
    }

    foreach (var button in buttonsToRemove) {

      button.Disconnect("ready", new Callable(this, nameof(OnPlayerChoiceButtonReady)));
      buttonReadyActions.Remove(button.GetInstanceId());
      playerChoicesContainer.RemoveChild(button);
      button.QueueFree();
    }
  }

  public async Task RemoveAllPlayerChoiceButtons() {
    foreach (Node child in playerChoicesContainer.GetChildren()) {
      if (child is PlayerChoiceButton button) {

        button.Disconnect("ready", new Callable(this, nameof(OnPlayerChoiceButtonReady)));
        buttonReadyActions.Remove(button.GetInstanceId());
        playerChoicesContainer.RemoveChild(child);
        child.QueueFree();
      }
    }
  }

  public List<PlayerChoiceButton> GetPlayerChoiceButtons() {
    var buttons = new List<PlayerChoiceButton>();
    if (globalMarginContainer != null && playerChoicesContainer != null) {
      foreach (var child in playerChoicesContainer.GetChildren()) {
        if (child is PlayerChoiceButton pcb) {
          buttons.Add(pcb);
        }
      }
    }
    return buttons;
  }
}