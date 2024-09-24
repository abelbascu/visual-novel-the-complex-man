#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;


[Tool]
public partial class visual_association_plugin : EditorPlugin {
  private Control dock;
  private ItemList dialogueListInPluginView;
  private Button associateButton;
  private Dictionary<int, List<DialogueObject>> conversationObjectsDB;
  private const string JSON_PATH = "res://DialogueDB/dialogueDB.json";
  private Button associateMusicButton;
  private Button associateSoundButton;

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  public override void _EnterTree() {
    GD.Print("VisualAssociationPlugin _EnterTree called");
    var scene = ResourceLoader.Load<PackedScene>("res://addons/visual_association/VisualAssociationDock.tscn");
    if (scene == null) {
      GD.PrintErr("Failed to load VisualAssociationDock scene");
      return;
    }

    dock = scene.Instantiate<Control>();
    if (dock == null) {
      GD.PrintErr("Failed to instantiate VisualAssociationDock scene");
      return;
    }

    dialogueListInPluginView = dock.GetNodeOrNull<ItemList>("VBoxContainer/ScrollContainer/DialogueList");
    associateButton = dock.GetNodeOrNull<Button>("VBoxContainer/AssociateButton");

    associateMusicButton = dock.GetNodeOrNull<Button>("VBoxContainer/AssociateMusicButton");
    associateSoundButton = dock.GetNodeOrNull<Button>("VBoxContainer/AssociateSoundButton");

    if (associateMusicButton != null) {
      associateMusicButton.Pressed += OnAssociateMusicButtonPressed;
    }
    if (associateSoundButton != null) {
      associateSoundButton.Pressed += OnAssociateSoundButtonPressed;
    }

    if (dialogueListInPluginView == null) GD.PrintErr("DialogueList not found");
    if (associateButton == null) GD.PrintErr("AssociateButton not found");

    if (associateButton != null) {
      associateButton.Pressed += OnAssociateButtonPressed;
    }

    if (dialogueListInPluginView != null) {
      dialogueListInPluginView.SelectMode = ItemList.SelectModeEnum.Multi;
    }

    VisualPathMappings.Load(); // Load existing mappings from json to dictionary
    CallDeferred(nameof(DeferredSetup));
  }


  private void InjectSavedVisualPaths() {
    foreach (var conversation in conversationObjectsDB) {
      foreach (var dialogue in conversation.Value) {

        if (VisualPathMappings.Mappings.TryGetValue(dialogue.ID, out MediaInfo mediaInfo)) {
          dialogue.VisualPath = mediaInfo.VisualPath;
          dialogue.VisualPreDelay = mediaInfo.VisualPreDelay;
          dialogue.VisualPostDelay = mediaInfo.VisualPostDelay;
          dialogue.MusicPath = mediaInfo.MusicPath;
          dialogue.MusicPreDelay = mediaInfo.MusicPreDelay;
          dialogue.MusicPostDelay = mediaInfo.MusicPostDelay;
          dialogue.SoundPath = mediaInfo.SoundPath;
          dialogue.SoundPreDelay = mediaInfo.SoundPreDelay;
          dialogue.SoundPostDelay = mediaInfo.SoundPostDelay;
        }
      }
    }
    SaveDialoguesToJson();
    GD.Print("[InjectSavedVisualPaths] Finished injecting saved visual paths to dialoguesDB.json");
  }

  private void DeferredSetup() {
    AddControlToBottomPanel(dock, "Visual Association");
    LoadDialogues();

    // Ensure the plugin is ready to handle file dialogs
    GetTree().CreateTimer(1.0).Timeout += () => {
      GD.Print("Plugin is ready for file dialogs");
    };
  }

  public override void _Ready() {
    base._Ready();
    // This ensures we have access to the EditorInterface
    if (EditorInterface.Singleton != null) {
      GD.Print("EditorInterface is available");
    } else {
      GD.PrintErr("EditorInterface is not available");
    }
  }

  public override void _ExitTree() {

    if (associateButton != null) {
      associateButton.Pressed += OnAssociateButtonPressed;
    }
    GD.Print("VisualAssociationPlugin _ExitTree called");
    if (dock != null) {
      RemoveControlFromBottomPanel(dock);
      dock.QueueFree();
    }
  }

