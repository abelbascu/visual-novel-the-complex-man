using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class DialogueBoxAndSpeakerTag : VBoxContainer {
    private const float MAX_WIDTH = 800f;
    private string dialogueLineToDisplay = "";
    private int letterIndex = 0;
    private float letterTime = 0.00005f;
    private float spaceTime = 0.00006f;
    private float punctuationTime = 0.000002f;
    public DialogueLineLabel dialogueLineLabel;
    public RichTextLabel speakerTag;
    Timer letterDisplayTimer;
    NinePatchRect backgroundRect;
    public MarginContainer innerMarginContainer;

    public Action FinishedDisplayingDialogueLine;
    public Action DialogueBoxUIWasResized;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Ignore;

        speakerTag = GetNode<RichTextLabel>("SpeakerTagMarginContainer/SpeakerTagRichTextLabel");
        speakerTag.BbcodeEnabled = true;

        backgroundRect = GetNode<NinePatchRect>("DialogueBoxUI/NinePatchRect"); // Adjust the path if needed
        if (backgroundRect != null) {
            // Set the alpha to 0.5 (adjust this value to change transparency)
            backgroundRect.Modulate = new Color(backgroundRect.Modulate, 0.9f);
        }

        dialogueLineLabel = GetNode<DialogueLineLabel>("DialogueBoxUI/MarginContainer/DialogueLineLabel");
        letterDisplayTimer = GetNode<Timer>("DialogueBoxUI/LetterDisplayTimer");
        letterDisplayTimer.Timeout += OnLetterDisplayTimerTimeout;

        dialogueLineLabel.MouseFilter = MouseFilterEnum.Stop;

        AddThemeConstantOverride("margin_left", 40);
        AddThemeConstantOverride("margin_top", 40);
        AddThemeConstantOverride("margin_right", 40);
        AddThemeConstantOverride("margin_bottom", 40);

        innerMarginContainer = GetNode<MarginContainer>("DialogueBoxUI/MarginContainer");
        innerMarginContainer.AddThemeConstantOverride("margin_left", 40);
        innerMarginContainer.AddThemeConstantOverride("margin_top", 25);
        innerMarginContainer.AddThemeConstantOverride("margin_right", 40);
        innerMarginContainer.AddThemeConstantOverride("margin_bottom", 25);

        innerMarginContainer.MouseFilter = MouseFilterEnum.Ignore;

        // Set GlobalMarginContainer to fill the entire DialogueBoxUI
        innerMarginContainer.AnchorRight = 1;
        innerMarginContainer.AnchorBottom = 1;
        innerMarginContainer.SizeFlagsHorizontal = SizeFlags.Fill;
        innerMarginContainer.SizeFlagsVertical = SizeFlags.Fill;
        innerMarginContainer.SizeFlagsVertical = SizeFlags.ShrinkBegin;

        //we need to know if all the chars of the dialogue text have been displayed, so we can clean the current text
        //and display the next one or the player choices. 
        FinishedDisplayingDialogueLine += DialogueManager.Instance.OnTextBoxFinishedDisplayingDialogueLine;
    }

    public void DisplayDialogueLine(DialogueObject dialogueObject, string locale) {
        this.dialogueLineToDisplay = GetLocaleDialogue(dialogueObject, locale);
        // dialogueLineLabel.Text = "";
        dialogueLineLabel.Clear();
        DisplayLetter();
    }

    public void DisplaySpeakerName(string actor)
    {
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
        letterDisplayTimer.Stop();
        //for whatever reason, the line [dialogueLineLabel.Text = dialogueLineToDisplay;]
        //won't work if i do not add this line before
        dialogueLineLabel.Text = "";
        dialogueLineLabel.Text = dialogueLineToDisplay;
        letterIndex = 0;
        FinishedDisplayingDialogueLine?.Invoke();
    }

    public void DisplayLetter() {

        dialogueLineLabel.AutowrapMode = TextServer.AutowrapMode.Word;

        if (letterIndex < dialogueLineToDisplay.Length) {
            dialogueLineLabel.AppendText(dialogueLineToDisplay[letterIndex].ToString());
            letterIndex++;
            //GD.Print($"letterIndex = {letterIndex}\ndialogueLineToDisplay.Length = {dialogueLineToDisplay.Length} ");
        } else {
            //GD.Print($"dialogueLineLabel.Size: {dialogueLineLabel.Size}");
            dialogueLineLabel.Text = "";
            dialogueLineLabel.Text = dialogueLineToDisplay;
            letterIndex = 0;
            
            FinishedDisplayingDialogueLine?.Invoke();

            return;
        }

        char[] punctuationCharacters = { '!', '.', ',' };

        if (letterIndex < dialogueLineToDisplay.Length && punctuationCharacters.Contains(dialogueLineToDisplay[letterIndex]))
            letterDisplayTimer.Start(punctuationTime);
        else if (letterIndex < dialogueLineToDisplay.Length && dialogueLineToDisplay[letterIndex] == ' ')
            letterDisplayTimer.Start(spaceTime);
        else
            letterDisplayTimer.Start(letterTime);
    }

    public void OnLetterDisplayTimerTimeout() {
        DisplayLetter();
    }
}


