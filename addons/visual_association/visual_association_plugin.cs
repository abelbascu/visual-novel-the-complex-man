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
    CallDeferred(nameof(InjectDialogueMediaDBToDialogueDB));
  }

  public override void _ExitTree() {
    RemovePluginFromDock();
  }

  private void RemovePluginFromDock() {
    if (setImageButton != null) {
      setImageButton.Pressed -= OnAssociateButtonPressed;
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
    if (setImageButton != null) setImageButton.Pressed += OnAssociateButtonPressed;
    if (setMusicButton != null) setMusicButton.Pressed += OnAssociateMusicButtonPressed;
    if (setSoundButton != null) setSoundButton.Pressed += OnAssociateSoundButtonPressed;
    if (resetImageButton != null) resetImageButton.Pressed += OnResetImageButtonPressed;
    if (resetMusicButton != null) resetMusicButton.Pressed += OnResetMusicButtonPressed;
    if (resetSoundButton != null) resetSoundButton.Pressed += OnResetSoundButtonPressed;
  }

  private void SetupDialogueListViewForMultiSelection() {
    if (dialogueListView != null) {
      dialogueListView.SelectMode = ItemList.SelectModeEnum.Multi;
    }
  }

  private void OnResetImageButtonPressed() {
    ResetMediaField("VisualPath");
  }

  private void OnResetMusicButtonPressed() {
    ResetMediaField("MusicPath");
  }

  private void OnResetSoundButtonPressed() {
    ResetMediaField("SoundPath");
  }

  private void ResetMediaField(string fieldName) {
    var selectedDialogueIDs = GetSelectedDialogueIDs();
    if (selectedDialogueIDs.Count == 0) {
      GD.Print("No dialogues selected.");
      return;
    }

    foreach (var dialogueID in selectedDialogueIDs) {
      if (DialoguesMediaHandler.AllMediaObjects.TryGetValue(dialogueID, out var dialogueObjectMediaInfo)) {
        switch (fieldName) {
          case "VisualPath":
            dialogueObjectMediaInfo.VisualPath = "";
            dialogueObjectMediaInfo.VisualPreDelay = 0;
            dialogueObjectMediaInfo.VisualPostDelay = 0;
            break;
          case "MusicPath":
            dialogueObjectMediaInfo.MusicPath = "";
            dialogueObjectMediaInfo.MusicPreDelay = 0;
            dialogueObjectMediaInfo.MusicPostDelay = 0;
            break;
          case "SoundPath":
            dialogueObjectMediaInfo.SoundPath = "";
            dialogueObjectMediaInfo.SoundPreDelay = 0;
            dialogueObjectMediaInfo.SoundPostDelay = 0;
            break;
        }
      }
    }

    DialoguesMediaHandler.Save();
    SaveDialoguesToJson();
    RefreshDialogueList();
  }


  private void DisplayDialogueDBInListView() {
    if (dialogueListView == null) {
      GD.PrintErr("DialogueList is null, cannot load dialogues");
      return;
    }
    try {
      conversationsAndDialoguesDict = LoadDialoguesFromJson();
      PopulateDialogueList();
    } catch (Exception e) {
      GD.PrintErr($"Error in LoadDialogues: {e.Message}");
      GD.PrintErr(e.StackTrace);
    }
  }

  private void InjectDialogueMediaDBToDialogueDB() {
    try {
      foreach (var conversation in conversationsAndDialoguesDict.Values) {
        foreach (var dialogue in conversation) {
          if (DialoguesMediaHandler.AllMediaObjects.TryGetValue(dialogue.ID, out DialogueMediaObject mediaInfo)) {
            UpdateDialogueMedia(dialogue, mediaInfo);
          }
        }
      }
    } catch (Exception e) {
      GD.PrintErr($"Error in InjectDialoguesMediaHandlerInfoDBToDialogueDB {e.Message}");
      GD.PrintErr(e.StackTrace);
    }
    SaveDialoguesToJson();
    GD.Print("[InjectDialogueMediaDBToDialogueDB] Finished injecting saved visual paths to dialoguesDB.json");
  }

  private void PopulateDialogueList() {
    dialogueListView.Clear();
    foreach (var conversation in conversationsAndDialoguesDict) {
      foreach (var dialogue in conversation.Value) {
        string itemText = $"Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 120))}...";
        int dialogueRowIndexInPluginView = dialogueListView.AddItem(itemText);
        dialogueListView.SetItemMetadata(dialogueRowIndexInPluginView, dialogue.ID);
      }
    }
  }

  private Dictionary<int, List<DialogueObject>> LoadDialoguesFromJson() {
    string fullJsonPath = ProjectSettings.GlobalizePath(DIALLOGUE_DB_JSON_PATH);
    if (!File.Exists(fullJsonPath)) {
      GD.PrintErr($"File not found: {fullJsonPath}");
      return new Dictionary<int, List<DialogueObject>>();
    }

    string jsonString = File.ReadAllText(fullJsonPath);
    return JSON2DialogueObjectParser.ExtractDialogueObjects(jsonString);
  }

  private async void OnAssociateButtonPressed() {
    var selectedDialogueIDs = GetSelectedDialogueIDs();
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

    UpdateDialogues(selectedDialogueIDs, new DialogueMediaObject {
      VisualPath = path,
      VisualPreDelay = preDelay,
      VisualPostDelay = postDelay
    });
  }

  private List<int> GetSelectedDialogueIDs() {
    return dialogueListView.GetSelectedItems()
        .Select(index => (int)dialogueListView.GetItemMetadata(index))
        .ToList();
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

  private void UpdateDialogues(List<int> dialogueIDs, DialogueMediaObject mediaInfo) {
    foreach (var dialogueID in dialogueIDs) {
      UpdateSingleDialogue(dialogueID, mediaInfo);
    }
    SaveDialoguesToJson();
    DialoguesMediaHandler.Save();
    RefreshDialogueList();
  }

  private void UpdateSingleDialogue(int dialogueID, DialogueMediaObject mediaInfo) {
    var dialogue = FindDialogueById(dialogueID);
    if (dialogue == null) {
      GD.PrintErr($"Dialogue with ID {dialogueID} not found.");
      return;
    }

    UpdateDialogueMedia(dialogue, mediaInfo);
    UpdateDialoguesMedia(dialogueID, mediaInfo);
  }

  private DialogueObject FindDialogueById(int dialogueID) {
    return conversationsAndDialoguesDict.Values
        .SelectMany(dialogues => dialogues)
        .FirstOrDefault(d => d.ID == dialogueID);
  }

  private void UpdateDialogueMedia(DialogueObject dialogue, DialogueMediaObject mediaInfo) {
    if (!string.IsNullOrEmpty(mediaInfo.VisualPath))
      dialogue.VisualPath = mediaInfo.VisualPath;
    if (mediaInfo.VisualPreDelay != 0)
      dialogue.VisualPreDelay = mediaInfo.VisualPreDelay;
    if (mediaInfo.VisualPostDelay != 0)
      dialogue.VisualPostDelay = mediaInfo.VisualPostDelay;

    // Only update music and sound fields if they were modified in the plugin
    if (!string.IsNullOrEmpty(mediaInfo.MusicPath))
      dialogue.MusicPath = mediaInfo.MusicPath;
    if (mediaInfo.MusicPreDelay != 0)
      dialogue.MusicPreDelay = mediaInfo.MusicPreDelay;
    if (mediaInfo.MusicPostDelay != 0)
      dialogue.MusicPostDelay = mediaInfo.MusicPostDelay;

    if (!string.IsNullOrEmpty(mediaInfo.SoundPath))
      dialogue.SoundPath = mediaInfo.SoundPath;
    if (mediaInfo.SoundPreDelay != 0)
      dialogue.SoundPreDelay = mediaInfo.SoundPreDelay;
    if (mediaInfo.SoundPostDelay != 0)
      dialogue.SoundPostDelay = mediaInfo.SoundPostDelay;
  }

  private void UpdateDialoguesMedia(int dialogueID, DialogueMediaObject mediaInfo) {
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
  private void SaveDialoguesToJson() {
    string fullJsonPath = ProjectSettings.GlobalizePath(DIALLOGUE_DB_JSON_PATH);
    string backupPath = fullJsonPath + ".backup";
    File.Copy(fullJsonPath, backupPath, true);

    try {
      string jsonString = File.ReadAllText(fullJsonPath);
      var updatedJson = UpdateJsonWithDialogues(jsonString);
      File.WriteAllText(fullJsonPath, updatedJson);
      GD.Print("JSON file updated successfully.");
    } catch (Exception e) {
      GD.PrintErr($"Error updating JSON file in SaveDialoguesToJson: {e.Message}");
      File.Copy(backupPath, fullJsonPath, true);
    }
  }

  private string UpdateJsonWithDialogues(string jsonString) {
    using var jsonDocument = JsonDocument.Parse(jsonString);
    var root = jsonDocument.RootElement;

    var conversationsArray = root.GetProperty("Assets").GetProperty("Conversations");
    var updatedConversations = new List<object>();

    foreach (var conversation in conversationsArray.EnumerateArray()) {
      var updatedConversation = UpdateConversation(conversation);
      updatedConversations.Add(updatedConversation);
    }

    //!what's this?
    var updatedRoot = new Dictionary<string, object> {
      ["Assets"] = new Dictionary<string, object> {
        ["Conversations"] = updatedConversations
      }
    };

    return JsonSerializer.Serialize(updatedRoot, EncodingJsonOptions);
  }

  private object UpdateConversation(JsonElement conversation) {
    int conversationId = conversation.GetProperty("ID").GetInt32();
    var dialogNodesArray = conversation.GetProperty("DialogNodes");
    var updatedDialogNodes = new List<object>();

    foreach (var dialogNode in dialogNodesArray.EnumerateArray()) {
      var updatedNode = UpdateDialogNode(dialogNode, conversationId);
      updatedDialogNodes.Add(updatedNode);
    }

    return new {
      ID = conversationId,
      DialogNodes = updatedDialogNodes
    };
  }

  private object UpdateDialogNode(JsonElement dialogNode, int conversationId) {
    int dialogId = dialogNode.GetProperty("ID").GetInt32();
    var fields = dialogNode.GetProperty("Fields");
    var updatedFields = JsonSerializer.Deserialize<Dictionary<string, object>>(fields.GetRawText());

    var dialogue = FindDialogueById(dialogId);
    if (dialogue != null) {
      UpdateFieldsFromDialogue(updatedFields, dialogue);
    }

    return new {
      OutgoingLinks = JsonSerializer.Deserialize<object>(dialogNode.GetProperty("OutgoingLinks").GetRawText()),
      ConversationID = conversationId,
      IsRoot = dialogNode.TryGetProperty("IsRoot", out var isRoot) ? isRoot.GetBoolean() : false,
      IsGroup = dialogue?.IsGroup ?? false,
      Fields = updatedFields,
      ID = dialogId
    };
  }

  private void UpdateFieldsFromDialogue(Dictionary<string, object> fields, DialogueObject dialogue) {
    fields["VisualPath"] = dialogue.VisualPath ?? "";
    fields["VisualPreDelay"] = dialogue.VisualPreDelay;
    fields["VisualPostDelay"] = dialogue.VisualPostDelay;
    fields["MusicPath"] = dialogue.MusicPath ?? "";
    fields["MusicPreDelay"] = dialogue.MusicPreDelay;
    fields["MusicPostDelay"] = dialogue.MusicPostDelay;
    fields["SoundPath"] = dialogue.SoundPath ?? "";
    fields["SoundPreDelay"] = dialogue.SoundPreDelay;
    fields["SoundPostDelay"] = dialogue.SoundPostDelay;
  }

  private void RefreshDialogueList() {
    PopulateDialogueList();
  }

  private async void OnAssociateMusicButtonPressed() {
    var selectedDialogueIDs = GetSelectedDialogueIDs();
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

    UpdateDialogues(selectedDialogueIDs, new DialogueMediaObject {
      MusicPath = path,
      MusicPreDelay = preDelay,
      MusicPostDelay = postDelay
    });
  }

  private async void OnAssociateSoundButtonPressed() {
    var selectedDialogueIDs = GetSelectedDialogueIDs();
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

    UpdateDialogues(selectedDialogueIDs, new DialogueMediaObject {
      SoundPath = path,
      SoundPreDelay = preDelay,
      SoundPostDelay = postDelay
    });
  }
}
#endif

