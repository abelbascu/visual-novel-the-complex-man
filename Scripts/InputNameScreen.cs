using Godot;
using System;
using static GameStateMachine;
using System.Threading.Tasks;

public partial class InputNameScreen : Control {
  private RichTextLabel questionLabel;
  private InteractableUILineEdit nameInput;
  private string username;
  private MarginContainer marginContainer;
  private bool isNameConfirmed = false;
  private string inputYourNameTitleTRANSLATE = "INPUT_YOUR_NAME_TOP_TITLE"; //You can't enter the tavern without telling your name, traveller...
  private string inputYourNameCancelButtonText_TRANSLATE = "INPUT_YOUR_NAME_CANCEL_BUTTON_TEXT"; //"No, {0} is not my name!\nLet me change it!"
  private string inputYourNameOKButtonText_TRANSLATE = "INPUT_YOUR_NAME_OK_BUTTON_TEXT"; //Yes, {0} is my name!.\nLet me enter the tavern!"
  private string inputYourNameConfirmNameText_TRANSLATE = "INPUT_YOUR_NAME_CONFIRM_NAME"; //[center]Are you sure that {username} is your final name?[/center]\n[center]You won't be able to change it during this current play![/center]"
  private RichTextLabel enterNameBeforeConfirmingText;
  private InteractableUITextureButton acceptNameButton;
  private ColorRect acceptButtonBackground;
  private Panel ConfirmNameDialogPanel;
  private InteractableUIButton YesAcceptNameButton;
  private InteractableUIButton NoAcceptNameButton;
  private RichTextLabel AreYouSureTextLabel;

  [Export] public float FadeDuration { get; set; } = 0.5f;

  private void SetupConfirmationDialogTheme() {
    YesAcceptNameButton.Pressed += () => _ = OnConfirmName();
    NoAcceptNameButton.Pressed += OnCancelConfirmation;
    ConfirmNameDialogPanel.Visible = false; // Ensure the dialog is initially hidden
  }


  public override void _Ready() {

    marginContainer = GetNode<MarginContainer>("MarginContainer");
    var vBoxContainer = marginContainer.GetNode<VBoxContainer>("MarginContainer1/VBoxContainer");

    nameInput = vBoxContainer.GetNode<InteractableUILineEdit>("HBoxContainer/LineEdit");
    nameInput.CaretBlink = true;
    nameInput.CaretForceDisplayed = true;

    acceptNameButton = vBoxContainer.GetNode<InteractableUITextureButton>("HBoxContainer/Control/InteractableUITextureButton");

    ConfirmNameDialogPanel = GetNode<Panel>("ConfirmNameDialogPanel");
    YesAcceptNameButton = GetNode<InteractableUIButton>("ConfirmNameDialogPanel/VBoxContainer/YesNoAcceptNameButtonsHBoxContainer/YesAcceptNameButton");
    NoAcceptNameButton = GetNode<InteractableUIButton>("ConfirmNameDialogPanel/VBoxContainer/YesNoAcceptNameButtonsHBoxContainer/NoAcceptNameButton");;
    AreYouSureTextLabel = GetNode<RichTextLabel>("ConfirmNameDialogPanel/VBoxContainer/MarginContainer/AreYouSureTextLabel");


    enterNameBeforeConfirmingText = vBoxContainer.GetNode<RichTextLabel>("RichTextLabel");

    acceptNameButton.CustomMinimumSize = new Vector2(100, 100);
    acceptNameButton.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
    acceptNameButton.SizeFlagsVertical = SizeFlags.ShrinkCenter;

    vBoxContainer.AddChild(acceptNameButton);

    acceptButtonBackground = GetNode<ColorRect>("MarginContainer/MarginContainer1/VBoxContainer/HBoxContainer/Control/AcceptButtonBackground");

    ListenForNameConfirmation();
    SetupConfirmationDialogTheme();

    this.Visible = false;
    nameInput.SetProcessInput(false);

    //seems a godot bug, i needed to put the key in the Godot Editor
    // string titleText =  TranslationServer.Translate(inputYourNameTitleTRANSLATE);
    // richText.Text = titleText; 
  }

  public async Task Show() {
    base.Show();
    ResetNameInputScreen();
    nameInput.SetProcessInput(true);
    CallDeferred(nameof(SetInitialFocus));
    await FadeIn();
  }

  private async Task FadeIn() {
    await UIFadeHelper.FadeInControl(this, 1.3f);
  }

