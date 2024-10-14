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





  public override void _Ready() {
    Show();

    backgroundRect = GetNode<NinePatchRect>("NinePatchRect"); // Adjust the path if needed
    if (backgroundRect != null) {
      // adjust this value to change transparency
      backgroundRect.Modulate = new Color(backgroundRect.Modulate, 0.9f);
    }

    //We are doing the comments below in the UIManager as delegating the position to the children gives issues
    //maybe becasue they do not have all the necessary info from the parent?

    //----------------------FIRST, SET UP THE MAIN MARGIN CONTAINER-------------------------
    //Set anchors to allow the container to grow
    // AnchorLeft = 0.08f;
    // AnchorRight = 0.925f;
    // AnchorTop = 1;
    // AnchorBottom = 1;

    // Reset offsets
    // OffsetLeft = 0;
    // OffsetRight = 0;
    // OffsetTop = -200;  // Adjust this value to set the initial height
    // OffsetBottom = 0;


    CustomMinimumSize = new Vector2(1200, 40);

    SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
    SizeFlagsVertical = SizeFlags.ShrinkEnd;
    GrowVertical = GrowDirection.Begin;
    AnchorLeft = 0.5f;
    AnchorRight = 0.5f;
    // Prevent horizontal growth
    // SizeFlagsHorizontal = SizeFlags.Fill;


    // Add margins to the PlayerChoicesBoxUI
    AddThemeConstantOverride("margin_left", 40);
    AddThemeConstantOverride("margin_right", 40);
    AddThemeConstantOverride("margin_top", 40);
    AddThemeConstantOverride("margin_bottom", 40);

    //----------------------SECOND, CREATE & SET UP AN INNER MARGIN CONTAINER-------------------------

    // Create and add GlobalMarginContainer to control padding better
    globalMarginContainer = new MarginContainer();
    AddChild(globalMarginContainer);

    // Set up GlobalMarginContainer with padding
    globalMarginContainer.AddThemeConstantOverride("margin_left", 40);
    globalMarginContainer.AddThemeConstantOverride("margin_right", 40);
    globalMarginContainer.AddThemeConstantOverride("margin_top", 25);
    globalMarginContainer.AddThemeConstantOverride("margin_bottom", 25);

    // Set GlobalMarginContainer to fill the entire PlayerChoicesBoxUI
    globalMarginContainer.AnchorRight = 1;
    globalMarginContainer.AnchorBottom = 1;
    // globalMarginContainer.SizeFlagsHorizontal = SizeFlags.Fill;
    globalMarginContainer.SizeFlagsVertical = SizeFlags.Fill;
    globalMarginContainer.SizeFlagsVertical = SizeFlags.ShrinkBegin;

    //----------------------THIRD AND LAST, CREATE & SET UP A VBOXCONTAINER -------------------------
    //---------- this VBoxContainer stores the playerChoiceButton, aka the player choices------------

    playerChoicesContainer = new VBoxContainer();
    globalMarginContainer.AddChild(playerChoicesContainer);
    playerChoiceButtonScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoiceButton.tscn");

    // Ensure buttons are aligned to the top
    playerChoicesContainer.Alignment = BoxContainer.AlignmentMode.Begin;
    // playerChoicesContainer.SizeFlagsHorizontal = SizeFlags.Fill;
    playerChoicesContainer.SizeFlagsVertical = SizeFlags.ShrinkEnd;

    //The space between the BoxContainer's elements, in pixels.
    playerChoicesContainer.AddThemeConstantOverride("separation", 20);

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

    await Task.WhenAll(tasks);

    CallDeferred(nameof(Show));
    GD.Print("All player choice buttons are ready");
    GD.Print("DisplayPlayerChoices completed");
  }

  private void CreateAndAddPlayerChoiceButton(DialogueObject playerChoiceObject, string languageCode, TaskCompletionSource<bool> tcs) {
    string playerChoiceToDisplay = GetLocalePlayerChoice(playerChoiceObject, languageCode);
    PlayerChoiceButton playerChoiceButton = playerChoiceButtonScene.Instantiate<PlayerChoiceButton>();

    buttonReadyTasks[playerChoiceButton.GetInstanceId()] = tcs;
    var connectionResult = playerChoiceButton.Connect("ready", Callable.From(() => OnPlayerChoiceButtonReady(playerChoiceButton)));
    GD.Print($"Connection result: {connectionResult}");

    playerChoiceButton.SetDialogueObject(playerChoiceObject);
    playerChoicesContainer.AddChild(playerChoiceButton);
    SizeChanged += playerChoiceButton.OnParentSizeChanged;
    playerChoiceButton.SetText(playerChoiceToDisplay);
  }

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
      SizeChanged -= button.OnParentSizeChanged;
      button.Disconnect("ready", new Callable(this, nameof(OnPlayerChoiceButtonReady)));
      buttonReadyActions.Remove(button.GetInstanceId());
      playerChoicesContainer.RemoveChild(button);
      button.QueueFree();
    }
  }

  public async Task RemoveAllPlayerChoiceButtons() {
    foreach (Node child in playerChoicesContainer.GetChildren()) {
      if (child is PlayerChoiceButton button) {
        SizeChanged -= button.OnParentSizeChanged;
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