using Godot;
using System.Collections.Generic;

public partial class DialogueDisplay : RichTextLabel
{
    private List<DialogueRow> dialogueRows;

    // Initialize dialogueRows with sample data (replace with actual data)
    public void InitializeDialogueRows(List<DialogueRow> rows)
    {
        dialogueRows = rows;
    }

    // Display the contents of dialogueRows on the screen
    public void DisplayDialogue()
    {
        if (dialogueRows == null || dialogueRows.Count == 0)
        {
            Text = "[color=red]No dialogue available.[/color]";
            return;
        }

        // Construct BBCode string to display dialogue rows
        string bbcode = "";
        foreach (DialogueRow row in dialogueRows)
        {
            bbcode += $"[b]ID:[/b] {row.ID}, [b]Destination ID:[/b] {row.DestinationDialogID}\n{row.DialogueText}\n\n";
        }

        Text = bbcode;
    }
}