  private void ResetNameInputScreen() {
    isNameConfirmed = false;
    //process again
    //confirmationDialog.ProcessMode = Node.ProcessModeEnum.Inherit;
    UIInputHelper.EnableParentChildrenInput(this);
    nameInput.Text = "";
    nameInput.Editable = true;
    nameInput.FocusMode = Control.FocusModeEnum.All;
    ConfirmNameDialogPanel.ProcessMode = Node.ProcessModeEnum.Inherit;
    nameInput.ProcessMode = ConfirmNameDialogPanel.ProcessMode = Node.ProcessModeEnum.Inherit;
  }

  private void SetInitialFocus() {
    nameInput.GrabFocus();
  }

  private void ListenForNameConfirmation() {
    // nameInput.PlaceholderText = "Enter your name";
    //player confirms by hitting the OK button on the LineEdit textobx or hitting the Enter key
    nameInput.TextSubmitted += OnNameSubmitted;
    acceptNameButton.Pressed += OnAcceptButtonPressed;
    nameInput.FocusEntered += () => GD.Print("LineEdit focus entered");
    nameInput.FocusExited += () => GD.Print("LineEdit focus exited");
    nameInput.InteractRequested += () => OnNameInputInteract();
    //nameInput.GuiInput += OnNameInputGuiInput;
  }

  private void OnNameInputInteract() {
    ShowConfirmationDialog();
  }

  private void OnAcceptButtonPressed() {
    ShowConfirmationDialog();
  }


  private void OnNameSubmitted(string text) {
    ShowConfirmationDialog();
  }


  private void ShowConfirmationDialog() {
    username = nameInput.Text;
    if (!string.IsNullOrWhiteSpace(username)) {

      NoAcceptNameButton.Text = string.Format(Tr(inputYourNameCancelButtonText_TRANSLATE), username);
      YesAcceptNameButton.Text = string.Format(Tr(inputYourNameOKButtonText_TRANSLATE), username);
      AreYouSureTextLabel.Text = string.Format(Tr(inputYourNameConfirmNameText_TRANSLATE), username);
      ConfirmNameDialogPanel.Visible = true;

    } else {
      enterNameBeforeConfirmingText.Text = Tr("PLEASE_ENTER_NAME_BEFORE_CONFIRMING");
      GetTree().CreateTimer(3.0f).Timeout += ClearErrorMessage;
    }
  }

  private void ClearErrorMessage() {
    enterNameBeforeConfirmingText.Text = Tr(inputYourNameTitleTRANSLATE);
  }

  private async Task OnConfirmName() {
    nameInput.Editable = false;
    nameInput.FocusMode = Control.FocusModeEnum.None;
    nameInput.ProcessMode = Node.ProcessModeEnum.Disabled;
    ConfirmNameDialogPanel.ProcessMode = Node.ProcessModeEnum.Disabled;

    if (isNameConfirmed) return; // Prevent multiple confirmations
    isNameConfirmed = true;
    GD.Print($"Name confirmed: {username}");
    ConfirmNameDialogPanel.ProcessMode = Node.ProcessModeEnum.Disabled;
    nameInput.ProcessMode = ConfirmNameDialogPanel.ProcessMode = Node.ProcessModeEnum.Disabled;
    UIInputHelper.DisableParentChildrenInput(this);
    // Hide the confirmation dialog
    ConfirmNameDialogPanel.Visible = false;
    await UIFadeHelper.FadeOutControl(this, 1.3f);
    GameStateManager.Instance.Fire(Trigger.DISPLAY_NEW_GAME_DIALOGUES);
    Visible = false;
  }

  private void OnCancelConfirmation() {
    ConfirmNameDialogPanel.Visible = false; // Hide the dialog when cancelled
    nameInput.GrabFocus();
  }


  public StyleBoxFlat GetHighlightedLineEditStyle() {
    return new StyleBoxFlat {
      BorderColor = new Color(1, 1, 1),
      BorderWidthBottom = 4,
      BorderWidthLeft = 4,
      BorderWidthRight = 4,
      BorderWidthTop = 4,
      DrawCenter = false // This ensures only the border is drawn
    };
  }

  public StyleBoxFlat GetNormalLineEditStyle() {
    return new StyleBoxFlat {
      BorderColor = new Color(0.5f, 0.5f, 0.5f),
      BorderWidthBottom = 2,
      BorderWidthLeft = 2,
      BorderWidthRight = 2,
      BorderWidthTop = 2,
      DrawCenter = false
    };
  }

  public Color GetHighlightedButtonColor() {
    return Colors.Yellow;
  }

  public Color GetNormalButtonColor() {
    return new Color(0, 0, 0, 0); // Fully transparent
  }
}




