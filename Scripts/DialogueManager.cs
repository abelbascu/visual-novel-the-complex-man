using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;

public partial class DialogueManager : Node {

    public static string languageCode = "en";
    [Export] public int currentConversationID = 2; //set here the conversation you want to load. Conversations in Chatmapper are what we could call chapters.
    [Export] public int currentDialogueID = 1; //set here the starting dialogue of the conversation
    private Dictionary<int, List<DialogueObject>> conversationDialogues; //the int refers to the conversation ID, see 'currentConversationID' above.
    //public static string LanguageLocale { get; set; } //set here the language, if language is not recognized it will default to what's defineat in Project Settings > Locale
    private DialogueBoxUI dialogueBoxUI; //the graphical rectangle container to display the text over
    private PlayerChoicesBoxUI playerChoicesBoxUI; //the graphical rectangle VBoxContainer to displayer the branching player choices.
    public DialogueObject currentDialogueObject { get; private set; }
    public bool isDialogueBeingPrinted = false; //we don't want to print a new dialogue is we are currently displaying another one
    public bool isPlayerChoiceBeingPrinted = false;
    public static Action StartButtonPressed;
    private const int UI_BOTTOM_POSITION = 200; //starting at the bottom of the screen, we subtract this value to position the Y screen position of the dilaogue box
    private List<DialogueObject> playerChoicesList;


    public override void _Ready() {
        LoadDialogueObjects("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");
        StartButtonPressed += OnStartButtonPressed;
        playerChoicesList = new();
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

    public void OnStartButtonPressed() {
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);
        DisplayDialogueOrPlayerChoice(currentDialogueObject);
    }

    private DialogueObject GetDialogueObject(int currentConversationID, int currentDialogueObjectID) {
        // Check if the conversationID exists in the dictionary
        if (conversationDialogues.TryGetValue(currentConversationID, out List<DialogueObject> dialogueList)) {
            // Use LINQ to find the first DialogueObject with the specified ID
            return dialogueList.FirstOrDefault(dialogueObject => dialogueObject.ID == currentDialogueObjectID);
        }

        return null; // Return null if the conversationID is not found in the dictionary
    }

    public void DisplayDialogueOrPlayerChoice(DialogueObject dialogObj) {
        // Check if the set contains only one unique "DestinationDialogID" value && that the Actor is NOT the player 
        // && the dialogue is not a Group (groups are empty and only contain multiple DestinationIDs that are player choices)
        if (dialogObj.Actor != "1") {
            DisplayDialogue(dialogObj);
            currentDialogueObject = dialogObj;
        } else if (dialogObj.Actor == "1") {
            AddPlayerChoicesToList(dialogObj.ID, dialogObj);
            DisplayPlayerChoices();
        }
    }

    public void DisplayDialogue(DialogueObject currentDialogueObject) {
        if (isDialogueBeingPrinted) //is we are currently printing a dialogue in the DialogueBoxUI, do nothing
            return;
        isDialogueBeingPrinted = true;
        if (dialogueBoxUI == null) {
            //before adding the dialogue text, we need to create the container box
            DisplayDialogueBoxUI();
        }
        if (dialogueBoxUI != null) 
            dialogueBoxUI.Show();
        if(playerChoicesBoxUI != null)
            playerChoicesBoxUI.Hide();
    
        dialogueBoxUI.DisplayDialogueLine(currentDialogueObject, languageCode);
    }

    //IEnumerable<int> so we can pass a list or a single int when there is only one player choice to add to the playerChoicesList
    public void AddPlayerChoicesToList(IEnumerable<int> destinationDialogIDs, DialogueObject dialogObj) {
        foreach (int dialogID in destinationDialogIDs)
            playerChoicesList.Add(GetDialogueObject(currentConversationID, dialogID));
    }

    //overload method when we only have one single player choice to add to the PlayerChoicesList
    public void AddPlayerChoicesToList(int dialogID, DialogueObject dialogObj) {
        AddPlayerChoicesToList(new[] { dialogID }, dialogObj);
    }

    public void AddPlayerChoicesToList(DialogueObject dialogObject) {
        playerChoicesList.Add(dialogObject);
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
        dialogueBoxUI.FinishedDisplayingDialogueLine += OnTextBoxFinishedDisplayingDialogueLine;
    }

