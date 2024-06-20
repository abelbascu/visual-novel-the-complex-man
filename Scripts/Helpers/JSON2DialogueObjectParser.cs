using Godot;
using System.Text.Json;
using System.Collections.Generic;


public static class JSON2DialogueObjectParser {

    public static Dictionary<int, List<DialogueObject>> ExtractDialogueObjects(string jsonText) {

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
                                //actor = 1 is the player, we put a high number to force error and not overlap with other actors
                                string actor = "";
                                bool isGroup = false;

                                if (dialogNode.TryGetProperty("IsGroup", out JsonElement isGroupElement)) {
                                    if (isGroupElement.ValueKind == JsonValueKind.False) {
                                        isGroup = false;
                                    } else if (isGroupElement.ValueKind == JsonValueKind.True) {
                                        isGroup = true;
                                    }
                                }

                                // Attempt to access "Fields" property
                                if (dialogNode.TryGetProperty("Fields", out JsonElement fieldsElement)) {
                                    // Attempt to access "Dialogue Text" property within "Fields"
                                    if (fieldsElement.TryGetProperty("Dialogue Text", out JsonElement dialogueTextElement))
                                        // Get the string value of "Dialogue Text"
                                        dialogueText = dialogueTextElement.GetString();

                                    if (fieldsElement.TryGetProperty("fr-FR", out JsonElement frenchTextElement))
                                        // Get the string value of "Dialogue Text"
                                        frLocaleText = frenchTextElement.GetString();

                                    if (fieldsElement.TryGetProperty("cat-CAT", out JsonElement catalanTextElement))
                                        // Get the string value of "Dialogue Text"
                                        catLocaleText = catalanTextElement.GetString();

                                    if (fieldsElement.TryGetProperty("Actor", out JsonElement actorElement))
                                        // Get the string value of "Dialogue Text"
                                        actor = actorElement.GetString();
                                }

                                if (dialogNode.TryGetProperty("OutgoingLinks", out JsonElement outgoingLinksElement)) {
                                    // Check if the "OutgoingLinks" property is an array
                                    //if (outgoingLinksElement.ValueKind == JsonValueKind.Array) {
                                    List<Dictionary<string, int>> outgoingLinks = new();
                                    // Iterate over each element in the array   

                                    int index = 0;
                                    foreach (var outgoingLink in outgoingLinksElement.EnumerateArray()) {

                                        Dictionary<string, int> outgoingLinkDict = new Dictionary<string, int>();

                                        if (outgoingLink.TryGetProperty("DestinationDialogID", out JsonElement destinationDialogIDElement)) {
                                            int destinationDialogID = destinationDialogIDElement.GetInt32();
                                            outgoingLinkDict["DestinationDialogID"] = destinationDialogID;
                                        }
                                        if (outgoingLink.TryGetProperty("DestinationConvoID", out JsonElement destinationConvoIDElement)) {
                                            int destinationConvoID = destinationConvoIDElement.GetInt32();
                                            outgoingLinkDict["DestinationConvoID"] = destinationConvoID;
                                        }
                                        if (outgoingLink.TryGetProperty("OriginDialogID", out JsonElement originDialogIDElement)) {
                                            int originDialogID = originDialogIDElement.GetInt32();
                                            outgoingLinkDict["OriginDialogID"] = originDialogID;
                                        }
                                        if (outgoingLink.TryGetProperty("OriginConvoID", out JsonElement originConvoIDElement)) {
                                            int originConvoID = originConvoIDElement.GetInt32();
                                            outgoingLinkDict["OriginConvoID"] = originConvoID;
                                        }

                                        outgoingLinks.Add(outgoingLinkDict);
                                    }

                                    dialogueObjects.Add(new DialogueObject {
                                        ID = dialogID,
                                        IsGroup = isGroup,
                                        OutgoingLinks = outgoingLinks,
                                        DialogueTextDefault = dialogueText,
                                        CatalanText = catLocaleText,
                                        FrenchText = frLocaleText,
                                        Actor = actor
                                    });

                                }
                                //THIS IS AN UNNECESSARY OPERATION AS IT IS OVERWRITTING conversationObjectsDB[conversationID] FOR EACH NEW dialogueObjects.  
                                //NEEDS TO BE PUT IN AN OUTER CLOSE TO DO THE OPERATION ONLY ONCE!!!!!!
                                // Add the list of dialogue rows to the dictionary
                                conversationObjectsDB[conversationID] = dialogueObjects;

                            }
                        }
                    }
                }
            }

        } catch (JsonException e) {
            GD.PrintErr("Error parsing JSON data: " + e.Message);
        }

        return conversationObjectsDB;
    }
}
