using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

[Tool]
public partial class visual_association_plugin : EditorPlugin
{
    private Control dock;
    private ItemList dialogueList;
    private Button associateButton;
    private OptionButton visualTypeOption;
    private Dictionary<int, List<DialogueObject>> conversationObjectsDB;
    private const string JSON_PATH = "res://DialogueDB/dialogueDB.json";

    public override void _EnterTree()
    {
        dock = ResourceLoader.Load<PackedScene>("res://addons/visual_association/VisualAssociationDock.tscn").Instantiate<Control>();
        AddControlToDock(DockSlot.LeftUl, dock);

        dialogueList = dock.GetNode<ItemList>("VBoxContainer/DialogueList");
        associateButton = dock.GetNode<Button>("VBoxContainer/AssociateButton");
        visualTypeOption = dock.GetNode<OptionButton>("VBoxContainer/VisualTypeOption");

        associateButton.Pressed += OnAssociateButtonPressed;

        visualTypeOption.AddItem("Image", 0);
        visualTypeOption.AddItem("Video", 1);

        LoadDialogues();
    }

    public override void _ExitTree()
    {
        RemoveControlFromDocks(dock);
        dock.Free();
    }

    private void LoadDialogues()
    {
        conversationObjectsDB = LoadDialoguesFromJson();
        foreach (var conversation in conversationObjectsDB)
        {
            foreach (var dialogue in conversation.Value)
            {
                string itemText = "Conv {conversation.Key} - Dialogue {dialogue.ID}: {dialogue.DialogueTextDefault.Substring(0, Math.Min(dialogue.DialogueTextDefault.Length, 30))}...";
                GD.Print($"Adding item: {itemText}");
            dialogueList.AddItem(itemText);
            }
        }
            GD.Print($"Total items added to dialogueList: {dialogueList.ItemCount}");

    }

    private void OnAssociateButtonPressed()
    {
        var selectedItems = dialogueList.GetSelectedItems();
        if (selectedItems.Length == 0) return;

        FileDialog fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.AddFilter("*.png ; PNG Images");
        fileDialog.AddFilter("*.jpg ; JPEG Images");
        fileDialog.AddFilter("*.mp4 ; MP4 Videos");

        AddChild(fileDialog);
        fileDialog.PopupCentered(new Vector2I(800, 600));

        fileDialog.FileSelected += (string path) =>
        {
            foreach (int index in selectedItems)
            {
                UpdateDialogue(index, path, visualTypeOption.Selected);
            }
            SaveDialoguesToJson();
        };
    }

    private void UpdateDialogue(int index, string visualPath, int visualType)
    {
        int dialogueIndex = 0;

        foreach (var conversation in conversationObjectsDB)
        {
            foreach (var dialogue in conversation.Value)
            {
                if (dialogueIndex == index)
                {
                    dialogue.VisualPath = visualPath;
                    dialogue.VisualType = visualType;
                    return;
                }
                dialogueIndex++;
            }
        }
    }

 private void SaveDialoguesToJson()
{
    string jsonString = File.ReadAllText(JSON_PATH);
    using var jsonDocument = JsonDocument.Parse(jsonString);
    var root = jsonDocument.RootElement;

    var conversationsArray = root.GetProperty("Assets").GetProperty("Conversations");
    var updatedConversations = new List<object>();

    for (int i = 0; i < conversationsArray.GetArrayLength(); i++)
    {
        var conversation = conversationsArray[i];
        int conversationId = conversation.GetProperty("ID").GetInt32();

        if (conversationObjectsDB.TryGetValue(conversationId, out var dialogues))
        {
            var dialogNodesArray = conversation.GetProperty("DialogNodes");
            var updatedDialogNodes = new List<object>();

            for (int j = 0; j < dialogNodesArray.GetArrayLength(); j++)
            {
                var dialogNode = dialogNodesArray[j];
                int dialogId = dialogNode.GetProperty("ID").GetInt32();

                var dialogue = dialogues.Find(d => d.ID == dialogId);
                if (dialogue != null && !string.IsNullOrEmpty(dialogue.VisualPath))
                {
                    var fields = dialogNode.GetProperty("Fields");
                    var updatedFields = new Dictionary<string, object>();
                    foreach (var field in fields.EnumerateObject())
                    {
                        updatedFields[field.Name] = field.Value.GetRawText();
                    }
                    updatedFields["VisualPath"] = dialogue.VisualPath;
                    updatedFields["VisualType"] = dialogue.VisualType;

                    var updatedDialogNode = new
                    {
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
                }
                else
                {
                    updatedDialogNodes.Add(JsonSerializer.Deserialize<object>(dialogNode.GetRawText()));
                }
            }

            var updatedConversation = new
            {
                ID = conversationId,
                DialogNodes = updatedDialogNodes,
                NodeColor = conversation.GetProperty("NodeColor").GetInt32(),
                Fields = JsonSerializer.Deserialize<object>(conversation.GetProperty("Fields").GetRawText())
            };

            updatedConversations.Add(updatedConversation);
        }
        else
        {
            updatedConversations.Add(JsonSerializer.Deserialize<object>(conversation.GetRawText()));
        }
    }

    var updatedAssets = new
    {
        Actors = JsonSerializer.Deserialize<object>(root.GetProperty("Assets").GetProperty("Actors").GetRawText()),
        Items = JsonSerializer.Deserialize<object>(root.GetProperty("Assets").GetProperty("Items").GetRawText()),
        Locations = JsonSerializer.Deserialize<object>(root.GetProperty("Assets").GetProperty("Locations").GetRawText()),
        Conversations = updatedConversations
    };

    var updatedRoot = new
    {
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

    private Dictionary<int, List<DialogueObject>> LoadDialoguesFromJson()
    {
        string jsonString = File.ReadAllText(JSON_PATH);
        return JSON2DialogueObjectParser.ExtractDialogueObjects(jsonString);
    }
}