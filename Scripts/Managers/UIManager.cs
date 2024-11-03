

using Godot;
using System.Threading.Tasks;

public partial class UIManager : Control {

  public static UIManager Instance { get; private set; }
  public PackedScene dialogueBoxUIScene;
  public DialogueBoxWithTag_YD_BH dialogueBoxUI; //the graphical rectangle container to display the text over
  private VBoxContainer dialogueChoicesMarginContainer;
  public PackedScene playerChoicesBoxUIScene;
  public PlayerChoicesBoxU_YD_BottomHorizontal playerChoicesBoxUI;
  public MainMenu mainMenu;
  public InGameMenuButton inGameMenuButton;
  public Control menuOverlay;
  public SaveGameScreen saveGameScreen;
  public InputNameScreen inputNameScreen;
  public SplashScreen splashScreen;
  public Control fadeOverlay;


  private void ConnectAllControlsGuiInput(Control parent) {
    foreach (var node in parent.GetChildren()) {
      if (node is Control control) {
        control.GuiInput += (InputEvent @event) => OnControlGuiInput(@event, control);
        ConnectAllControlsGuiInput(control);
      }
    }
  }

  private void OnControlGuiInput(InputEvent @event, Control clickedControl) {
    if (@event is InputEventMouseButton mouseEvent &&
        mouseEvent.Pressed &&
        mouseEvent.ButtonIndex == MouseButton.Left) {
      HandleClick(clickedControl);
    }
  }

  private void HandleClick(Control clickedControl) {
    GD.Print($"Clicked on: {clickedControl.Name} (Type: {clickedControl.GetType().Name})");
    // Add your custom click handling logic here
  }

  public override void _Ready() {

    // Connect to the GUI input event of all child controls
    //!why is this still here?
    ConnectAllControlsGuiInput(this);

    fadeOverlay = new Control {
      Name = "FadeOverlay",
      MouseFilter = MouseFilterEnum.Ignore,
      AnchorRight = 1,
      AnchorBottom = 1
    };

    // Add a ColorRect as a child of the fadeOverlay
    var colorRect = new ColorRect {
      Color = Colors.Black,
      MouseFilter = MouseFilterEnum.Ignore,
      AnchorRight = 1,
      AnchorBottom = 1
    };
    fadeOverlay.AddChild(colorRect);
    // becasue even if we hide the scen it still processes input behind.


    AddChild(fadeOverlay);
    MoveChild(fadeOverlay, -1);  // Move to top (last child)

    // Initially set the overlay to be transparent
    fadeOverlay.Modulate = new Color(1, 1, 1, 0);

    MouseFilter = MouseFilterEnum.Ignore;

    splashScreen = GetNode<SplashScreen>("SplashScreen");

    mainMenu = GetNode<MainMenu>("MainMenu");
    mainMenu.Hide();

    inGameMenuButton = GetNode<InGameMenuButton>("InGameMenuButton");
    inGameMenuButton.Hide();

    saveGameScreen = GetNode<SaveGameScreen>("SaveGameScreen");
    saveGameScreen.Hide();

    // inputNameScreen = GetNode<InputNameScreen>("InputNameScreen");
    // inputNameScreen.Hide();

    //Set up PlayerChoicesBoxUI
    playerChoicesBoxUIScene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoicesBoxU_YD_BottomHorizontal.tscn");
    playerChoicesBoxUI = playerChoicesBoxUIScene.Instantiate<PlayerChoicesBoxU_YD_BottomHorizontal>();
    AddChild(playerChoicesBoxUI);
    playerChoicesBoxUI.Hide();

    //Set up DialogueBoxUI
    dialogueBoxUIScene = ResourceLoader.Load<PackedScene>("res://Scenes/DialogueBoxWithTag_YD_BH.tscn");
    dialogueBoxUI = dialogueBoxUIScene.Instantiate<DialogueBoxWithTag_YD_BH>();
    AddChild(dialogueBoxUI);
    dialogueBoxUI.Hide();


    // Overlay mask to filter out input when inggame menu is open
    menuOverlay = new Control {
      Name = "MenuOverlay",
      MouseFilter = MouseFilterEnum.Stop,
      Visible = false,
      AnchorRight = 1,
      AnchorBottom = 1,
      GrowHorizontal = GrowDirection.Both,
      GrowVertical = GrowDirection.Both
    };

    CallDeferred(nameof(SetupNodeOrder));
    CallDeferred(nameof(EnsureOverlayOnTop));

    // Set up input handling for the overlay
    menuOverlay.Connect("gui_input", new Callable(this, nameof(OnOverlayGuiInput)));

    AddChild(menuOverlay);
  }


