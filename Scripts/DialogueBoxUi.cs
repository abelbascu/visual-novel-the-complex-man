using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class DialogueBoxUI : MarginContainer {
    private const float MAX_WIDTH = 800f;
    private string dialogueLineToDisplay = "";
    private int letterIndex = 0;
    private float letterTime = 0.00005f;
    private float spaceTime = 0.00006f;
    private float punctuationTime = 0.000002f;
    public DialogueLineLabel dialogueLineLabel;
    Timer letterDisplayTimer;
    NinePatchRect backgroundRect;
    public MarginContainer innerMarginContainer;

    public Action FinishedDisplayingDialogueLine;
    public Action DialogueBoxUIWasResized;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

        MouseFilter = MouseFilterEnum.Ignore;

        backgroundRect = GetNode<NinePatchRect>("NinePatchRect"); // Adjust the path if needed
        if (backgroundRect != null) {
            // Set the alpha to 0.5 (adjust this value to change transparency)
            backgroundRect.Modulate = new Color(backgroundRect.Modulate, 0.9f);
        }

        dialogueLineLabel = GetNode<DialogueLineLabel>("MarginContainer/DialogueLineLabel");
        letterDisplayTimer = GetNode<Timer>("LetterDisplayTimer");
        letterDisplayTimer.Timeout += OnLetterDisplayTimerTimeout;

        dialogueLineLabel.MouseFilter = MouseFilterEnum.Stop;

        //We are doing the comments below in the UIManager as delegating the position to the children gives issues
        //maybe becasue they do not have all the necessary info from the parent?

        // //Set anchors to allow the container to grow
        // AnchorLeft = 0.08f;
        // AnchorRight = 0.925f;
        // AnchorTop = 1;
        // AnchorBottom = 1;

        // //Set offsets to define the initial size
        // OffsetLeft = -800;  // Half of the desired width
        // OffsetRight = 800;  // Half of the desired width
        // OffsetTop = -200;   // Initial height, will grow as needed

        AddThemeConstantOverride("margin_left", 40);
        AddThemeConstantOverride("margin_top", 40);
        AddThemeConstantOverride("margin_right", 40);
        AddThemeConstantOverride("margin_bottom", 40);

        innerMarginContainer = GetNode<MarginContainer>("MarginContainer");
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

    public string GetLocaleDialogue(DialogueObject dialogueObj, string locale) {

        string localeCurrentDialogue = locale switch {
            "f  " => dialogueObj.FrenchText,
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


