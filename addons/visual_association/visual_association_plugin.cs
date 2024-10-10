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

  private List<int> GetSelectedDialogueIDsFromListView() {
    return dialogueListView.GetSelectedItems()
        .Select(index => (int)dialogueListView.GetItemMetadata(index))
        .ToList();
  }

  private void ResetDialogueMediaField(string fieldName) {
    var selectedDialogueIDs = GetSelectedDialogueIDsFromListView();
    if (selectedDialogueIDs.Count == 0) {
      GD.Print("No dialogues selected.");
      return;
    }
    foreach (var dialogueID in selectedDialogueIDs) {
      if (DialoguesMediaHandler.AllMediaObjects.TryGetValue(dialogueID, out var dialogueObjectMedia)) {
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
    string dialogueDBPath = ProjectSettings.GlobalizePath(DIALLOGUE_DB_JSON_PATH);
    if (!File.Exists(dialogueDBPath)) {
      GD.PrintErr($"File not found: {dialogueDBPath}");
      return new Dictionary<int, List<DialogueObject>>();
    }

    string jsonString = File.ReadAllText(dialogueDBPath);
    return JSON2DialogueObjectParser.ExtractDialogueObjects(jsonString);
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
        dialogueListView.SetItemMetadata(dialogueRowIndexInPluginView, dialogue.ID);
      }
    }
  }


  private void InjectDialogueMediaToDialogueDB() {
    try {
      //we first need to modify the dialogues in the dictionary of dialogues with the new media
      foreach (var conversation in conversationsAndDialoguesDict.Values) {
        foreach (var dialogue in conversation) {
          //in this case the media info comes from the dictionary of media info in the DB, not from the user selection in the plugin
          if (DialoguesMediaHandler.AllMediaObjects.TryGetValue(dialogue.ID, out DialogueMediaObject mediaInfo)) {
            InjectDialogueWithMediaDBorSelection(dialogue, mediaInfo);
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


  private void InjectDialogueWithMediaDBorSelection(DialogueObject dialogue, DialogueMediaObject mediaInfo) {
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
    //we get a dictionary with 'OngaoingLinks', 'ConversationID', 'IsGroup', 'Fields', 'ID'.
    var updatedDialogueFieldsObject = JsonSerializer.Deserialize<Dictionary<string, object>>(fields.GetRawText());

    //*IMPORTANT: here we are getting the UPDATED dialogue object with the new media from conversationsAndDialoguesDict
    var dialogueObject = GetDialogueObjectById(dialogId);
    if (dialogueObject != null) {
      UpdateFieldsFromDialogue(updatedDialogueFieldsObject, dialogueObject);
    }
    //returns a Dictionary<string, object> with the updated fields. String is the field name, like visualPath, visualPreDelay, etc.
    //the values of path nned to be an object and delays are strings and floats so we need an object to hold both types
    //!we are not using IsRoot in the game it seems, check if we can remove it from the json in chatmapper.
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
    var selectedDialogueIDs = GetSelectedDialogueIDsFromListView();
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
    var selectedDialogueIDs = GetSelectedDialogueIDsFromListView();
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
    var selectedDialogueIDs = GetSelectedDialogueIDsFromListView();
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
      SoundPath = path,
      SoundPreDelay = preDelay,
      SoundPostDelay = postDelay
    });
  }

  private void UpdateSelectedDialoguesWithNewMedia(List<int> dialogueIDs, DialogueMediaObject mediaInfo) {
    foreach (var dialogueID in dialogueIDs) {
      var dialogueObj = GetDialogueObject(dialogueID, mediaInfo);
      if (dialogueObj == null) {
        GD.PrintErr($"Dialogue with ID {dialogueID} not found.");
        continue;
      }
      InjectDialogueWithMediaDBorSelection(dialogueObj, mediaInfo);
      UpdateAllMediaObjects(dialogueID, mediaInfo);
    }
    SaveInjectedDialogueObjectsToDialogueDB();
    DialoguesMediaHandler.Save();
    DisplayDialogueDBInListView();
  }


  private DialogueObject GetDialogueObject(int dialogueID, DialogueMediaObject mediaInfo) {

    var dialogue = GetDialogueObjectById(dialogueID);
    if (dialogue == null) {
      GD.PrintErr($"Dialogue with ID {dialogueID} not found.");
      return null;
    }
    return dialogue;
  }

  private DialogueObject GetDialogueObjectById(int dialogueID) {
    //we get the dialogue object from the dictionary that holds all the deserialized dialogue objects from the DB
    return conversationsAndDialoguesDict.Values
        .SelectMany(dialogues => dialogues)
        .FirstOrDefault(d => d.ID == dialogueID);
  }

  private void UpdateAllMediaObjects(int dialogueID, DialogueMediaObject mediaInfo) {
    if (!DialoguesMediaHandler.AllMediaObjects.TryGetValue(dialogueID, out var existingDialogueMediaObject)) {
      existingDialogueMediaObject = new DialogueMediaObject();
      DialoguesMediaHandler.AllMediaObjects[dialogueID] = existingDialogueMediaObject;
    }

    if (!string.IsNullOrEmpty(mediaInfo.VisualPath))
      existingDialogueMediaObject.VisualPath = mediaInfo.VisualPath;
    if (mediaInfo.VisualPreDelay != 0)
      existingDialogueMediaObject.VisualPreDelay = mediaInfo.VisualPreDelay;
    if (mediaInfo.VisualPostDelay != 0)
      existingDialogueMediaObject.VisualPostDelay = mediaInfo.VisualPostDelay;

    if (!string.IsNullOrEmpty(mediaInfo.MusicPath))
      existingDialogueMediaObject.MusicPath = mediaInfo.MusicPath;
    if (mediaInfo.MusicPreDelay != 0)
      existingDialogueMediaObject.MusicPreDelay = mediaInfo.MusicPreDelay;
    if (mediaInfo.MusicPostDelay != 0)
      existingDialogueMediaObject.MusicPostDelay = mediaInfo.MusicPostDelay;

    if (!string.IsNullOrEmpty(mediaInfo.SoundPath))
      existingDialogueMediaObject.SoundPath = mediaInfo.SoundPath;
    if (mediaInfo.SoundPreDelay != 0)
      existingDialogueMediaObject.SoundPreDelay = mediaInfo.SoundPreDelay;
    if (mediaInfo.SoundPostDelay != 0)
      existingDialogueMediaObject.SoundPostDelay = mediaInfo.SoundPostDelay;
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

