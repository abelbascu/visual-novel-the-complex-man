#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

[Tool]
public partial class visual_association_plugin : EditorPlugin {
  private Control pluginEditorDock;
  private ItemList dialogueListView;
  private Dictionary<int, List<DialogueObject>> conversationsAndDialoguesDict;
  private const string DIALLOGUE_DB_JSON_PATH = "res://DialogueDB/dialogueDB.json";
  private Button setImageButton;
  private Button setMusicButton;
  private Button setSoundButton;
  private Button resetImageButton;
  private Button resetMusicButton;
  private Button resetSoundButton;
  private bool isResetMedia = false;

  private static readonly JsonSerializerOptions EncodingJsonOptions = new JsonSerializerOptions {
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  public override void _EnterTree() {
    RemovePluginFromDock();
    InitializePluginControls();
    SetupButtonsEvents();
    SetupDialogueListViewForMultiSelection();
    AddControlToBottomPanel(pluginEditorDock, "Dialogue Media Editor");
    // EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();
    // settings.SetSetting("debugger/auto_switch_to_remote_scene_tree", false);
    //CallDeferred(nameof(AddMainDockInEditorToBottom));
    CallDeferred(nameof(DisplayDialogueDBInListView));
    CallDeferred(nameof(InjectDialogueMediaToDialogueDB));
  }

  public override void _ExitTree() {
    RemovePluginFromDock();
  }

  private void RemovePluginFromDock() {
    if (setImageButton != null) {
      setImageButton.Pressed -= OnAssociateVisualButtonPressed;
      setImageButton = null;
    }
    if (setMusicButton != null) {
      setMusicButton.Pressed -= OnAssociateMusicButtonPressed;
      setMusicButton = null;
    }
    if (setSoundButton != null) {
      setSoundButton.Pressed -= OnAssociateSoundButtonPressed;
      setSoundButton = null;
    }
    if (pluginEditorDock != null) {
      RemoveControlFromBottomPanel(pluginEditorDock);
      pluginEditorDock.QueueFree();
      pluginEditorDock = null;
    }
  }

  private void InitializePluginControls() {
    var scene = ResourceLoader.Load<PackedScene>("res://addons/visual_association/VisualAssociationDock.tscn");
    pluginEditorDock = scene?.Instantiate<Control>();
    if (pluginEditorDock == null) {
      GD.PrintErr("Failed to load or instantiate VisualAssociationDock scene");
      return;
    }
    dialogueListView = pluginEditorDock.GetNodeOrNull<ItemList>("VBoxContainer/ScrollContainer/DialogueList");
    setImageButton = pluginEditorDock.GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/AssociateVisualButton");
    setMusicButton = pluginEditorDock.GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/AssociateMusicButton");
    setSoundButton = pluginEditorDock.GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/AssociateSoundButton");
    resetImageButton = pluginEditorDock.GetNodeOrNull<Button>("VBoxContainer/HBoxContainer2/ResetVisualButton");
    resetMusicButton = pluginEditorDock.GetNodeOrNull<Button>("VBoxContainer/HBoxContainer2/ResetMusicButton");
    resetSoundButton = pluginEditorDock.GetNodeOrNull<Button>("VBoxContainer/HBoxContainer2/ResetSoundButton");
  }

  private void SetupButtonsEvents() {
    if (setImageButton != null) setImageButton.Pressed += OnAssociateVisualButtonPressed;
    if (setMusicButton != null) setMusicButton.Pressed += OnAssociateMusicButtonPressed;
    if (setSoundButton != null) setSoundButton.Pressed += OnAssociateSoundButtonPressed;
    if (resetImageButton != null) resetImageButton.Pressed += OnResetImageButtonPressed;
    if (resetMusicButton != null) resetMusicButton.Pressed += OnResetMusicButtonPressed;
    if (resetSoundButton != null) resetSoundButton.Pressed += OnResetSoundButtonPressed;
  }

  private void OnResetImageButtonPressed() {
    ResetDialogueMediaField("VisualPath");
  }

  private void OnResetMusicButtonPressed() {
    ResetDialogueMediaField("MusicPath");
  }

  private void OnResetSoundButtonPressed() {
    ResetDialogueMediaField("SoundPath");
  }

  private List<(int ConversationID, int DialogueID)> GetSelectedDialoguesFromListView() {
    return dialogueListView.GetSelectedItems()
        .Select(index => {
          var metadata = (Godot.Collections.Dictionary)dialogueListView.GetItemMetadata(index);
          int conversationID = (int)metadata["ConversationID"];
          int dialogueID = (int)metadata["DialogueID"];
          return (conversationID, dialogueID);
        })
        .ToList();
  }
  private void ResetDialogueMediaField(string fieldName) {
    var selectedDialogues = GetSelectedDialoguesFromListView();
    if (selectedDialogues.Count == 0) {
      GD.Print("No dialogues selected.");
      return;
    }
    foreach (var (conversationID, dialogueID) in selectedDialogues) {
      var dialogue = GetDialogueObjectById(conversationID, dialogueID);
      if (dialogue == null) continue;

      if (!DialoguesMediaHandler.AllMediaObjects.TryGetValue(conversationID, out var conversationMediaObjects)) {
        conversationMediaObjects = new Dictionary<int, DialogueMediaObject>();
        DialoguesMediaHandler.AllMediaObjects[conversationID] = conversationMediaObjects;
      }

      if (!conversationMediaObjects.TryGetValue(dialogueID, out var dialogueObjectMedia)) {
        dialogueObjectMedia = new DialogueMediaObject();
        conversationMediaObjects[dialogueID] = dialogueObjectMedia;
      }

      switch (fieldName) {
        case "VisualPath":
          dialogueObjectMedia.VisualPath = "";
          dialogueObjectMedia.VisualPreDelay = 0;
          dialogueObjectMedia.VisualPostDelay = 0;
          break;
        case "MusicPath":
          dialogueObjectMedia.MusicPath = "";
          dialogueObjectMedia.MusicPreDelay = 0;
          dialogueObjectMedia.MusicPostDelay = 0;
          break;
        case "SoundPath":
          dialogueObjectMedia.SoundPath = "";
          dialogueObjectMedia.SoundPreDelay = 0;
          dialogueObjectMedia.SoundPostDelay = 0;
          break;
      }
    }

    isResetMedia = true;

    DialoguesMediaHandler.Save();
    InjectDialogueMediaToDialogueDB();
    DisplayDialogueDBInListView();

    isResetMedia = false;
  }
  private void SetupDialogueListViewForMultiSelection() {
    if (dialogueListView != null) {
      dialogueListView.SelectMode = ItemList.SelectModeEnum.Multi;
    }
  }

  private void DisplayDialogueDBInListView() {
    if (dialogueListView == null) {
      GD.PrintErr("DialogueList is null, cannot load dialogues");
      return;
    }
    try {
      conversationsAndDialoguesDict = GetDialoguesFromDialogueDB();
      AddDialoguesToListView();
    } catch (Exception e) {
      GD.PrintErr($"Error in LoadDialogues: {e.Message}");
      GD.PrintErr(e.StackTrace);
    }
  }

  private Dictionary<int, List<DialogueObject>> GetDialoguesFromDialogueDB() {
    conversationsAndDialoguesDict = new Dictionary<int, List<DialogueObject>>();

    string dialogueDBPath = ProjectSettings.GlobalizePath(DIALLOGUE_DB_JSON_PATH);
    if (!File.Exists(dialogueDBPath)) {
      GD.PrintErr($"File not found: {dialogueDBPath}");
      return conversationsAndDialoguesDict;
    }

    string jsonString = File.ReadAllText(dialogueDBPath);
    var allDialogues = JSON2DialogueObjectParser.ExtractDialogueObjects(jsonString);

    // Ensure all conversations are included
    foreach (var conversation in allDialogues) {
      conversationsAndDialoguesDict[conversation.Key] = conversation.Value;
    }

    return conversationsAndDialoguesDict;
  }

  private void AddDialoguesToListView() {
    dialogueListView.Clear();
    foreach (var conversation in conversationsAndDialoguesDict) {
      foreach (var dialogue in conversation.Value) {
        string itemText = $"Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 120))}...";
        //AddItem is a Godot method that adds an item to an item list
        //and returns the index of the item in the list
        int dialogueRowIndexInPluginView = dialogueListView.AddItem(itemText);
        //add the real dialogue ID as metadata to the item in the list, 
        //because may not coincide with the row index of the item in the list
        //!should we save the conversatin ID in the metadata?
        var metadata = new Godot.Collections.Dictionary {
                { "ConversationID", conversation.Key },
                { "DialogueID", dialogue.ID }
            };
        dialogueListView.SetItemMetadata(dialogueRowIndexInPluginView, metadata);
      }
    }
  }

  private void InjectDialogueMediaToDialogueDB() {
    try {
      foreach (var conversationKVP in conversationsAndDialoguesDict) {
        int conversationID = conversationKVP.Key;
        foreach (var dialogue in conversationKVP.Value) {
          if (DialoguesMediaHandler.AllMediaObjects.TryGetValue(conversationID, out var conversationDialogues) &&
              conversationDialogues.TryGetValue(dialogue.ID, out DialogueMediaObject mediaInfo)) {
            InjectDialogueObjectWithMediaDBorSelection(dialogue, mediaInfo);
          }
        }
      }
    } catch (Exception e) {
      GD.PrintErr($"Error in InjectDialoguesMediaHandlerInfoDBToDialogueDB {e.Message}");
      GD.PrintErr(e.StackTrace);
    }
    SaveInjectedDialogueObjectsToDialogueDB();
    GD.Print("[InjectDialogueMediaToDialogueDB] Finished injecting saved visual paths to dialoguesDB.json");
  }

  private void InjectDialogueObjectWithMediaDBorSelection(DialogueObject dialogue, DialogueMediaObject mediaInfo) {

    //the dialogue that we are passed is part of conversationsAndDialoguesDict 
    //we don't need the concersationID here because we come from a conversation foreach loop
    if (!string.IsNullOrEmpty(mediaInfo.VisualPath) || isResetMedia)
      dialogue.VisualPath = mediaInfo.VisualPath;
    if (mediaInfo.VisualPreDelay != 0 || isResetMedia)
      dialogue.VisualPreDelay = mediaInfo.VisualPreDelay;
    if (mediaInfo.VisualPostDelay != 0 || isResetMedia)
      dialogue.VisualPostDelay = mediaInfo.VisualPostDelay;

    // Only update music and sound fields if they were modified in the plugin
    if (!string.IsNullOrEmpty(mediaInfo.MusicPath) || isResetMedia)
      dialogue.MusicPath = mediaInfo.MusicPath;
    if (mediaInfo.MusicPreDelay != 0 || isResetMedia)
      dialogue.MusicPreDelay = mediaInfo.MusicPreDelay;
    if (mediaInfo.MusicPostDelay != 0 || isResetMedia)
      dialogue.MusicPostDelay = mediaInfo.MusicPostDelay;

    if (!string.IsNullOrEmpty(mediaInfo.SoundPath) || isResetMedia)
      dialogue.SoundPath = mediaInfo.SoundPath;
    if (mediaInfo.SoundPreDelay != 0 || isResetMedia)
      dialogue.SoundPreDelay = mediaInfo.SoundPreDelay;
    if (mediaInfo.SoundPostDelay != 0 || isResetMedia)
      dialogue.SoundPostDelay = mediaInfo.SoundPostDelay;

    //we have modified the dialogue object, so we have also modified conversationsAndDialoguesDict
    //so after this we need to save the conversationsAndDialoguesDict to the dialogueDB.json file
  }

  private void SaveInjectedDialogueObjectsToDialogueDB() {
    string dialogueDBPath = ProjectSettings.GlobalizePath(DIALLOGUE_DB_JSON_PATH);
    string backupDialogueDBPath = dialogueDBPath + ".backup";
    File.Copy(dialogueDBPath, backupDialogueDBPath, true);

    try {
      string dialogueDBJsonString = File.ReadAllText(dialogueDBPath);
      //the method returns a serialized json string with the dialogueDB already updated with the media
      var updatedJson = UpdateDialogueDBWithUpdatedDialogueObjects(dialogueDBJsonString);
      //now we save the all the dialogues to the dialogueDB.json file
      File.WriteAllText(dialogueDBPath, updatedJson);
      GD.Print("JSON file updated successfully.");
    } catch (Exception e) {
      GD.PrintErr($"Error updating JSON file in SaveInjectedDialogueObjectsToDialogueDB: {e.Message}");
      File.Copy(backupDialogueDBPath, dialogueDBPath, true);
    }
  }


  private string UpdateDialogueDBWithUpdatedDialogueObjects(string dialogueDBJsonString) {
    //we convert the string to Docuemnt for more efficient manipulation on nested structures and safety
    using var dialogueDBJsonDocument = JsonDocument.Parse(dialogueDBJsonString);
    //the first element of our dialgoueDB is "Assets"
    var root = dialogueDBJsonDocument.RootElement;
    //we get the next level of the json, which is the "Conversations" array
    var conversationsArray = root.GetProperty("Assets").GetProperty("Conversations");
    var updatedConversations = new List<object>();

    foreach (var conversation in conversationsArray.EnumerateArray()) {
      var updatedConversationObject = UpdateDialoguesInConversation(conversation);
      updatedConversations.Add(updatedConversationObject);
    }
    //we are mimicking the dialgoueDB.json structure, so updateRoot is a dictionary with Key: "Assets"
    //and Value: dictionary with Key: "Conversations" and Value: List of updated object conversations
    var updatedRoot = new Dictionary<string, object> {
      ["Assets"] = new Dictionary<string, object> {
        ["Conversations"] = updatedConversations
      }
    };
    return JsonSerializer.Serialize(updatedRoot, EncodingJsonOptions);
  }

  private object UpdateDialoguesInConversation(JsonElement conversation) {
    object updatedConversationObject;
    int conversationId = conversation.GetProperty("ID").GetInt32();
    var dialogNodesArray = conversation.GetProperty("DialogNodes");
    var updatedDialogNodes = new List<object>();

    foreach (var dialogNode in dialogNodesArray.EnumerateArray()) {
      var updatedNode = UpdateDialogNode(dialogNode, conversationId);
      updatedDialogNodes.Add(updatedNode);
    }

    updatedConversationObject = new {
      ID = conversationId,
      DialogNodes = updatedDialogNodes
    };

    return updatedConversationObject;
  }

  private object UpdateDialogNode(JsonElement dialogNode, int conversationId) {
    int dialogId = dialogNode.GetProperty("ID").GetInt32();
    var fields = dialogNode.GetProperty("Fields");
    var updatedDialogueFieldsObject = JsonSerializer.Deserialize<Dictionary<string, object>>(fields.GetRawText());

    var dialogueObject = GetDialogueObjectById(conversationId, dialogId);
    if (dialogueObject != null) {
      UpdateFieldsFromDialogue(updatedDialogueFieldsObject, dialogueObject);
    }

    return new {
      OutgoingLinks = JsonSerializer.Deserialize<object>(dialogNode.GetProperty("OutgoingLinks").GetRawText()),
      ConversationID = conversationId,
      IsRoot = dialogNode.TryGetProperty("IsRoot", out var isRoot) ? isRoot.GetBoolean() : false,
      IsGroup = dialogueObject?.IsGroup ?? false,
      Fields = updatedDialogueFieldsObject,
      ID = dialogId
    };
  }
  private void UpdateFieldsFromDialogue(Dictionary<string, object> fields, DialogueObject dialogueObject) {
    fields["VisualPath"] = dialogueObject.VisualPath ?? "";
    fields["VisualPreDelay"] = dialogueObject.VisualPreDelay;
    fields["VisualPostDelay"] = dialogueObject.VisualPostDelay;
    fields["MusicPath"] = dialogueObject.MusicPath ?? "";
    fields["MusicPreDelay"] = dialogueObject.MusicPreDelay;
    fields["MusicPostDelay"] = dialogueObject.MusicPostDelay;
    fields["SoundPath"] = dialogueObject.SoundPath ?? "";
    fields["SoundPreDelay"] = dialogueObject.SoundPreDelay;
    fields["SoundPostDelay"] = dialogueObject.SoundPostDelay;
  }

  private async void OnAssociateVisualButtonPressed() {
    var selectedDialogueIDs = GetSelectedDialoguesFromListView();
    if (selectedDialogueIDs.Count == 0) {
      GD.Print("No dialogues selected.");
      return;
    }
    var fileDialog = new FileDialog {
      FileMode = FileDialog.FileModeEnum.OpenFile,
      MinSize = new Vector2I(800, 600)
    };
    fileDialog.AddFilter("*.png, *.jpg, *.jpeg ; Supported Images");

    GetTree().Root.AddChild(fileDialog);
    fileDialog.PopupCentered();

    var result = await ToSignal(fileDialog, "file_selected");
    string path = (string)result[0];
    var (preDelay, postDelay) = await ShowDelayInputDialog();

    UpdateSelectedDialoguesWithNewMedia(selectedDialogueIDs, new DialogueMediaObject {
      VisualPath = path,
      VisualPreDelay = preDelay,
      VisualPostDelay = postDelay
    });
  }

  private async void OnAssociateMusicButtonPressed() {
    var selectedDialogueIDs = GetSelectedDialoguesFromListView();
    if (selectedDialogueIDs.Count == 0) {
      GD.Print("No dialogues selected.");
      return;
    }
    var fileDialog = new FileDialog {
      FileMode = FileDialog.FileModeEnum.OpenFile,
      MinSize = new Vector2I(800, 600)
    };
    fileDialog.AddFilter("*.mp3, *.wav ; Supported Audio");

    GetTree().Root.AddChild(fileDialog);
    fileDialog.PopupCentered();

    var result = await ToSignal(fileDialog, "file_selected");
    string path = (string)result[0];

    var (preDelay, postDelay) = await ShowDelayInputDialog();

    UpdateSelectedDialoguesWithNewMedia(selectedDialogueIDs, new DialogueMediaObject {
      MusicPath = path,
      MusicPreDelay = preDelay,
      MusicPostDelay = postDelay
    });
  }

  private async void OnAssociateSoundButtonPressed() {
    var selectedDialogueIDs = GetSelectedDialoguesFromListView();
    if (selectedDialogueIDs.Count == 0) {
      GD.Print("No dialogues selected.");
      return;
    }
    var fileDialog = new FileDialog {
      FileMode = FileDialog.FileModeEnum.OpenFile,
      MinSize = new Vector2I(800, 600)
    };
    fileDialog.AddFilter("*.mp3, *.wav ; Supported Audio");

    GetTree().Root.AddChild(fileDialog);
    fileDialog.PopupCentered();

    var result = await ToSignal(fileDialog, "file_selected");
    string path = (string)result[0];

    var (preDelay, postDelay) = await ShowDelayInputDialog();

    //!this selectedDialogueIDs
    UpdateSelectedDialoguesWithNewMedia(selectedDialogueIDs, new DialogueMediaObject {
      SoundPath = path,
      SoundPreDelay = preDelay,
      SoundPostDelay = postDelay
    });
  }
  private void UpdateSelectedDialoguesWithNewMedia(List<(int ConversationID, int DialogueID)> selectedDialogues, DialogueMediaObject mediaInfo) {
    foreach (var (conversationID, dialogueID) in selectedDialogues) {
      var dialogueObj = GetDialogueObjectById(conversationID, dialogueID);
      if (dialogueObj == null) {
        GD.Print($"Dialogue with ID {dialogueID} in Conversation {conversationID} not found.");
        continue;
      }
      InjectDialogueObjectWithMediaDBorSelection(dialogueObj, mediaInfo);
      UpdateDialogueMediaObject(conversationID, dialogueID, mediaInfo);
    }
    DialoguesMediaHandler.Save();
    InjectDialogueMediaToDialogueDB();
    DisplayDialogueDBInListView();
  }

  private DialogueObject GetDialogueObject(int conversationID, int dialogueID, DialogueMediaObject mediaInfo) {
    var dialogue = GetDialogueObjectById(conversationID, dialogueID);
    if (dialogue == null) {
      GD.PrintErr($"Dialogue with ID {dialogueID} in Conversation {conversationID} not found.");
      return null;
    }
    return dialogue;
  }
  private DialogueObject GetDialogueObjectById(int conversationID, int dialogueID) {
    if (conversationsAndDialoguesDict.TryGetValue(conversationID, out var dialogues)) {
      return dialogues.FirstOrDefault(d => d.ID == dialogueID);
    }
    return null;
  }


  //this method updates the changed dialogue media object in the dictionary DialoguesMediaHandler.AllMediaObjects
  //it correctly preserves previous values for fields that are not being updated
  //it does NOT save it yet in the dialogueMediaDB.json
  private void UpdateDialogueMediaObject(int conversationID, int dialogueID, DialogueMediaObject mediaInfo) {
    if (!DialoguesMediaHandler.AllMediaObjects.TryGetValue(conversationID, out var conversationDialogues)) {
      conversationDialogues = new Dictionary<int, DialogueMediaObject>();
      DialoguesMediaHandler.AllMediaObjects[conversationID] = conversationDialogues;
    }

    if (!conversationDialogues.TryGetValue(dialogueID, out var existingMedia)) {
      existingMedia = new DialogueMediaObject { ConversationID = conversationID };
    }

    // Update visual fields only if new data exists
    if (!string.IsNullOrEmpty(mediaInfo.VisualPath)) { existingMedia.VisualPath = mediaInfo.VisualPath; }
    if (mediaInfo.VisualPreDelay != 0) { existingMedia.VisualPreDelay = mediaInfo.VisualPreDelay; }
    if (mediaInfo.VisualPostDelay != 0) { existingMedia.VisualPostDelay = mediaInfo.VisualPostDelay; }

    // Update music fields only if new data exists
    if (!string.IsNullOrEmpty(mediaInfo.MusicPath)) { existingMedia.MusicPath = mediaInfo.MusicPath; }
    if (mediaInfo.MusicPreDelay != 0) { existingMedia.MusicPreDelay = mediaInfo.MusicPreDelay; }
    if (mediaInfo.MusicPostDelay != 0) { existingMedia.MusicPostDelay = mediaInfo.MusicPostDelay; }

    // Update sound fields only if new data exists
    if (!string.IsNullOrEmpty(mediaInfo.SoundPath)) { existingMedia.SoundPath = mediaInfo.SoundPath; }
    if (mediaInfo.SoundPreDelay != 0) { existingMedia.SoundPreDelay = mediaInfo.SoundPreDelay; }
    if (mediaInfo.SoundPostDelay != 0) { existingMedia.SoundPostDelay = mediaInfo.SoundPostDelay; }

    //if we had to create a new media object, we need to add it to DialoguesMediaHandler.AllMediaObjects via its reference
    conversationDialogues[dialogueID] = existingMedia;
  }




  private async Task<(float preDelay, float postDelay)> ShowDelayInputDialog() {
    var popup = new AcceptDialog();
    var vbox = new VBoxContainer();
    var preDelayInput = new LineEdit { PlaceholderText = "Pre-dialogue delay (seconds)" };
    var postDelayInput = new LineEdit { PlaceholderText = "Post-dialogue delay (seconds)" };
    vbox.AddChild(preDelayInput);
    vbox.AddChild(postDelayInput);
    popup.AddChild(vbox);
    AddChild(popup);
    popup.PopupCentered();

    await ToSignal(popup, "confirmed");

    float.TryParse(preDelayInput.Text, out float preDelay);
    float.TryParse(postDelayInput.Text, out float postDelay);

    return (preDelay, postDelay);
  }
}
#endif

