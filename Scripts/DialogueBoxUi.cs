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

    public Action FinishedDisplayingDialogueLine;
    public Action DialogueBoxUIWasResized;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        dialogueLineLabel = GetNode<DialogueLineLabel>("MarginContainer/DialogueLineLabel");
        letterDisplayTimer = GetNode<Timer>("LetterDisplayTimer");
        letterDisplayTimer.Timeout += OnLetterDisplayTimerTimeout;
        
        // // Make sure the Label can receive input
        // dialogueLineLabel.MouseFilter = Control.MouseFilterEnum.Stop;
        // // Change cursor on hover
        // dialogueLineLabel.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        // dialogueLineLabel.AddThemeConstantOverride("line_spacing", 2); 
    }

    public void DisplayDialogueLine(DialogueObject dialogueObject, string locale) {
        this.dialogueLineToDisplay = GetLocaleDialogue(dialogueObject, locale);
        //dialogueLineLabel.Text = "";
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
            FinishedDisplayingDialogueLine?.Invoke();
            letterIndex = 0;
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


