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
  private Control mainDockInEditor;
  private ItemList dialogueListInPluginView;
  private Dictionary<int, List<DialogueObject>> conversationObjectsDB;
  private const string JSON_PATH = "res://DialogueDB/dialogueDB.json";
  private Button setImageButton;
  private Button setMusicButton;
  private Button setSoundButton;

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  // public override void _Ready() {
  //   GetTree().Root.Connect("focus_entered", new Callable(this, nameof(OnEditorFocusEntered)));
  // }

  // private void OnEditorFocusEntered() {
  //   if (!Engine.IsEditorHint()) {
  //     return;
  //   }
  //   ReinitializePlugin();
  // }

  // private void ReinitializePlugin() {
  //   CleanupPlugin();
  //   InitializeMainDockInEditor();
  //   VisualPathMappings.Load();
  //   AddMainDockInEditorToBottom();
  //   LoadDialogues();
  //   InjectSavedVisualPaths();
  // }

  public override void _EnterTree() {
    CleanupPlugin();
    InitializeMainDockInEditor();
    // EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();
    // settings.SetSetting("debugger/auto_switch_to_remote_scene_tree", false);
    CallDeferred(nameof(AddMainDockInEditorToBottom));
    CallDeferred(nameof(LoadDialogues));
    CallDeferred(nameof(InjectSavedVisualPaths));
  }

  public override void _ExitTree() {
    CleanupPlugin();
  }


  private void CleanupPlugin() {
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
    if (mainDockInEditor != null) {
      RemoveControlFromBottomPanel(mainDockInEditor);
      mainDockInEditor.QueueFree();
      mainDockInEditor = null;
    }
  }

  private void InitializeMainDockInEditor() {
    var scene = ResourceLoader.Load<PackedScene>("res://addons/visual_association/VisualAssociationDock.tscn");
    mainDockInEditor = scene?.Instantiate<Control>();
    if (mainDockInEditor == null) {
      GD.PrintErr("Failed to load or instantiate VisualAssociationDock scene");
      return;
    }

    dialogueListInPluginView = mainDockInEditor.GetNodeOrNull<ItemList>("VBoxContainer/ScrollContainer/DialogueList");
    setImageButton = mainDockInEditor.GetNodeOrNull<Button>("VBoxContainer/AssociateButton");
    setMusicButton = mainDockInEditor.GetNodeOrNull<Button>("VBoxContainer/AssociateMusicButton");
    setSoundButton = mainDockInEditor.GetNodeOrNull<Button>("VBoxContainer/AssociateSoundButton");

    SetupButtons();
  }

  private void SetupButtons() {
    if (setImageButton != null) setImageButton.Pressed += OnAssociateButtonPressed;
    if (setMusicButton != null) setMusicButton.Pressed += OnAssociateMusicButtonPressed;
    if (setSoundButton != null) setSoundButton.Pressed += OnAssociateSoundButtonPressed;
    if (dialogueListInPluginView != null) dialogueListInPluginView.SelectMode = ItemList.SelectModeEnum.Multi;
  }

  private void AddMainDockInEditorToBottom() {
    if (mainDockInEditor != null) {
      AddControlToBottomPanel(mainDockInEditor, "Visual Association");
    }
  }

  private void LoadDialogues() {
    if (dialogueListInPluginView == null) {
      GD.PrintErr("DialogueList is null, cannot load dialogues");
      return;
    }
    try {
      conversationObjectsDB = LoadDialoguesFromJson();
      PopulateDialogueList();
    } catch (Exception e) {
      GD.PrintErr($"Error in LoadDialogues: {e.Message}");
      GD.PrintErr(e.StackTrace);
    }
  }

  private void InjectSavedVisualPaths() {
    try {
      foreach (var conversation in conversationObjectsDB.Values) {
        foreach (var dialogue in conversation) {
          if (VisualPathMappings.Mappings.TryGetValue(dialogue.ID, out DialogueObjectMediaInfo mediaInfo)) {
            UpdateDialogueFields(dialogue, mediaInfo);
          }
        }
      }
    } catch (Exception e) {
      GD.PrintErr($"Error in InjectSavedVisualPaths {e.Message}");
      GD.PrintErr(e.StackTrace);
    }
    SaveDialoguesToJson();
    GD.Print("[InjectSavedVisualPaths] Finished injecting saved visual paths to dialoguesDB.json");
  }

  private void PopulateDialogueList() {
    dialogueListInPluginView.Clear();
    foreach (var conversation in conversationObjectsDB) {
      foreach (var dialogue in conversation.Value) {
        string itemText = $"Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 120))}...";
        int dialogueRowIndexInPluginView = dialogueListInPluginView.AddItem(itemText);
        dialogueListInPluginView.SetItemMetadata(dialogueRowIndexInPluginView, dialogue.ID);
      }
    }
  }

  private Dictionary<int, List<DialogueObject>> LoadDialoguesFromJson() {
    string fullJsonPath = ProjectSettings.GlobalizePath(JSON_PATH);
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

    UpdateDialogues(selectedDialogueIDs, new DialogueObjectMediaInfo {
      VisualPath = path,
      VisualPreDelay = preDelay,
      VisualPostDelay = postDelay
    });
  }

  private List<int> GetSelectedDialogueIDs() {
    return dialogueListInPluginView.GetSelectedItems()
        .Select(index => (int)dialogueListInPluginView.GetItemMetadata(index))
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

  private void UpdateDialogues(List<int> dialogueIDs, DialogueObjectMediaInfo mediaInfo) {
    foreach (var dialogueID in dialogueIDs) {
      UpdateSingleDialogue(dialogueID, mediaInfo);
    }
    SaveDialoguesToJson();
    VisualPathMappings.Save();
    RefreshDialogueList();
  }

  private void UpdateSingleDialogue(int dialogueID, DialogueObjectMediaInfo mediaInfo) {
    var dialogue = FindDialogueById(dialogueID);
    if (dialogue == null) {
      GD.PrintErr($"Dialogue with ID {dialogueID} not found.");
      return;
    }

    UpdateDialogueFields(dialogue, mediaInfo);
    UpdateVisualPathMappings(dialogueID, mediaInfo);
  }

  private DialogueObject FindDialogueById(int dialogueID) {
    return conversationObjectsDB.Values
        .SelectMany(dialogues => dialogues)
        .FirstOrDefault(d => d.ID == dialogueID);
  }

  private void UpdateDialogueFields(DialogueObject dialogue, DialogueObjectMediaInfo mediaInfo) {
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

  private void UpdateVisualPathMappings(int dialogueID, DialogueObjectMediaInfo mediaInfo) {
    if (!VisualPathMappings.Mappings.TryGetValue(dialogueID, out var existingDialogueObjectMediaInfo)) {
      existingDialogueObjectMediaInfo = new DialogueObjectMediaInfo();
      VisualPathMappings.Mappings[dialogueID] = existingDialogueObjectMediaInfo;
    }

    if (!string.IsNullOrEmpty(mediaInfo.VisualPath))
      existingDialogueObjectMediaInfo.VisualPath = mediaInfo.VisualPath;
    if (mediaInfo.VisualPreDelay != 0)
      existingDialogueObjectMediaInfo.VisualPreDelay = mediaInfo.VisualPreDelay;
    if (mediaInfo.VisualPostDelay != 0)
      existingDialogueObjectMediaInfo.VisualPostDelay = mediaInfo.VisualPostDelay;

    if (!string.IsNullOrEmpty(mediaInfo.MusicPath))
      existingDialogueObjectMediaInfo.MusicPath = mediaInfo.MusicPath;
    if (mediaInfo.MusicPreDelay != 0)
      existingDialogueObjectMediaInfo.MusicPreDelay = mediaInfo.MusicPreDelay;
    if (mediaInfo.MusicPostDelay != 0)
      existingDialogueObjectMediaInfo.MusicPostDelay = mediaInfo.MusicPostDelay;

    if (!string.IsNullOrEmpty(mediaInfo.SoundPath))
      existingDialogueObjectMediaInfo.SoundPath = mediaInfo.SoundPath;
    if (mediaInfo.SoundPreDelay != 0)
      existingDialogueObjectMediaInfo.SoundPreDelay = mediaInfo.SoundPreDelay;
    if (mediaInfo.SoundPostDelay != 0)
      existingDialogueObjectMediaInfo.SoundPostDelay = mediaInfo.SoundPostDelay;
  }
  private void SaveDialoguesToJson() {
    string fullJsonPath = ProjectSettings.GlobalizePath(JSON_PATH);
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

    var updatedRoot = new Dictionary<string, object> {
      ["Assets"] = new Dictionary<string, object> {
        ["Conversations"] = updatedConversations
      }
    };

    return JsonSerializer.Serialize(updatedRoot, JsonOptions);
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

    UpdateDialogues(selectedDialogueIDs, new DialogueObjectMediaInfo {
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

    UpdateDialogues(selectedDialogueIDs, new DialogueObjectMediaInfo {
      SoundPath = path,
      SoundPreDelay = preDelay,
      SoundPostDelay = postDelay
    });
  }
}
#endif