  private void LoadDialogues() {
    GD.Print("LoadDialogues method called");
    if (dialogueListInPluginView == null) {
      GD.PrintErr("DialogueList is null, cannot load dialogues");
      return;
    }

    try {
      conversationObjectsDB = LoadDialoguesFromJson();
      GD.Print($"Loaded {conversationObjectsDB.Count} conversations");

      dialogueListInPluginView.Clear();
      foreach (var conversation in conversationObjectsDB) {
        foreach (var dialogue in conversation.Value) {
          string itemText = $"Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 120))}...";
          GD.Print($"Adding item: {itemText}");
          int dialogueRowIndexInPluginView = dialogueListInPluginView.AddItem(itemText);
          dialogueListInPluginView.SetItemMetadata(dialogueRowIndexInPluginView, dialogue.ID);
        }
      }
      GD.Print($"Total items added to dialogueListInPluginView: {dialogueListInPluginView.ItemCount}");
    } catch (Exception e) {
      GD.PrintErr($"Error in LoadDialogues: {e.Message}");
      GD.PrintErr(e.StackTrace);
    }

    InjectSavedVisualPaths();
  }

  private Dictionary<int, List<DialogueObject>> LoadDialoguesFromJson() {
    string fullJsonPath = ProjectSettings.GlobalizePath(JSON_PATH);
    GD.Print($"Attempting to load JSON from {fullJsonPath}");
    try {
      if (!File.Exists(fullJsonPath)) {
        GD.PrintErr($"File not found: {fullJsonPath}");
        return new Dictionary<int, List<DialogueObject>>();
      }

      string jsonString = File.ReadAllText(fullJsonPath);
      GD.Print($"JSON content length: {jsonString.Length}");
      var result = JSON2DialogueObjectParser.ExtractDialogueObjects(jsonString);
      GD.Print($"Extracted {result.Count} conversations");
      return result;
    } catch (Exception e) {
      GD.PrintErr($"Error loading dialogues from JSON: {e.Message}");
      GD.PrintErr(e.StackTrace);
      return new Dictionary<int, List<DialogueObject>>();
    }
  }

  public EditorFileDialog fileDialog;

  public async void OnAssociateButtonPressed() {
    var fileDialog = new FileDialog();
    fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
    fileDialog.AddFilter("*.png, *.jpg, *.jpeg ; Supported Images");
    fileDialog.MinSize = new Vector2I(800, 600); // Set minimum size

    GetTree().Root.AddChild(fileDialog);
    fileDialog.PopupCentered();

    var result = await ToSignal(fileDialog, "file_selected");
    string path = (string)result[0];

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

    float preDelay = float.TryParse(preDelayInput.Text, out float pd) ? pd : 0;
    float postDelay = float.TryParse(postDelayInput.Text, out float psd) ? psd : 0;

    var selectedIndices = dialogueListInPluginView.GetSelectedItems();
    var selectedIDs = new List<int>();
    foreach (int index in selectedIndices) {
      selectedIDs.Add((int)dialogueListInPluginView.GetItemMetadata(index));
    }
    UpdateDialogue(selectedIDs, path, preDelay, postDelay, null, null, null, null, null, null);
  }

  private async void OnAssociateMusicButtonPressed() {
    var fileDialog = new FileDialog();
    fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
    fileDialog.AddFilter("*.mp3, *.wav ; Supported Audio");
    fileDialog.MinSize = new Vector2I(800, 600); // Set minimum size

    GetTree().Root.AddChild(fileDialog);
    fileDialog.PopupCentered();

    var result = await ToSignal(fileDialog, "file_selected");
    string path = (string)result[0];

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

    float preDelay = float.TryParse(preDelayInput.Text, out float pd) ? pd : 0;
    float postDelay = float.TryParse(postDelayInput.Text, out float psd) ? psd : 0;

    var selectedIndices = dialogueListInPluginView.GetSelectedItems();
    var selectedIDs = new List<int>();
    foreach (int index in selectedIndices) {
      selectedIDs.Add((int)dialogueListInPluginView.GetItemMetadata(index));
    }
    UpdateDialogue(selectedIDs, null, null, null, path, preDelay, postDelay, null, null);
  }