  public async Task FadeInScreenOverlay(float duration = 1.2f) {
    fadeOverlay.Visible = true;
    fadeOverlay.TopLevel = true;
    fadeOverlay.Modulate = new Color(1, 1, 1, 0);  // Start fully opaque
    await UIFadeHelper.FadeInControl(fadeOverlay, duration);
    fadeOverlay.Visible = false;
    fadeOverlay.TopLevel = false;
  }

  public async Task FadeOutScreenOverlay(float duration = 2.2f) {
    fadeOverlay.Visible = true;
    fadeOverlay.TopLevel = true;
    fadeOverlay.Modulate = new Color(1, 1, 1, 1);  // Start fully transparent
    await UIFadeHelper.FadeOutControl(fadeOverlay, duration);
    fadeOverlay.Visible = false;
    // becasue even if we hide the scene it still processes input behind.
    fadeOverlay.SetProcessInput(false);
    //colorRect.SetProcessInput(false);

    fadeOverlay.TopLevel = false;
    //fadeOverlay.Free();

  }

  public void ShowSplashScreen() {
    splashScreen.Show();
  }

  public void HideSplashScreen() {
    splashScreen.Hide();
  }

  // public void HideAllUIElements() {
  //     var uiManager = this;
  //     foreach (Control child in uiManager.GetChildren()) {
  //         if (child != this && child is Control controlNode) {
  //             controlNode.Visible = false;
  //         }
  //     }
  // }

  private void SetupNodeOrder() {
    // Ensure VisualsManager is below UI elements in the scene tree
    var visualManager = GetNode<VisualManager>("../VisualManager");
    GetParent().MoveChild(visualManager, 0);
    // Move this UIManager to be the last child (top layer)
    GetParent().MoveChild(this, GetParent().GetChildCount() - 1);
  }

  private void OnOverlayGuiInput(InputEvent @event) {
    // Consume all input events to ensure no other UI elements are clickable
    // this is for when the ingame menu is open
    GetViewport().SetInputAsHandled();
  }

  private void EnsureOverlayOnTop() {
    // Move the overlay to be the last child (on top of other UI elements)
    MoveChild(menuOverlay, GetChildCount() - 1);
    // Ensure the ingame menu and the ingame menu icon are on top of the overlay
    MoveChild(mainMenu, GetChildCount() - 1);
    //for whatever reason i need to put this line last for the ingame menu button to work
    MoveChild(inGameMenuButton, GetChildCount() - 1);
    //if i put it between the menuOverlay and mainMenu lines, it doesn't work.
    MoveChild(saveGameScreen, GetChildCount() - 1);
  }

  // Call this method whenever you show or hide UI elements
  public void UpdateUILayout() {
    EnsureOverlayOnTop();
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (Instance == null) {
      Instance = this;
    } else {
      Instance.QueueFree();
    }
  }

  public DialogueBoxWithTag_YD_BH GetDialogueBoxUI() {
    return dialogueBoxUI;
  }

  public PlayerChoicesBoxU_YD_BottomHorizontal GetPlayerChoicesBoxUI() {
    return playerChoicesBoxUI;
  }
}

