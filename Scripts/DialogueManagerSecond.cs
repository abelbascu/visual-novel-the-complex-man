using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

public partial class DialogueManagerSecond : Node
{
    private Dictionary<int, List<DialogueObject>> dialogueRowsByConversation;

    public override void _Ready()
    {
        // Load dialogue data and populate dialogueRowsByConversation
        LoadDialogueData("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");

        // Display the extracted dialogue rows for each conversation
        foreach (var kvp in dialogueRowsByConversation)
        {
            GD.Print($"Conversation ID: {kvp.Key}");
            foreach (var row in kvp.Value)
            {
                GD.Print($"ID: {row.ID}, DestinationDialogID: {row.DestinationDialogID}, Dialogue Text: {row.DialogueText}");
            }
        }

        // Get reference to DialogueDisplay node
        DialogueDisplay dialogueDisplay = GetNode<DialogueDisplay>("/root/GameStartScene/DialogueDisplay");

        // Initialize and display dialogue
        dialogueDisplay.InitializeConversationDialogues(dialogueRowsByConversation);
        foreach(var conversationID in dialogueRowsByConversation.Keys)
        {
        dialogueDisplay.DisplayDialogue();
        }
    }

    private void LoadDialogueData(string filePath)
    {
        try
        {
            // Read JSON data from file
            string jsonText = File.ReadAllText(filePath);

            // Deserialize JSON data and extract the required fields
            dialogueRowsByConversation = ExtractDialogueRows(jsonText);
        }
        catch (IOException e)
        {
            GD.PrintErr("Error loading dialogue data: " + e.Message);
        }
        catch (JsonException e)
        {
            GD.PrintErr("Error parsing JSON data: " + e.Message);
        }
    }

    private Dictionary<int, List<DialogueObject>> ExtractDialogueRows(string jsonText)
    {
        // Initialize a dictionary to store dialogue rows by conversation ID
        var dialogueRowsByConversation = new Dictionary<int, List<DialogueObject>>();

        // Deserialize the JSON data into a dynamic object
        var jsonObject = JsonSerializer.Deserialize<dynamic>(jsonText);

        // Traverse the JSON structure and extract the desired fields
        foreach (var conversation in jsonObject["Assets"]["Conversations"])
        {
            int conversationID = conversation["ID"];
            var dialogueRows = new List<DialogueObject>();

            foreach (var dialogNode in conversation["DialogNodes"])
            {
                // Extract "ID" from the dialog node
                int dialogID = dialogNode["ID"];

                foreach (var outgoingLink in dialogNode["OutgoingLinks"])
                {
                    int destinationDialogID = outgoingLink["DestinationDialogID"];

                    // Extract "Dialogue Text" from "Fields"
                    string dialogueText = dialogNode["Fields"]["Dialogue Text"];

                    // Add the extracted dialogue row to the list
                    dialogueRows.Add(new DialogueObject
                    {
                        ID = dialogID,
                        DestinationDialogID = destinationDialogID,
                        DialogueText = dialogueText
                    });
                }
            }

            // Add the list of dialogue rows to the dictionary
            dialogueRowsByConversation[conversationID] = dialogueRows;
        }

        return dialogueRowsByConversation;
    }
}