  private async void OnAssociateSoundButtonPressed() {
    var fileDialog = new FileDialog();
    fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
    fileDialog.AddFilter("*.mp3, *.wav ; Supported Audio");
    fileDialog.MinSize = new Vector2I(800, 600); // Set minimum size

    GetTree().Root.AddChild(fileDialog);
    fileDialog.PopupCentered();

    var result = await ToSignal(fileDialog, "file_selected");
    string path = (string)result[0];

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

    float preDelay = float.TryParse(preDelayInput.Text, out float pd) ? pd : 0;
    float postDelay = float.TryParse(postDelayInput.Text, out float psd) ? psd : 0;

    var selectedIndices = dialogueListInPluginView.GetSelectedItems();
    var selectedIDs = new List<int>();
    foreach (int index in selectedIndices) {
      selectedIDs.Add((int)dialogueListInPluginView.GetItemMetadata(index));
    }
    UpdateDialogue(selectedIDs, null, null, null, null, null, null, path, preDelay, postDelay);
  }

  private void RefreshDialogueList() {
    dialogueListInPluginView.Clear();
    foreach (var conversation in conversationObjectsDB) {
      foreach (var dialogue in conversation.Value) {
        string itemText = $"Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 120))}...";
        dialogueListInPluginView.AddItem(itemText);
      }
    }
  }

  private void UpdateDialogue(List<int> dialogueIDs, string visualPath = null, float? visualPreDelay = null, float? visualPostDelay = null,
                              string musicPath = null, float? musicPreDelay = null, float? musicPostDelay = null,
                              string soundPath = null, float? soundPreDelay = null, float? soundPostDelay = null) {

    if (conversationObjectsDB == null) {
      GD.PrintErr("[UpdateDialogue] conversationObjectsDB is null. Cannot update dialogue.");
      return;
    }

    bool foundAny = false;

    foreach (int dialogueID in dialogueIDs) {
      bool found = false;

      foreach (var conversation in conversationObjectsDB) {
        if (conversation.Value == null) continue;

        var dialogue = conversation.Value.FirstOrDefault(d => d.ID == dialogueID);
        if (dialogue != null) {
          found = true;
          foundAny = true;

          // Update dialogue object
          if (visualPath != null) {
            dialogue.VisualPath = visualPath;
            dialogue.VisualPreDelay = visualPreDelay ?? dialogue.VisualPreDelay;
            dialogue.VisualPostDelay = visualPostDelay ?? dialogue.VisualPostDelay;
          }
          if (musicPath != null) {
            dialogue.MusicPath = musicPath;
            dialogue.MusicPreDelay = musicPreDelay ?? dialogue.MusicPreDelay;
            dialogue.MusicPostDelay = musicPostDelay ?? dialogue.MusicPostDelay;
          }
          if (soundPath != null) {
            dialogue.SoundPath = soundPath;
            dialogue.SoundPreDelay = soundPreDelay ?? dialogue.SoundPreDelay;
            dialogue.SoundPostDelay = soundPostDelay ?? dialogue.SoundPostDelay;
          }

          // Update VisualPathMappings
          if (VisualPathMappings.Mappings == null) {
            GD.PrintErr("[UpdateDialogue] VisualPathMappings.Mappings is null. Initializing new dictionary.");
            VisualPathMappings.Mappings = new Dictionary<int, MediaInfo>();
          }

          if (!VisualPathMappings.Mappings.TryGetValue(dialogue.ID, out var mediaInfo)) {
            mediaInfo = new MediaInfo();
            VisualPathMappings.Mappings[dialogue.ID] = mediaInfo;
          }

          if (visualPath != null) {
            mediaInfo.VisualPath = visualPath;
            mediaInfo.VisualPreDelay = visualPreDelay ?? mediaInfo.VisualPreDelay;
            mediaInfo.VisualPostDelay = visualPostDelay ?? mediaInfo.VisualPostDelay;
          }
          if (musicPath != null) {
            mediaInfo.MusicPath = musicPath;
            mediaInfo.MusicPreDelay = musicPreDelay ?? mediaInfo.MusicPreDelay;
            mediaInfo.MusicPostDelay = musicPostDelay ?? mediaInfo.MusicPostDelay;
          }
          if (soundPath != null) {
            mediaInfo.SoundPath = soundPath;
            mediaInfo.SoundPreDelay = soundPreDelay ?? mediaInfo.SoundPreDelay;
            mediaInfo.SoundPostDelay = soundPostDelay ?? mediaInfo.SoundPostDelay;
          }

          GD.Print($"[UpdateDialogue] Updated dialogue {dialogue.ID} with new media information");
          GD.Print($"Dialogue ID {dialogue.ID} updated in VisualPathMappings:");
          GD.Print($"  VisualPath: {mediaInfo.VisualPath}, PreDelay: {mediaInfo.VisualPreDelay}, PostDelay: {mediaInfo.VisualPostDelay}");
          GD.Print($"  MusicPath: {mediaInfo.MusicPath}, PreDelay: {mediaInfo.MusicPreDelay}, PostDelay: {mediaInfo.MusicPostDelay}");
          GD.Print($"  SoundPath: {mediaInfo.SoundPath}, PreDelay: {mediaInfo.SoundPreDelay}, PostDelay: {mediaInfo.SoundPostDelay}");
          break;
          //return;
        }
      }
      if (!found) {
        GD.PrintErr($"[UpdateDialogue] Dialogue at index {dialogueID} not found.");
      }
    }
    if (!foundAny) {
      GD.PrintErr("[UpdateDialogue] No dialogue found for the given indices.");
    }
    SaveDialoguesToJson();
    VisualPathMappings.Save();
    RefreshDialogueList();

    //GD.PrintErr($"[UpdateDialogue] Dialogue at index {index} not found.");
  }



