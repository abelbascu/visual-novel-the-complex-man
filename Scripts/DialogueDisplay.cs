using Godot;
using System.Collections.Generic;

public partial class DialogueDisplay : RichTextLabel
{
    private Dictionary<int, List<DialogueObject>> conversationDialogues;

    public void InitializeConversationDialogues(Dictionary<int, List<DialogueObject>> conversationDialogues)
    {
        this.conversationDialogues = conversationDialogues;
    }

    public void DisplayDialogue()
    {
        // Clear the current text
        Clear();
        foreach (var conversationID in conversationDialogues.Keys) {
            // Check if the conversation ID exists
            if (conversationDialogues.ContainsKey(conversationID)) {
                // Find the dialogue rows for the specified conversation ID
                List<DialogueObject> conversation = conversationDialogues[conversationID];

                // Display each dialogue row
                foreach (var dialogueObject in conversation) {
                    string dialogueProperties = $"[b]ID:[/b] {dialogueObject.ID}, [b]Destination ID:[/b] {dialogueObject.DestinationDialogIDs}\n";
                    dialogueProperties += $"{dialogueObject.DialogueText}\n\n";

                    AddText(dialogueProperties);
                    Text = dialogueProperties;
                }
            } else {
                GD.PrintErr($"Conversation with ID {conversationID} not found.");
            }
        }
    }
}
