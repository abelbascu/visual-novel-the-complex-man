using Godot;
using System;
using System.Linq;

public partial class DialogueBoxUi : MarginContainer {
    private const int MAX_WIDTH = 1500;
    private string dialogueLineToDisplay = "";
    private int letterIndex = 0;
    private float letterTime = 0.005f;
    private float spaceTime = 0.06f;
    private float punctuationTime = 0.2f;
    public Label dialogueLineLabel;
    Timer letterDisplayTimer;

    public Action FinishedDisplaying;
    public Action DialogueBoxUIWasResized;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        dialogueLineLabel = GetNode<Label>("MarginContainer/DialogueLineLabel");
        letterDisplayTimer = GetNode<Timer>("LetterDisplayTimer");
        letterDisplayTimer.Timeout += OnLetterDisplayTimerTimeout;
    }

    public async void DisplayDialogueLine(DialogueObject dialogueObject, string locale) {

        this.dialogueLineToDisplay = GetLocaleString(dialogueObject, locale);
        dialogueLineLabel.Text = this.dialogueLineToDisplay;

        await ToSignal(this, "resized");

        float customMinX = Math.Min(Size.X, MAX_WIDTH);
        CustomMinimumSize = new Vector2(customMinX, CustomMinimumSize.Y);

        if (Size.X > MAX_WIDTH) {
            dialogueLineLabel.AutowrapMode = TextServer.AutowrapMode.Word;
            await ToSignal(this, "resized"); //wait for resizing x of DialogueBoxUI
            await ToSignal(this, "resized"); //wait for resizing y of DialogueBoxUI

            CustomMinimumSize = new Vector2(customMinX, Size.Y);
        }

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
        if (letterIndex < dialogueLineToDisplay.Length) {
            dialogueLineLabel.Text += dialogueLineToDisplay[letterIndex];
            letterIndex++;
            GD.Print($"letterIndex = {letterIndex}\ndialogueLineToDisplay.Length = {dialogueLineToDisplay.Length} ");
        } else {
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