  private void SaveDialoguesToJson() {
    string fullJsonPath = ProjectSettings.GlobalizePath(JSON_PATH);
    string backupPath = fullJsonPath + ".backup";
    GD.Print($"Attempting to save JSON to: {fullJsonPath}");

    File.Copy(fullJsonPath, backupPath, true);
    try {
      string jsonString = File.ReadAllText(fullJsonPath);
      using var jsonDocument = JsonDocument.Parse(jsonString);
      var root = jsonDocument.RootElement;

      var conversationsArray = root.GetProperty("Assets").GetProperty("Conversations");
      var updatedConversations = new List<object>();

      for (int i = 0; i < conversationsArray.GetArrayLength(); i++) {
        var conversation = conversationsArray[i];
        int conversationId = conversation.GetProperty("ID").GetInt32();

        if (conversationObjectsDB.TryGetValue(conversationId, out var dialogues)) {
          var dialogNodesArray = conversation.GetProperty("DialogNodes");
          var updatedDialogNodes = new List<object>();

          for (int j = 0; j < dialogNodesArray.GetArrayLength(); j++) {
            var dialogNode = dialogNodesArray[j];
            int dialogId = dialogNode.GetProperty("ID").GetInt32();

            var dialogue = dialogues.Find(d => d.ID == dialogId);
            if (dialogue != null) {
              var fields = dialogNode.GetProperty("Fields");
              var updatedFields = new Dictionary<string, object>();
              foreach (var field in fields.EnumerateObject()) {
                // Deserialize the field value to remove extra quotes
                updatedFields[field.Name] = JsonSerializer.Deserialize<object>(field.Value.GetRawText());
              }

              // Update fields
              updatedFields["VisualPath"] = dialogue.VisualPath ?? "";
              updatedFields["MusicPath"] = dialogue.MusicPath ?? "";
              updatedFields["MusicPreDelay"] = dialogue.MusicPreDelay;
              updatedFields["MusicPostDelay"] = dialogue.MusicPostDelay;
              updatedFields["SoundPath"] = dialogue.SoundPath ?? "";
              updatedFields["SoundPreDelay"] = dialogue.SoundPreDelay;
              updatedFields["SoundPostDelay"] = dialogue.SoundPostDelay;

              var updatedDialogNode = new {
                OutgoingLinks = JsonSerializer.Deserialize<object>(dialogNode.GetProperty("OutgoingLinks").GetRawText()),
                ConversationID = conversationId,
                IsRoot = dialogNode.TryGetProperty("IsRoot", out var isRoot) ? isRoot.GetBoolean() : false,
                IsGroup = dialogue.IsGroup,
                Fields = updatedFields,
                ID = dialogId
              };

              updatedDialogNodes.Add(updatedDialogNode);
            } else {
              updatedDialogNodes.Add(JsonSerializer.Deserialize<object>(dialogNode.GetRawText()));
            }
          }

          var updatedConversation = new {
            ID = conversationId,
            DialogNodes = updatedDialogNodes
          };

          updatedConversations.Add(updatedConversation);
        } else {
          updatedConversations.Add(JsonSerializer.Deserialize<object>(conversation.GetRawText()));
        }
      }

      var updatedAssets = new {
        Conversations = updatedConversations
      };

      var updatedRoot = new {
        Assets = updatedAssets
      };

      string updatedJsonString = JsonSerializer.Serialize(updatedRoot, JsonOptions);
      File.WriteAllText(fullJsonPath, updatedJsonString);


      if (new FileInfo(fullJsonPath).Length == 0) {
        throw new Exception("Save resulted in empty file");
      } else {
        GD.Print("JSON file updated successfully.");
      }

      VisualPathMappings.Save(); // Save mappings to a separate file
    } catch (Exception e) {
      GD.PrintErr($"Error updating JSON file: {e.Message}");
      GD.PrintErr(e.StackTrace);
      GD.PrintErr("Reverting to backup dialogueDB.json...");

      File.Copy(backupPath, fullJsonPath, true);

    }
  }

