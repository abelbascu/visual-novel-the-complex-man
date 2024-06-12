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
    private Dictionary<int, List<DialogueObject>> conversationDialogues; //the int refers to the conversation ID, see 'currentConversationID' above.
    //public static string LanguageLocale { get; set; } //set here the language, if language is not recognized it will default to what's defineat in Project Settings > Locale
    private DialogueBoxUI dialogueBoxUI; //the graphical rectangle container to display the text over
    private PlayerChoicesBoxUI playerChoicesBoxUI; //the graphical rectangle VBoxContainer to displayer the branching player choices.
    public DialogueObject currentDialogueObject { get; private set; }
    bool isDialogueBeingPrinted = false; //we don't want to print a new dialogue is we are currently displaying another one
    public static Action StartButtonPressed;
    private const int UI_BOTTOM_POSITION = 200; //starting at the bottom of the screen, we subtract this value to position the Y screen position of the dilaogue box
    private List<DialogueObject> playerChoicesList;


    public override void _Ready() {
        LoadDialogueObjects("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");
        StartButtonPressed += OnStartButtonPressed;
        playerChoicesList = new();
    }

    public void OnStartButtonPressed() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster

        currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);

        if (currentDialogueObject.DestinationDialogIDs.Count() <= 1)
            DisplayDialogue(currentDialogueObject);
        else
            DisplayPlayerChoices(playerChoicesList);
    }

    private DialogueObject GetDialogueObject(int currentConversationID, int currentDialogueObjectID) {
        // Check if the conversationID exists in the dictionary
        if (conversationDialogues.TryGetValue(currentConversationID, out List<DialogueObject> dialogueList)) {
            // Use LINQ to find the first DialogueObject with the specified ID
            return dialogueList.FirstOrDefault(dialogueObject => dialogueObject.ID == currentDialogueObjectID);
        }
        return null; // Return null if the conversationID is not found in the dictionary
    }

    public void DisplayDialogue(DialogueObject currentDialogueObject) {
        if (isDialogueBeingPrinted) //is we are currently printing a dialogue in the DialogueBoxUI, do nothing
            return;
        isDialogueBeingPrinted = true;

        if (dialogueBoxUI == null) {
            //before adding the dialogue text, we need to create the container box
            DisplayDialogueBoxUI();
        }

        dialogueBoxUI.DisplayDialogueLine(currentDialogueObject, languageCode);
    }

    private void DisplayDialogueBoxUI() {
        //we add the dialogueUI to the scene and display it 
        //THIS MAY BE WRONG, SPECIALLY IS USER LOADS A PREVIOUS FILE AND IT STARTS WITH A MULTIPLE PLAYER CHOICES UI 
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/DialogueBoxUI.tscn");
        Node instance = scene.Instantiate();
        AddChild(instance);
        dialogueBoxUI = instance as DialogueBoxUI;
        // position dialogue box centered at the bottom
        Vector2 screenSize = GetTree().Root.Size;
        float xPosition = (screenSize.X - dialogueBoxUI.Size.X) / 3;
        float yPosition = screenSize.Y - UI_BOTTOM_POSITION;
        dialogueBoxUI.Position = new Vector2(xPosition, yPosition);
        //once all chars of the dialogue text are displayed in the container, we can show the next line.
        dialogueBoxUI.FinishedDisplaying += OnTextBoxFinishedDisplayingDialogueLine;
    }

    private void DisplayplayerChoicesBoxUI() {
        //we add the dialogueUI to the scene and display it 
        //THIS MAY BE WRONG, SPECIALLY IS USER LOADS A PREVIOUS FILE AND IT STARTS WITH A MULTIPLE PLAYER CHOICES UI 
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoicesBoxUI.tscn");
        Node instance = scene.Instantiate();
        AddChild(instance);
        //VBoxContainer playerChoìces = instance as VBoxContainer;
        playerChoicesBoxUI = instance as PlayerChoicesBoxUI; 
        // position dialogue box centered at the bottom
        Vector2 screenSize = GetTree().Root.Size;
        //float xPosition = (screenSize.X - playerChoicesBoxUI.Size.X) / 3;
        //float yPosition = screenSize.Y - UI_BOTTOM_POSITION;
        //playerChoicesBoxUI.Position = new Vector2(xPosition, yPosition);
        //once all chars of the dialogue text are displayed in the container, we can show the next line.
        playerChoicesBoxUI.FinishedDisplaying += OnTextBoxFinishedDisplayingPlayerChoices;
    }

    // TO DO TO DO TO DO TO DO
    private void OnTextBoxFinishedDisplayingPlayerChoices() {
        throw new NotImplementedException();
    }

    public void OnTextBoxFinishedDisplayingDialogueLine() {
        isDialogueBeingPrinted = false;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event.IsActionPressed("advance_dialogue"))
            if (!isDialogueBeingPrinted) {
                dialogueBoxUI.dialogueLineLabel.Text = "";
                //we update the ID to the next dialogue to show
                if (currentDialogueObject.DestinationDialogIDs.Count() <= 1) {
                    currentDialogueID = currentDialogueObject.DestinationDialogIDs[0];
                    currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);
                    DisplayDialogue(currentDialogueObject);
                }
                //if more than one destination, it's a multiple player choice
                if (currentDialogueObject.DestinationDialogIDs.Count() > 1) {
                    GetPlayerChoices(currentDialogueObject);
                    DisplayPlayerChoices(playerChoicesList);
                }
            }
    }

    public void GetPlayerChoices(DialogueObject currentDialogueObject) {
        playerChoicesList.Clear();
        foreach (int destinationDialogID in currentDialogueObject.DestinationDialogIDs)
            playerChoicesList.Add(GetDialogueObject(currentConversationID, destinationDialogID));
    }

    private void DisplayPlayerChoices(List<DialogueObject> playerChoicesList) {

        if (playerChoicesBoxUI == null) {
            //before adding the dialogue text, we need to create the container box
            DisplayplayerChoicesBoxUI();
        }
        foreach (var playerChoiceObject in playerChoicesList) {
            playerChoicesBoxUI.DisplayPlayerChoice(playerChoiceObject, languageCode);
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
