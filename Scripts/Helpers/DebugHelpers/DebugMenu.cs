using Godot;
using System;

public partial class DebugMenu : Control {
  private CheckBox showContainersCheck;
  private SpinBox dialogueIdSpin;
  private SpinBox convoIdSpin;
  private OptionButton languageSelect;
  private Button jumpButton;
  private LineEdit dialogueIdLineEdit;
  private LineEdit convoIdLineEdit;

  public override void _Ready() {
#if DEBUG
    // Main container with padding
    var mainContainer = new MarginContainer();
    mainContainer.Name = "MainContainer";
    mainContainer.AddThemeConstantOverride("margin_left", 20);
    mainContainer.AddThemeConstantOverride("margin_right", 20);
    mainContainer.AddThemeConstantOverride("margin_top", 20);
    mainContainer.AddThemeConstantOverride("margin_bottom", 20);
    AddChild(mainContainer);

    // Background panel
    var panel = new PanelContainer();
    panel.Name = "Panel";
    mainContainer.AddChild(panel);

    // Main vertical layout
    var vbox = new VBoxContainer();
    vbox.AddThemeConstantOverride("separation", 15);
    panel.AddChild(vbox);

    // Header
    var headerContainer = new MarginContainer();
    headerContainer.AddThemeConstantOverride("margin_bottom", 10);
    var headerLabel = new Label { Text = "Debug Tools" };
    headerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
    headerContainer.AddChild(headerLabel);
    vbox.AddChild(headerContainer);

    // Containers debug section
    var debugSection = new VBoxContainer();
    debugSection.AddThemeConstantOverride("separation", 5);
    showContainersCheck = new CheckBox { Text = "Show Dialogue Containers" };
    showContainersCheck.Toggled += OnShowContainersToggled;
    debugSection.AddChild(showContainersCheck);
    vbox.AddChild(debugSection);

    // Dialogue jump section
    var jumpSection = new VBoxContainer();
    jumpSection.AddThemeConstantOverride("separation", 10);

    var jumpGrid = new GridContainer();
    jumpGrid.Columns = 2;
    jumpGrid.AddThemeConstantOverride("h_separation", 10);
    jumpGrid.AddThemeConstantOverride("v_separation", 10);

    jumpGrid.AddChild(new Label { Text = "Dialogue ID:" });
    dialogueIdSpin = new SpinBox { MinValue = 1, Value = 1, CustomMinimumSize = new Vector2(100, 30) };
    // Style the SpinBox
    dialogueIdSpin.AddThemeConstantOverride("arrow_margin", 4); // Add spacing between arrows
    dialogueIdSpin.AddThemeConstantOverride("button_width", 20); // Make arrows wider
    dialogueIdLineEdit = dialogueIdSpin.GetNode<LineEdit>("LineEdit");

    jumpGrid.AddChild(dialogueIdSpin);

    jumpGrid.AddChild(new Label { Text = "Convo ID:" });
    convoIdSpin = new SpinBox { MinValue = 1, Value = 1, CustomMinimumSize = new Vector2(100, 30) };
    convoIdSpin.AddThemeConstantOverride("arrow_margin", 4);
    convoIdSpin.AddThemeConstantOverride("button_width", 20);
    convoIdLineEdit = convoIdSpin.GetNode<LineEdit>("LineEdit");

    // For the grid layout, add some spacing
    jumpGrid.AddThemeConstantOverride("h_separation", 10);
    jumpGrid.AddThemeConstantOverride("v_separation", 10);
    jumpGrid.AddChild(convoIdSpin);

    jumpSection.AddChild(jumpGrid);

    jumpButton = new Button { Text = "Jump to Dialogue" };
    jumpButton.Pressed += OnJumpPressed;
    jumpSection.AddChild(jumpButton);
    vbox.AddChild(jumpSection);

    // Language section
    var langSection = new VBoxContainer();
    langSection.AddThemeConstantOverride("separation", 5);
    langSection.AddChild(new Label { Text = "Language:" });

    languageSelect = new OptionButton();
    languageSelect.AddItem("English", 0);
    languageSelect.AddItem("French", 1);
    languageSelect.AddItem("Catalan", 2);
    languageSelect.ItemSelected += OnLanguageSelected;
    langSection.AddChild(languageSelect);
    vbox.AddChild(langSection);

    // Set initial position
    Position = new Vector2(50, 50);
    CustomMinimumSize = new Vector2(300, 0);

    Hide();
#endif
  }

  //   public override void _Input(InputEvent @event) {
  // #if DEBUG
  //     if (@event is InputEventKey eventKey) {
  //       if (eventKey.Keycode == Key.F12 && eventKey.Pressed && !eventKey.Echo) {
  //         Visible = !Visible;
  //         if (Visible) {
  //           SwitchToSystemCursor();
  //         } else {
  //           SwitchToGameCursor();
  //         }
  //       }
  //     }
  // #endif
  //   }

  private bool isDragging = false;
  private Vector2 dragOffset;

  // public override void _GuiInput(InputEvent @event) {
  //   if (@event is InputEventMouseButton mouseEvent) {
  //     if (mouseEvent.ButtonIndex == MouseButton.Left) {
  //       isDragging = mouseEvent.Pressed;
  //       if (isDragging) {
  //         dragOffset = GetGlobalMousePosition() - Position;
  //       }
  //     }
  //   } else if (@event is InputEventMouseMotion motionEvent && isDragging) {
  //     Position = GetGlobalMousePosition() - dragOffset;
  //   }
  // }

  private void OnShowContainersToggled(bool toggled) {
    PlayerChoicesBoxU_YD_BottomHorizontal.DEBUG_SHOW_CONTAINERS = toggled;
  }

  public void OnJumpPressed() {
    var dialogueManager = DialogueManager.Instance;
    dialogueManager.currentDialogueID = (int)dialogueIdSpin.Value;
    dialogueManager.currentConversationID = (int)convoIdSpin.Value;
    var dialogObj = dialogueManager.GetDialogueObject(
        dialogueManager.currentConversationID,
        dialogueManager.currentDialogueID
    );
    if (dialogObj != null) {
      dialogueManager.DisplayDialogueOrPlayerChoice(dialogObj);
    }
  }

  public void OnLanguageSelected(long index) {
    string locale = index switch {
      1 => "fr",
      2 => "ca",
      _ => "en"
    };
    TranslationServer.SetLocale(locale);
  }

  private void SwitchToSystemCursor() {
    Input.SetCustomMouseCursor(null);
  }

  private void SwitchToGameCursor() {
    var cursorScaler = GetNode<DynamicCursorScaler>("/root/DynamicCursorScaler");
    if (cursorScaler != null) {
      cursorScaler.UpdateCursorScale();
    }
  }

}