  private void InjectVisualPaths(string jsonPath) {
    try {
      string jsonString = File.ReadAllText(jsonPath);
      using var jsonDocument = JsonDocument.Parse(jsonString);
      var root = jsonDocument.RootElement;

      var conversationsArray = root.GetProperty("Assets").GetProperty("Conversations");
      var updatedConversations = new List<object>();

      foreach (var conversation in conversationsArray.EnumerateArray()) {
        var dialogNodes = conversation.GetProperty("DialogNodes");
        var updatedDialogNodes = new List<object>();

        foreach (var node in dialogNodes.EnumerateArray()) {
          var id = node.GetProperty("ID").GetInt32();
          var fields = node.GetProperty("Fields");
          var updatedFields = JsonSerializer.Deserialize<Dictionary<string, object>>(fields.GetRawText());

          if (VisualPathMappings.Mappings.TryGetValue(id, out MediaInfo mediaInfo)) {
            updatedFields["VisualPath"] = mediaInfo.VisualPath;
            updatedFields["VisualPreDelay"] = mediaInfo.VisualPreDelay;
            updatedFields["VisualPostDelay"] = mediaInfo.VisualPostDelay;
            updatedFields["MusicPath"] = mediaInfo.MusicPath;
            updatedFields["MusicPreDelay"] = mediaInfo.MusicPreDelay;
            updatedFields["MusicPostDelay"] = mediaInfo.MusicPostDelay;
            updatedFields["SoundPath"] = mediaInfo.SoundPath;
            updatedFields["SoundPreDelay"] = mediaInfo.SoundPreDelay;
            updatedFields["SoundPostDelay"] = mediaInfo.SoundPostDelay;

            GD.Print($"Injected VisualPath for dialogue ID {id}: {mediaInfo.VisualPath}");
            GD.Print($"Injected VisualPreDelay for dialogue ID {id}: {mediaInfo.VisualPreDelay}");

            var updatedNode = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText());
            updatedNode["Fields"] = updatedFields;
            updatedDialogNodes.Add(updatedNode);
          }

          var updatedConversation = JsonSerializer.Deserialize<Dictionary<string, object>>(conversation.GetRawText());
          updatedConversation["DialogNodes"] = updatedDialogNodes;
          updatedConversations.Add(updatedConversation);
        }

        var updatedRoot = new Dictionary<string, object> {
          ["Assets"] = new Dictionary<string, object> {
            ["Conversations"] = updatedConversations
          }
        };

        string updatedJsonString = JsonSerializer.Serialize(updatedRoot, JsonOptions);
        File.WriteAllText(jsonPath, updatedJsonString);
        GD.Print("Visual paths injected successfully.");
      }

    } catch (Exception e) {
      GD.PrintErr($"Error injecting visual paths: {e.Message}");
    }
  }

  // Add a method to handle reloading the JSON file
  public void ReloadJSON() {
    LoadDialogues();
    string fullJsonPath = ProjectSettings.GlobalizePath(JSON_PATH);
    InjectVisualPaths(fullJsonPath);
  }
}
#endif