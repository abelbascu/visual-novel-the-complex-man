using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
//using Godot.Editor;
//using Scripts;
//using Scripts.Helpers;

[Tool]
public partial class visual_association_plugin : EditorPlugin {
    private Control dock;
    private ItemList dialogueList;
    private Button associateButton;
    private OptionButton visualTypeOption;
    private Dictionary<int, List<DialogueObject>> conversationObjectsDB;
    private const string JSON_PATH = "res://DialogueDB/dialogueDB.json";

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

        dialogueList = dock.GetNodeOrNull<ItemList>("VBoxContainer/ScrollContainer/DialogueList");
        associateButton = dock.GetNodeOrNull<Button>("VBoxContainer/AssociateButton");
        visualTypeOption = dock.GetNodeOrNull<OptionButton>("VBoxContainer/VisualTypeOption");

        if (dialogueList == null) GD.PrintErr("DialogueList not found");
        if (associateButton == null) GD.PrintErr("AssociateButton not found");
        if (visualTypeOption == null) GD.PrintErr("VisualTypeOption not found");

        if (associateButton != null) {
            associateButton.Pressed += OnAssociateButtonPressed;
        }

        if (visualTypeOption != null) {
            visualTypeOption.AddItem("Image", 0);
            visualTypeOption.AddItem("Video", 1);
        }

        if (dialogueList != null) {
            dialogueList.SelectMode = ItemList.SelectModeEnum.Multi;
        }

