using Godot;
using System.Collections.Generic;

public partial class DialogueDisplay : RichTextLabel
{
    private Dictionary<int, List<DialogueRow>> dialogueRowsByConversation;

    public void InitializeDialogueRows(Dictionary<int, List<DialogueRow>> dialogueRowsByConversation)
    {
        this.dialogueRowsByConversation = dialogueRowsByConversation;
    }

    public void DisplayDialogue()
    {
        // Clear the current text
        Clear();
        foreach (var conversationID in dialogueRowsByConversation.Keys) {
            // Check if the conversation ID exists
            if (dialogueRowsByConversation.ContainsKey(conversationID)) {
                // Find the dialogue rows for the specified conversation ID
                List<DialogueRow> conversation = dialogueRowsByConversation[conversationID];

                // Display each dialogue row
                foreach (var dialogueRow in conversation) {
                    string dialogueText = $"[b]ID:[/b] {dialogueRow.ID}, [b]Destination ID:[/b] {dialogueRow.DestinationDialogID}\n";
                    dialogueText += $"{dialogueRow.DialogueText}\n\n";

                    AddText(dialogueText);
                    Text = dialogueText;
                }
            } else {
                GD.PrintErr($"Conversation with ID {conversationID} not found.");
            }
        }
    }
}
