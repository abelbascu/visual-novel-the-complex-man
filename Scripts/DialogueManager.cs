using Godot;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

public partial class DialogueManager : Node
{
    private List<DialogueRow> dialogueRows;

    public override void _Ready()
    {
        // Load dialogue data and populate dialogueRows
        LoadDialogueData("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueTest.json");

        // Get reference to DialogueDisplay node
        DialogueDisplay dialogueDisplay = GetNode<DialogueDisplay>("/root/GameStartScene/DialogueDisplay");

        // Initialize and display dialogue
        dialogueDisplay.InitializeDialogueRows(dialogueRows);
        dialogueDisplay.DisplayDialogue();
    }

    private void LoadDialogueData(string filePath)
    {
        try
        {
            // Read JSON data from file
            string jsonText = File.ReadAllText(filePath);

            // Deserialize JSON data into List<DialogueRow>
            dialogueRows = JsonSerializer.Deserialize<List<DialogueRow>>(jsonText);
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
}