    private void DisplayPlayerChoices() {
        //se want to print what is already in the playerChoiceList but before we need to add new choices coming from the currentDialogueObject    
        if (playerChoicesBoxUI == null) {
            //before adding the dialogue text, we need to create the container box
            DisplayPlayerChoicesBoxUI();
        }
        foreach (var playerChoiceObject in playerChoicesList) {
            isPlayerChoiceBeingPrinted = true;
            if (playerChoicesBoxUI != null) {
                playerChoicesBoxUI.Show();
                dialogueBoxUI.Hide();
            }
            playerChoicesBoxUI.DisplayPlayerChoice(playerChoiceObject, languageCode);
        }
    }

    private void DisplayPlayerChoicesBoxUI() {
        //we add the dialogueUI to the scene and display it 
        //THIS MAY BE WRONG, SPECIALLY IS USER LOADS A PREVIOUS FILE AND IT STARTS WITH A MULTIPLE PLAYER CHOICES UI 
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoicesBoxUI.tscn");
        Node instance = scene.Instantiate();
        AddChild(instance);
        //VBoxContainer playerChoìces = instance as VBoxContainer;
        playerChoicesBoxUI = instance as PlayerChoicesBoxUI;
        // position dialogue box centered at the bottom
        Vector2 screenSize = GetTree().Root.Size;
        float xPosition = (screenSize.X - playerChoicesBoxUI.Size.X) / 3;
        float yPosition = screenSize.Y - UI_BOTTOM_POSITION;
        playerChoicesBoxUI.Position = new Vector2(xPosition, yPosition);
        //once all chars of the dialogue text are displayed in the container, we can show the next line.
        playerChoicesBoxUI.FinishedDisplayingPlayerChoice += OnTextBoxFinishedDisplayingPlayerChoices;
    }

    // TO DO TO DO TO DO TO DO
    private void OnTextBoxFinishedDisplayingPlayerChoices() {
        isPlayerChoiceBeingPrinted = false;
    }

    public void OnTextBoxFinishedDisplayingDialogueLine() {
        isDialogueBeingPrinted = false;
    }

    public void RemoveFromPlayerChoicesToList(DialogueObject dialogObj) {
        playerChoicesList.Remove(dialogObj);
    }

    public void OnDialogueBoxUIPressed() {

        DialogueObject nextDialogObject = new();
        List<int> destinationDialogIDs = new();

        if (!isDialogueBeingPrinted) {
            dialogueBoxUI.dialogueLineLabel.Text = "";
        } else {
            DisplayDialogueSuddenly();
            return;
        }
        // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
        foreach (Dictionary<string, int> dict in currentDialogueObject.OutgoingLinks) {
            if (dict.ContainsKey("DestinationDialogID")) {
                destinationDialogIDs.Add(dict["DestinationDialogID"]);
            }
        }
        //here we get the nextDialogueObject to display, but we still don't know if it's a Narrator, single Player choice, Group Node or No Group node
        if (destinationDialogIDs.Count == 1) {
            var linkDict = currentDialogueObject.OutgoingLinks.FirstOrDefault(dict =>
                dict.ContainsKey("DestinationDialogID") && dict.ContainsKey("DestinationConvoID"));

            if (linkDict != null) {
                int destinationDialogID = linkDict["DestinationDialogID"];
                int destinationConvoID = linkDict["DestinationConvoID"];
                nextDialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);
                DisplayDialogueOrPlayerChoice(nextDialogObject);
                currentDialogueObject = nextDialogObject;
            }
        }
        //it's a Group Node?
        if (nextDialogObject.IsGroup == true) {
            nextDialogObject.IsGroupParent = true;
            AddGroupPlayerChoicesToList(nextDialogObject);
            DisplayPlayerChoices();
        }

