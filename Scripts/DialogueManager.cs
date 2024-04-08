using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class DialogueManager : Node {

    private int currentConversationID = 2; //set here the conversation you want to load
    public int currentDialogueID = 1; //set here the starting dialogue of the conversation
    private Dictionary<int, List<DialogueObject>> conversationDialogues;
    public static string LanguageLocale { get; set; } = "ca";
    private DialogueBoxUi dialogueBoxUI;
    public DialogueObject currentDialogueObject { get; private set; }

    int currentLineIndex = 0;
    bool isDialogueBeingPrinted = false;
    bool canAdvanceLine = false;
    public static Action StartButtonPressed;
    

    public override void _Ready() {

        LoadDialogueObjects("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");
        StartButtonPressed += OnStartButtonPressed;   
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/DialogueBoxUI.tscn");
        Node instance = scene.Instantiate();
        AddChild(instance);
        dialogueBoxUI = instance as DialogueBoxUi; 
        //once all chars of the dialogue text are displayed in the container, we can show the next line.
        dialogueBoxUI.FinishedDisplaying += OnTextBoxFinishedDisplayingDialogueLine; 
    }

    public void OnStartButtonPressed() {   
        //TO DO: pass a player profile object with bools of his previous choices to test advanced parts faster
        currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);
        StartDialogue(currentDialogueObject);
    }


    private DialogueObject GetDialogueObject(int currentConversationID, int currentDialogueObjectID) {

        // Check if the conversationID exists in the dictionary
        if (conversationDialogues.TryGetValue(currentConversationID, out List<DialogueObject> dialogueList)) {
            // Use LINQ to find the first DialogueObject with the specified ID
            return dialogueList.FirstOrDefault(dialogueObject => dialogueObject.ID == currentDialogueObjectID);
        }
        return null; // Return null if the conversationID is not found in the dictionary
    }

    public void StartDialogue(DialogueObject currentDialogueObject) {
        if (isDialogueBeingPrinted) //is we are currently printing a dialogue in the DialogueBoxUI, do nothing
            return;
        isDialogueBeingPrinted = true;
        canAdvanceLine = false;
        dialogueBoxUI.DisplayDialogueLine(currentDialogueObject, LanguageLocale);
    }

    public void OnTextBoxFinishedDisplayingDialogueLine() {
        canAdvanceLine = true;
        isDialogueBeingPrinted = false;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event.IsActionPressed("advance_dialogue"))
            if (canAdvanceLine && !isDialogueBeingPrinted) {
                dialogueBoxUI.dialogueLineLabel.Text = "";
                currentDialogueID = currentDialogueObject.DestinationDialogID[0];
                currentDialogueObject = GetDialogueObject(currentConversationID, currentDialogueID);
                StartDialogue(currentDialogueObject);
            }
    }

    private void LoadDialogueObjects(string filePath) {
        try {
            string jsonText = File.ReadAllText(filePath);
            // Deserialize JSON data and extract the required fields
            conversationDialogues = ExtractDialogueObjects(jsonText);
        } catch (IOException e) {
            GD.PrintErr("Error loading dialogue data: " + e.Message);
        } catch (JsonException e) {
            GD.PrintErr("Error parsing JSON data: " + e.Message);
        }
    }

    private Dictionary<int, List<DialogueObject>> ExtractDialogueObjects(string jsonText) {
        // Initialize a dictionary to store dialogue rows by conversation ID
        var conversationObjectsDB = new Dictionary<int, List<DialogueObject>>();

        try {
            // Deserialize JSON data into a JsonElement
            JsonElement jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonText);

            // Check if the "Assets" property exists
            if (jsonObject.TryGetProperty("Assets", out JsonElement assetsElement)) {
                // Check if the "Conversations" property exists within "Assets"
                if (assetsElement.TryGetProperty("Conversations", out JsonElement conversationsElement)) {
                    // Iterate over each conversation in "Conversations"
                    foreach (var conversation in conversationsElement.EnumerateArray()) {
                        // Access conversation properties
                        int conversationID = conversation.GetProperty("ID").GetInt32();
                        var dialogueObjects = new List<DialogueObject>();

                        // Access "DialogNodes" property within the conversation
                        if (conversation.TryGetProperty("DialogNodes", out JsonElement dialogNodesElement)) {
                            // Iterate over each dialog node in "DialogNodes"
                            foreach (var dialogNode in dialogNodesElement.EnumerateArray()) {
                                // Extract "ID" from the dialog node
                                int dialogID = dialogNode.GetProperty("ID").GetInt32();
                                string dialogueText = "";
                                string catLocaleText = "";
                                string frLocaleText = "";


                                // Attempt to access "Fields" property
                                if (dialogNode.TryGetProperty("Fields", out JsonElement fieldsElement)) {
                                    // Attempt to access "Dialogue Text" property within "Fields"
                                    if (fieldsElement.TryGetProperty("Dialogue Text", out JsonElement dialogueTextElement)) {
                                        // Get the string value of "Dialogue Text"
                                        dialogueText = dialogueTextElement.GetString();

                                    } else {
                                        GD.PrintErr("Error: 'Dialogue Text' property not found in 'Fields'.");
                                    }
                                    if (fieldsElement.TryGetProperty("fr-FR", out JsonElement frenchTextElement)) {
                                        // Get the string value of "Dialogue Text"
                                        frLocaleText = frenchTextElement.GetString();

                                    } else {
                                        GD.PrintErr("Error: 'Dialogue Text' property not found in 'Fields'.");
                                    }
                                    if (fieldsElement.TryGetProperty("cat-CAT", out JsonElement catalanTextElement)) {
                                        // Get the string value of "Dialogue Text"
                                        catLocaleText = catalanTextElement.GetString();

                                    } else {
                                        GD.PrintErr("Error: 'Dialogue Text' property not found in 'Fields'.");
                                    }


                                } else {
                                    GD.PrintErr("Error: 'Fields' property not found in dialog node.");
                                }

                                if (dialogNode.TryGetProperty("OutgoingLinks", out JsonElement outgoingLinksElement)) {
                                    
                                    // Check if the "OutgoingLinks" property is an array
                                    if (outgoingLinksElement.ValueKind == JsonValueKind.Array) {
                                        List<int> destinationDialogIDs = new List<int>();
                                        // Iterate over each element in the array   
                                                                       
                                        foreach (var outgoingLink in outgoingLinksElement.EnumerateArray()) {
                                            if (outgoingLink.TryGetProperty("DestinationDialogID", out JsonElement destinationDialogIDElement)) {

                                                int destinationDialogID = destinationDialogIDElement.GetInt32();
                                                destinationDialogIDs.Add(destinationDialogID);
                                                // Add the extracted dialogue row to the list  
                                                dialogueObjects.Add(new DialogueObject {
                                                    ID = dialogID,
                                                    DestinationDialogID = destinationDialogIDs,
                                                    DialogueText = dialogueText,
                                                    CatalanText = catLocaleText,
                                                    FrenchText = frLocaleText
                                                });
                                            } else {
                                                GD.PrintErr("Error: 'DestinationDialogID' property not found in 'OutgoingLinks'.");
                                            }
                                        }
                                    } else {
                                        GD.PrintErr("Error: 'OutgoingLinks' property not found in dialog node.");
                                    }
                                }
                            }
                        } else {
                            GD.PrintErr("Error: 'DialogNodes' property not found in conversation.");
                        }

                        // Add the list of dialogue rows to the dictionary
                        conversationObjectsDB[conversationID] = dialogueObjects;
                    }
                } else {
                    GD.PrintErr("Error: 'Conversations' property not found in 'Assets'.");
                }
            } else {
                GD.PrintErr("Error: 'Assets' property not found in JSON.");
            }
        } catch (JsonException e) {
            GD.PrintErr("Error parsing JSON data: " + e.Message);
        }

        return conversationObjectsDB;
    }
}
