using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

public partial class DialogueManager : Node {
    private Dictionary<int, List<DialogueRow>> dialogueRowsByConversation;

    public override void _Ready() {
        // Load dialogue data and populate dialogueRowsByConversation
        LoadDialogueData("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");

        //Display the extracted dialogue rows for each conversation
        foreach (var kvp in dialogueRowsByConversation) {
            GD.Print($"Conversation ID: {kvp.Key}");
            foreach (var row in kvp.Value) {
                GD.Print($"ID: {row.ID}, DestinationDialogID: {row.DestinationDialogID}, Dialogue Text: {row.DialogueText}");
            }
        }

        // Get reference to DialogueDisplay node
        DialogueDisplay dialogueDisplay = GetNode<DialogueDisplay>("/root/GameStartScene/DialogueDisplay");

        // Initialize and display dialogue
        dialogueDisplay.InitializeDialogueRows(dialogueRowsByConversation);


        // Display the dialogue for the current conversation
        dialogueDisplay.DisplayDialogue();


    }

    private void LoadDialogueData(string filePath) {
        try {
            // Read JSON data from file
            string jsonText = File.ReadAllText(filePath);

            // Deserialize JSON data and extract the required fields
            dialogueRowsByConversation = ExtractDialogueRows(jsonText);
        } catch (IOException e) {
            GD.PrintErr("Error loading dialogue data: " + e.Message);
        } catch (JsonException e) {
            GD.PrintErr("Error parsing JSON data: " + e.Message);
        }
    }

    private Dictionary<int, List<DialogueRow>> ExtractDialogueRows(string jsonText) {
        // Initialize a dictionary to store dialogue rows by conversation ID
        var dialogueRowsByConversation = new Dictionary<int, List<DialogueRow>>();

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
                        var dialogueRows = new List<DialogueRow>();

                        // Access "DialogNodes" property within the conversation
                        if (conversation.TryGetProperty("DialogNodes", out JsonElement dialogNodesElement)) {
                            // Iterate over each dialog node in "DialogNodes"
                            foreach (var dialogNode in dialogNodesElement.EnumerateArray()) {
                                // Extract "ID" from the dialog node
                                int dialogID = dialogNode.GetProperty("ID").GetInt32();
                                string dialogueText = "";


                                // Attempt to access "Fields" property
                                if (dialogNode.TryGetProperty("Fields", out JsonElement fieldsElement)) {
                                    // Attempt to access "Dialogue Text" property within "Fields"
                                    if (fieldsElement.TryGetProperty("Dialogue Text", out JsonElement dialogueTextElement)) {
                                        // Get the string value of "Dialogue Text"
                                        dialogueText = dialogueTextElement.GetString();

                                    } else {
                                        GD.PrintErr("Error: 'Dialogue Text' property not found in 'Fields'.");
                                    }
                                } else {
                                    GD.PrintErr("Error: 'Fields' property not found in dialog node.");
                                }

                                if (dialogNode.TryGetProperty("OutgoingLinks", out JsonElement outgoingLinksElement)) {

                                    // Check if the "OutgoingLinks" property is an array
                                    if (outgoingLinksElement.ValueKind == JsonValueKind.Array) {
                                        // Iterate over each element in the array
                                        foreach (var outgoingLink in outgoingLinksElement.EnumerateArray()) {
                                            if (outgoingLink.TryGetProperty("DestinationDialogID", out JsonElement destinationDialogIDElement)) {

                                                int destinationDialogID = destinationDialogIDElement.GetInt32();

                                                // Add the extracted dialogue row to the list  
                                                dialogueRows.Add(new DialogueRow {
                                                    ID = dialogID,
                                                    DestinationDialogID = destinationDialogID,
                                                    DialogueText = dialogueText
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
                        dialogueRowsByConversation[conversationID] = dialogueRows;
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

        return dialogueRowsByConversation;
    }
}
