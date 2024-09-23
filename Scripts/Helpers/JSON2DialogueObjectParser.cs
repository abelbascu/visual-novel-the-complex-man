using Godot;
using System;
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
                bool isNoTurningBackPath = false;
                string visualPath = "";
                float visualPredelay = 0;
                float visualPostDelay = 0;
                string musicPath = "";
                float musicPreDelay = 0;
                float musicPostDelay = 0;
                string soundPath = "";
                float soundPreDelay = 0;
                float soundPostDelay = 0;

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
                    dialogueText = dialogueTextElement.GetString();
                  if (fieldsElement.TryGetProperty("fr-FR", out JsonElement frenchTextElement))
                    frLocaleText = frenchTextElement.GetString();
                  if (fieldsElement.TryGetProperty("cat-CAT", out JsonElement catalanTextElement))
                    catLocaleText = catalanTextElement.GetString();
                  if (fieldsElement.TryGetProperty("Actor", out JsonElement actorElement))
                    actor = actorElement.GetString();
                  if (fieldsElement.TryGetProperty("VisualPath", out JsonElement visualPathElement))
                    visualPath = visualPathElement.GetString();
                  if (fieldsElement.TryGetProperty("VisualPreDelay", out JsonElement visualPreDelayElement))
                    visualPostDelay = visualPreDelayElement.ValueKind == JsonValueKind.String ?
                        float.TryParse(visualPreDelayElement.GetString(), out float result) ? result : 0 :
                        visualPreDelayElement.GetSingle();
                  if (fieldsElement.TryGetProperty("VisualPostDelay", out JsonElement visualPostDelayElement))
                    visualPostDelay = visualPostDelayElement.ValueKind == JsonValueKind.String ?
                        float.TryParse(visualPostDelayElement.GetString(), out float result) ? result : 0 :
                        visualPostDelayElement.GetSingle();
                  if (fieldsElement.TryGetProperty("MusicPath", out JsonElement musicPathElement))
                    musicPath = musicPathElement.GetString();
                  if (fieldsElement.TryGetProperty("MusicPreDelay", out JsonElement musicPreDelayElement))
                    musicPreDelay = musicPreDelayElement.ValueKind == JsonValueKind.String ?
                        float.TryParse(musicPreDelayElement.GetString(), out float result) ? result : 0 :
                        musicPreDelayElement.GetSingle();
                  if (fieldsElement.TryGetProperty("MusicPostDelay", out JsonElement musicPostDelayElement))
                    musicPostDelay = musicPostDelayElement.ValueKind == JsonValueKind.String ?
                        float.TryParse(musicPostDelayElement.GetString(), out float result) ? result : 0 :
                        musicPostDelayElement.GetSingle();
                  if (fieldsElement.TryGetProperty("SoundPath", out JsonElement soundPathElement))
                    soundPath = soundPathElement.GetString();
                  if (fieldsElement.TryGetProperty("SoundPreDelay", out JsonElement soundPreDelayElement))
                    soundPreDelay = soundPreDelayElement.ValueKind == JsonValueKind.String ?
                        float.TryParse(soundPreDelayElement.GetString(), out float result) ? result : 0 :
                        soundPreDelayElement.GetSingle();
                  if (fieldsElement.TryGetProperty("SoundPostDelay", out JsonElement soundPostDelayElement))
                    soundPostDelay = soundPostDelayElement.ValueKind == JsonValueKind.String ?
                        float.TryParse(soundPostDelayElement.GetString(), out float result) ? result : 0 :
                        soundPostDelayElement.GetSingle();
                  if (fieldsElement.TryGetProperty("IsNoTurningBackPath", out JsonElement isNoTurningBackPathElement)) {
                    if (isNoTurningBackPathElement.ValueKind == JsonValueKind.String) {
                      string value = isNoTurningBackPathElement.GetString();
                      isNoTurningBackPath = value.Equals("True", StringComparison.OrdinalIgnoreCase);
                    } else if (isNoTurningBackPathElement.ValueKind == JsonValueKind.True) {
                      isNoTurningBackPath = true;
                    } else if (isNoTurningBackPathElement.ValueKind == JsonValueKind.False) {
                      isNoTurningBackPath = false;
                    }
                  }
                }

                if (dialogNode.TryGetProperty("OutgoingLinks", out JsonElement outgoingLinksElement)) {
                  // Check if the "OutgoingLinks" property is an array
                  //if (outgoingLinksElement.ValueKind == JsonValueKind.Array) {
                  List<Dictionary<string, int>> outgoingLinks = new();
                  // Iterate over each element in the array   

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
                    Actor = actor,
                    IsNoTurningBackPath = isNoTurningBackPath,
                    VisualPath = visualPath,
                    MusicPath = musicPath,
                    MusicPreDelay = musicPreDelay,
                    MusicPostDelay = musicPostDelay,
                    SoundPath = soundPath,
                    SoundPreDelay = soundPreDelay,
                    SoundPostDelay = soundPostDelay

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
};

