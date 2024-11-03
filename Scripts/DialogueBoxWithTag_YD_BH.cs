using Godot;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class DialogueBoxWithTag_YD_BH : VBoxContainer {
  private const float MAX_WIDTH = 800f;
  private string dialogueLineToDisplay = "";
  private int letterIndex = 0;
  private float letterTime = 0.03f;
  private float spaceTime = 0.00006f;
  private float punctuationTime = 0.000002f;
  public DialogueLineLabel dialogueLineLabel;
  public RichTextLabel speakerTag;
  //Timer letterDisplayTimer;
  NinePatchRect backgroundRect;
  public MarginContainer innerMarginContainer;

  public Action FinishedDisplayingDialogueLine;
  public Action DialogueBoxUIWasResized;
  private RichTextLabel waitingIndicator;
  //private Timer blinkTimer;
  private int dotCount = 0;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    // Set layout mode to Anchor (layout_mode = 1)
    LayoutMode = 1;

    // Center horizontally and align to bottom
    AnchorLeft = 0.5f;
    AnchorRight = 0.5f;
    AnchorTop = 1;
    AnchorBottom = 0.98f;
    MouseFilter = MouseFilterEnum.Ignore;

    Alignment = AlignmentMode.Center;

    speakerTag = GetNode<RichTextLabel>("SpeakerTagMarginContainer/SpeakerTagRichTextLabel");
    speakerTag.BbcodeEnabled = true;

    //DO NOT DELETE, THIS IS FOR DEBUGGING THE BACKGROUND RECT
    backgroundRect = GetNode<NinePatchRect>("DialogueBoxUI_YD_BH/NinePatchRect"); // Adjust the path if needed
    if (backgroundRect != null) {
      // Set the alpha to 0.5 (adjust this value to change transparency)
      backgroundRect.Modulate = new Color(backgroundRect.Modulate, 0.9f);
    }

    dialogueLineLabel = GetNode<DialogueLineLabel>("DialogueBoxUI_YD_BH/MarginContainer/DialogueLineLabel");
    //letterDisplayTimer = GetNode<Timer>("DialogueBoxUI_YD_BH/LetterDisplayTimer");
    // letterDisplayTimer.Timeout += OnLetterDisplayTimerTimeout;

    dialogueLineLabel.MouseFilter = MouseFilterEnum.Stop;


    innerMarginContainer = GetNode<MarginContainer>("DialogueBoxUI_YD_BH/MarginContainer");
    innerMarginContainer.AddThemeConstantOverride("margin_left", 40);
    //innerMarginContainer.AddThemeConstantOverride("margin_top", 25);
    innerMarginContainer.AddThemeConstantOverride("margin_right", 40);
    // innerMarginContainer.AddThemeConstantOverride("margin_bottom", 25);

    innerMarginContainer.MouseFilter = MouseFilterEnum.Ignore;

    // Set GlobalMarginContainer to fill the entire DialogueBoxUI
    innerMarginContainer.AnchorRight = 1;
    //innerMarginContainer.AnchorBottom = 1f;
    innerMarginContainer.SizeFlagsHorizontal = SizeFlags.Fill;
    innerMarginContainer.SizeFlagsVertical = SizeFlags.Fill;
    innerMarginContainer.SizeFlagsVertical = SizeFlags.Fill;


    //we need to know if all the chars of the dialogue text have been displayed, so we can clean the current text
    //and display the next one or the player choices. 
    FinishedDisplayingDialogueLine += DialogueManager.Instance.OnTextBoxFinishedDisplayingDialogueLine;
  }

  public async Task DisplayDialogueLine(DialogueObject dialogueObject, string locale) {
    this.dialogueLineToDisplay = GetLocaleDialogue(dialogueObject, locale);
    dialogueLineLabel.Clear();
    dialogueLineLabel.Text = dialogueLineToDisplay;

    // Start with 0 visible characters
    dialogueLineLabel.VisibleCharacters = 0;

    // Show characters one by one with delay
    for (int i = 0; i < dialogueLineToDisplay.Length; i++) {
      dialogueLineLabel.VisibleCharacters = i + 1;

      // Add different delays based on character type
      if (dialogueLineToDisplay[i] == '.' || dialogueLineToDisplay[i] == '!' || dialogueLineToDisplay[i] == '?')
        await Task.Delay((int)(punctuationTime * 1000));
      else if (dialogueLineToDisplay[i] == ' ')
        await Task.Delay((int)(spaceTime * 1000));
      else
        await Task.Delay((int)(letterTime * 1000));
    }

    FinishedDisplayingDialogueLine?.Invoke();
  }

  public void DisplaySpeakerName(string actor) {
    speakerTag.AddThemeFontSizeOverride("normal_font", 50);
    speakerTag.Text = $"[center][b][color=yellow][font size=30]{actor}[/font][/color][/b][/center]";
  }

  public string GetLocaleDialogue(DialogueObject dialogueObj, string locale) {

    string localeCurrentDialogue = locale switch {
      "fr" => dialogueObj.FrenchText,
      "ca" => dialogueObj.CatalanText,
      // Add more cases as needed for other locales
      _ => dialogueObj.DialogueTextDefault  // Default to the default text field
    };

    return localeCurrentDialogue;
  }

  public void StopLetterByLetterDisplay() {
    //  letterDisplayTimer.Stop();
    //for whatever reason, the line [dialogueLineLabel.Text = dialogueLineToDisplay;]
    //won't work if i do not add this line before
    //   dialogueLineLabel.Text = "";
    dialogueLineLabel.Text = dialogueLineToDisplay;
    //   letterIndex = 0;
    FinishedDisplayingDialogueLine?.Invoke();
  }

  //   public void DisplayLetter() {

  //     dialogueLineLabel.AutowrapMode = TextServer.AutowrapMode.Word;

  //     if (letterIndex < dialogueLineToDisplay.Length) {
  //       dialogueLineLabel.AppendText(dialogueLineToDisplay[letterIndex].ToString());
  //       letterIndex++;
  //       //GD.Print($"letterIndex = {letterIndex}\ndialogueLineToDisplay.Length = {dialogueLineToDisplay.Length} ");
  //     } else {
  //       //GD.Print($"dialogueLineLabel.Size: {dialogueLineLabel.Size}");
  //       dialogueLineLabel.Text = "";
  //       dialogueLineLabel.Text = dialogueLineToDisplay;
  //       letterIndex = 0;

  //       FinishedDisplayingDialogueLine?.Invoke();

  //       return;
  //     }

  //     char[] punctuationCharacters = { '!', '.', ',' };

  //     if (letterIndex < dialogueLineToDisplay.Length && punctuationCharacters.Contains(dialogueLineToDisplay[letterIndex]))
  //       letterDisplayTimer.Start(punctuationTime);
  //     else if (letterIndex < dialogueLineToDisplay.Length && dialogueLineToDisplay[letterIndex] == ' ')
  //       letterDisplayTimer.Start(spaceTime);
  //     else
  //       letterDisplayTimer.Start(letterTime);
  //   }

  //   public void OnLetterDisplayTimerTimeout() {
  //     DisplayLetter();
  //   }
}