        //if the node is a NoGroupParent, meaning that it is not a GROUP node but it has branching childs, 
        //tag it as NoGroupParent and do the same for the children as NoGroupChild
        //NoGroupChild are exclusive, meaning that at the exact moment that a  NoGroup child player choice
        // is clicked by the user, any other child at the same level must be removed from the PlayerChoicesList
        //and those subpaths cannot be traversed anymore unless the player starts a new game. 
        // the dialogObj.Actor != 1 is to ensure that the player answers are triggered by the narrator
        else if (destinationDialogIDs.Count > 1 && nextDialogObject.Actor != "1" && nextDialogObject.IsGroup == false) {
            nextDialogObject.IsNoGroupParent = true;
            AddNoGroupPlayerChoicesToList(nextDialogObject);
            DisplayPlayerChoices();
        }
    }

    public void OnPlayerButtonUIPressed(DialogueObject playerChoiceObject) {
        //we need to remove first the dialogObject on playerChoicesList with the same ID as playerChoiceObject.ID
        playerChoicesList.RemoveAll(dialogObj => dialogObj.ID == playerChoiceObject.ID);

        currentDialogueObject = playerChoiceObject;
        DialogueObject nextDialogObject = new();
        List<int> destinationDialogIDs = new();

        if (isPlayerChoiceBeingPrinted)
            return;

        // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
        foreach (Dictionary<string, int> dict in playerChoiceObject.OutgoingLinks) {
            if (dict.ContainsKey("DestinationDialogID")) {
                destinationDialogIDs.Add(dict["DestinationDialogID"]);
            }
        }
        //here we get the nextDialogueObject to display, but we still don't know if it's a Narrator, single Player choice, Group Node or No Group node
        if (destinationDialogIDs.Count == 1) {
            var linkDict = playerChoiceObject.OutgoingLinks.FirstOrDefault(dict =>
                dict.ContainsKey("DestinationDialogID") && dict.ContainsKey("DestinationConvoID"));

            if (linkDict != null) {
                int destinationDialogID = linkDict["DestinationDialogID"];
                int destinationConvoID = linkDict["DestinationConvoID"];
                nextDialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);

                if (nextDialogObject.IsGroup == false) {
                    DisplayDialogueOrPlayerChoice(nextDialogObject);
                    currentDialogueObject = nextDialogObject;
                } else if (nextDialogObject.IsGroup == true) {
                    AddGroupPlayerChoicesToList(nextDialogObject);
                    DisplayPlayerChoices();
                }
            }
        }

        //if the node is a NoGroupParent, meaning that it is not a GROUP node but it has branching childs, 
        //tag it as NoGroupParent and do the same for the children as NoGroupChild
        //NoGroupChild are exclusive, meaning that at the exact moment that a  NoGroup child player choice
        // is clicked by the user, any other child at the same level must be removed from the PlayerChoicesList
        //and those subpaths cannot be traversed anymore unless the player starts a new game. 
        // the dialogObj.Actor != 1 is to ensure that the player answers are triggered by the narrator
        else if (destinationDialogIDs.Count > 1 && nextDialogObject.Actor != "1" && nextDialogObject.IsGroup == false) {
            nextDialogObject.IsNoGroupParent = true;
            AddNoGroupPlayerChoicesToList(nextDialogObject);
            DisplayPlayerChoices();
        }
    }

    public void AddGroupPlayerChoicesToList(DialogueObject nextDialogueObject) {
        nextDialogueObject.IsGroupParent = true;
        // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
        foreach (Dictionary<string, int> dict in nextDialogueObject.OutgoingLinks) {
            if (dict.TryGetValue("DestinationDialogID", out int destinationDialogID) &&
                dict.TryGetValue("DestinationConvoID", out int destinationConvoID)) {
                DialogueObject dialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);
                dialogObject.IsGroupChild = true;
                AddPlayerChoicesToList(dialogObject);
            }
        }
    }

    public void AddNoGroupPlayerChoicesToList(DialogueObject nextDialogueObject) {
        nextDialogueObject.IsNoGroupParent = true;
        // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
        foreach (Dictionary<string, int> dict in nextDialogueObject.OutgoingLinks) {
            if (dict.TryGetValue("DestinationDialogID", out int destinationDialogID) &&
                dict.TryGetValue("DestinationConvoID", out int destinationConvoID)) {
                DialogueObject dialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);
                dialogObject.IsNoGroupChild = true;
                AddPlayerChoicesToList(dialogObject);
            }
        }
    }

    public void OnPlayerChoicePressed() {
        //always that the user clicks on a player choice, we know it is always Actor = "1" and that must be stored in the PlayerCHoicesList
        //so as it has already been displayed and now we move on to the next dialogue or choices to show, let's remove it first so it's not displayed again
        //when player choices are displayed again 
        if (currentDialogueObject.Actor == "1") {
            RemoveFromPlayerChoicesToList(currentDialogueObject);
        }
    }

    public void DisplayDialogueSuddenly() {
        isDialogueBeingPrinted = false;
        dialogueBoxUI.StopLetterByLetterDisplay();
    }
}




