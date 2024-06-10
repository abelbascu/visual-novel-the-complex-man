using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class DialogueManager : Node {

    public static string languageCode = "en";
    [Export] public int currentConversationID = 2; //set here the conversation you want to load. Conversations in Chatmapper are what we could call chapters.
    [Export] public int currentDialogueID = 1; //set here the starting dialogue of the conversation
    private Dictionary<int, List<DialogueObject>> conversationDialogues;
    //public static string LanguageLocale { get; set; } //set here the language, if language is not recognized it will default to what's defineat in Project Settings > Locale
    private DialogueBoxUi dialogueBoxUI; //the graphical rectangle container to display the text over
    public DialogueObject currentDialogueObject { get; private set; }
    bool isDialogueBeingPrinted = false; //we don't want to print a new dialogue is we are currently displaying another one
    public static Action StartButtonPressed;
    private const int UI_BOTTOM_POSITION = 200; //starting at the bottom of the screen, we subtract this value to position the Y screen position of the dilaogue box


    public override void _Ready() {

        //***** SET LANGUAGE HERE *****
        //we check what language the user has in his Windows OS
        string currentCultureName = System.Globalization.CultureInfo.CurrentCulture.Name;
        string[] parts = currentCultureName.Split('-');
        //languageCode = parts[0];
        TranslationServer.SetLocale(languageCode);
        //for testing purposes, will change the language directly here so we do not have to tinker witn Windows locale settings each time
        //languageCode = "en";
        //TranslationServer.SetLocale(languageCode);

        LoadDialogueObjects("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");
        StartButtonPressed += OnStartButtonPressed;
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/DialogueBoxUI.tscn");
        Node instance = scene.Instantiate();
        AddChild(instance);
        dialogueBoxUI = instance as DialogueBoxUi;
        // position dialogue box centered at the bottom
        Vector2 screenSize = GetTree().Root.Size;
        float xPosition = (screenSize.X - dialogueBoxUI.Size.X) / 3;
        float yPosition = screenSize.Y - UI_BOTTOM_POSITION;
        dialogueBoxUI.Position = new Vector2(xPosition, yPosition);
        //once all chars of the dialogue text are displayed in the container, we can show the next line.
        dialogueBoxUI.FinishedDisplaying += OnTextBoxFinishedDisplayingDialogueLine;
    }

    public void OnStartButtonPressed() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);

        if (currentDialogueObject.DestinationDialogIDs.Count() <= 1)
            ShowDialogue(currentDialogueObject);
        else
            LoadPlayerChoices(currentDialogueObject);
    }

    public void LoadPlayerChoices(DialogueObject dialogueObj) {
        GD.Print("HERE WE NEED TO DISPLAY THE DIFFERENT PLAYER CHOICES IN A VERTICAL BOX");
    }


    private DialogueObject GetDialogueObject(int currentConversationID, int currentDialogueObjectID) {

        // Check if the conversationID exists in the dictionary
        if (conversationDialogues.TryGetValue(currentConversationID, out List<DialogueObject> dialogueList)) {
            // Use LINQ to find the first DialogueObject with the specified ID
            return dialogueList.FirstOrDefault(dialogueObject => dialogueObject.ID == currentDialogueObjectID);
        }
        return null; // Return null if the conversationID is not found in the dictionary
    }

    public void ShowDialogue(DialogueObject currentDialogueObject) {
        if (isDialogueBeingPrinted) //is we are currently printing a dialogue in the DialogueBoxUI, do nothing
            return;
        isDialogueBeingPrinted = true;
        dialogueBoxUI.DisplayDialogueLine(currentDialogueObject, languageCode);
    }

    public void OnTextBoxFinishedDisplayingDialogueLine() {
        isDialogueBeingPrinted = false;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event.IsActionPressed("advance_dialogue"))
            if (!isDialogueBeingPrinted) {
                dialogueBoxUI.dialogueLineLabel.Text = "";
                currentDialogueID = currentDialogueObject.DestinationDialogIDs[0];
                currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);

                if (currentDialogueObject.DestinationDialogIDs.Count() <= 1)
                    ShowDialogue(currentDialogueObject);
                else
                    LoadPlayerChoices(currentDialogueObject);

            }
    }

    private void LoadDialogueObjects(string filePath) {
        try {
            string jsonText = File.ReadAllText(filePath);
            // Deserialize JSON data and extract the required fields
            conversationDialogues = JSON2DialogueObjectParser.ExtractDialogueObjects(jsonText);
        } catch (IOException e) {
            GD.PrintErr("Error loading dialogue data: " + e.Message);
        } catch (JsonException e) {
            GD.PrintErr("Error parsing JSON data: " + e.Message);
        }
    }

}