        CallDeferred(nameof(DeferredSetup));
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
        GD.Print("VisualAssociationPlugin _ExitTree called");
        if (dock != null) {
            RemoveControlFromBottomPanel(dock);
            dock.QueueFree();
        }
    }

    private void LoadDialogues() {
        GD.Print("LoadDialogues method called");
        if (dialogueList == null) {
            GD.PrintErr("DialogueList is null, cannot load dialogues");
            return;
        }

        try {
            conversationObjectsDB = LoadDialoguesFromJson();
            GD.Print($"Loaded {conversationObjectsDB.Count} conversations");

            dialogueList.Clear();
            foreach (var conversation in conversationObjectsDB) {
                foreach (var dialogue in conversation.Value) {
                    string itemText = $"Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 120))}...";
                    GD.Print($"Adding item: {itemText}");
                    dialogueList.AddItem(itemText);
                }
            }
            GD.Print($"Total items added to dialogueList: {dialogueList.ItemCount}");
        } catch (Exception e) {
            GD.PrintErr($"Error in LoadDialogues: {e.Message}");
            GD.PrintErr(e.StackTrace);
        }
    }

    private Dictionary<int, List<DialogueObject>> LoadDialoguesFromJson() {
        GD.Print($"Attempting to load JSON from {JSON_PATH}");
        try {
            string projectRoot = ProjectSettings.GlobalizePath("res://");
            string fullPath = Path.Combine(projectRoot, JSON_PATH.TrimStart("res://".ToCharArray()));
            GD.Print($"Full path: {fullPath}");

            if (!File.Exists(fullPath)) {
                GD.PrintErr($"File not found: {fullPath}");
                return new Dictionary<int, List<DialogueObject>>();
            }

            string jsonString = File.ReadAllText(fullPath);
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

    private EditorFileDialog fileDialog;

    private void OnAssociateButtonPressed() {
        var selectedItems = dialogueList.GetSelectedItems();
        if (selectedItems.Length == 0) return;

        if (EditorInterface.Singleton == null) {
            GD.PrintErr("EditorInterface is not available");
            return;
        }

        fileDialog = new EditorFileDialog();
        fileDialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
        fileDialog.AddFilter("*.png ; PNG Images");
        fileDialog.AddFilter("*.jpg ; JPEG Images");
        fileDialog.AddFilter("*.mp4 ; MP4 Videos");

        fileDialog.FileSelected += OnFileSelected;
        fileDialog.Canceled += OnFileDialogCanceled;

        AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));
    }

    private void OnFileSelected(string path) {
        GD.Print($"File selected: {path}");
        var selectedItems = dialogueList.GetSelectedItems();
        foreach (int index in selectedItems) {
            UpdateDialogue(index, path, visualTypeOption.Selected);
        }
        SaveDialoguesToJson();
        RemoveFileDialog();
    }

    private void OnFileDialogCanceled() {
        GD.Print("File selection canceled");
        RemoveFileDialog();
    }

    private void RemoveFileDialog() {
        if (fileDialog != null) {
            fileDialog.QueueFree();
            fileDialog = null;
        }
    }

    private void UpdateDialogue(int index, string visualPath, int visualType) {
        int dialogueIndex = 0;

        foreach (var conversation in conversationObjectsDB) {
            foreach (var dialogue in conversation.Value) {
                if (dialogueIndex == index) {
                    dialogue.VisualPath = visualPath;
                    dialogue.VisualType = visualType;
                    return;
                }
                dialogueIndex++;
            }
        }
    }

    private void SaveDialoguesToJson() {
        string jsonString = File.ReadAllText(JSON_PATH);
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
                    if (dialogue != null && !string.IsNullOrEmpty(dialogue.VisualPath)) {
                        var fields = dialogNode.GetProperty("Fields");
                        var updatedFields = new Dictionary<string, object>();
                        foreach (var field in fields.EnumerateObject()) {
                            updatedFields[field.Name] = field.Value.GetRawText();
                        }
                        updatedFields["VisualPath"] = dialogue.VisualPath;
                        updatedFields["VisualType"] = dialogue.VisualType;

                        var updatedDialogNode = new {
                            OutgoingLinks = JsonSerializer.Deserialize<object>(dialogNode.GetProperty("OutgoingLinks").GetRawText()),
                            ConversationID = dialogNode.GetProperty("ConversationID").GetInt32(),
                            IsRoot = dialogNode.GetProperty("IsRoot").GetBoolean(),
                            IsGroup = dialogNode.GetProperty("IsGroup").GetBoolean(),
                            ConditionsString = dialogNode.GetProperty("ConditionsString").GetString(),
                            UserScript = dialogNode.GetProperty("UserScript").GetString(),
                            NodeColor = dialogNode.GetProperty("NodeColor").GetInt32(),
                            DelaySimStatus = dialogNode.GetProperty("DelaySimStatus").GetBoolean(),
                            FalseConditionAction = dialogNode.GetProperty("FalseConditionAction").GetInt32(),
                            ConditionPriority = dialogNode.GetProperty("ConditionPriority").GetInt32(),
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
                    DialogNodes = updatedDialogNodes,
                    NodeColor = conversation.GetProperty("NodeColor").GetInt32(),
                    Fields = JsonSerializer.Deserialize<object>(conversation.GetProperty("Fields").GetRawText())
                };

                updatedConversations.Add(updatedConversation);
            } else {
                updatedConversations.Add(JsonSerializer.Deserialize<object>(conversation.GetRawText()));
            }
        }

        var updatedAssets = new {
            Actors = JsonSerializer.Deserialize<object>(root.GetProperty("Assets").GetProperty("Actors").GetRawText()),
            Items = JsonSerializer.Deserialize<object>(root.GetProperty("Assets").GetProperty("Items").GetRawText()),
            Locations = JsonSerializer.Deserialize<object>(root.GetProperty("Assets").GetProperty("Locations").GetRawText()),
            Conversations = updatedConversations
        };

        var updatedRoot = new {
            Language = root.GetProperty("Language").GetString(),
            Title = root.GetProperty("Title").GetString(),
            Version = root.GetProperty("Version").GetString(),
            Author = root.GetProperty("Author").GetString(),
            Description = root.GetProperty("Description").GetString(),
            UserScript = root.GetProperty("UserScript").GetRawText(),
            Assets = updatedAssets,
            UserVariables = JsonSerializer.Deserialize<object>(root.GetProperty("UserVariables").GetRawText())
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string updatedJsonString = JsonSerializer.Serialize(updatedRoot, options);
        File.WriteAllText(JSON_PATH, updatedJsonString);
    }

    // private Dictionary<int, List<DialogueObject>> LoadDialoguesFromJson() {
    //     string jsonString = File.ReadAllText(JSON_PATH);
    //     return JSON2DialogueObjectParser.ExtractDialogueObjects(jsonString);
    // }
}