using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class DialogueBoxUi : MarginContainer {
    private const float MAX_WIDTH = 800f;
    private string dialogueLineToDisplay = "";
    private int letterIndex = 0;
    private float letterTime = 0.0005f;
    private float spaceTime = 0.0006f;
    private float punctuationTime = 0.002f;
    public Label dialogueLineLabel;
    Timer letterDisplayTimer;

    public Action FinishedDisplaying;
    public Action DialogueBoxUIWasResized;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Show(); //make the dialogue box visible
        dialogueLineLabel = GetNode<Label>("MarginContainer/DialogueLineLabel");
        letterDisplayTimer = GetNode<Timer>("LetterDisplayTimer");
        letterDisplayTimer.Timeout += OnLetterDisplayTimerTimeout;
    }

    public void DisplayDialogueLine(DialogueObject dialogueObject, string locale) {

        this.dialogueLineToDisplay = GetLocaleString(dialogueObject, locale);       
        dialogueLineLabel.Text = "";
        DisplayLetter();
    }

    public string GetLocaleString(DialogueObject dialogueObj, string locale) {

        string localeCurrentDialogue = locale switch {
            "fr" => dialogueObj.FrenchText,
            "ca" => dialogueObj.CatalanText,
            // Add more cases as needed for other locales
            _ => dialogueObj.DialogueText  // Default to the default text field
        };

        return localeCurrentDialogue;
    }

    public void DisplayLetter() {      
         
               if (Size.X > MAX_WIDTH) {            
            dialogueLineLabel.AutowrapMode = TextServer.AutowrapMode.Word;
           // await ToSignal(this, "resized"); //wait for resizing x of DialogueBoxUI
           // await ToSignal(this, "resized"); //wait for resizing y of DialogueBoxUI

           float customMinX = Math.Min(Size.X, MAX_WIDTH);
           CustomMinimumSize = new Vector2(customMinX, Size.Y);
           Size = CustomMinimumSize;
        }
        if (letterIndex < dialogueLineToDisplay.Length) {
            dialogueLineLabel.Text += dialogueLineToDisplay[letterIndex];
            letterIndex++;
            GD.Print($"letterIndex = {letterIndex}\ndialogueLineToDisplay.Length = {dialogueLineToDisplay.Length} ");
        } else {
            GD.Print($"dialogueLineLabel.Size: {dialogueLineLabel.Size}");
            FinishedDisplaying.Invoke();
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


