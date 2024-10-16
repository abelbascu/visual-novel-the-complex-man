using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public class DialogueMediaObject {
  public int ConversationID { get; set; }
  public string VisualPath { get; set; }
  public float VisualPreDelay { get; set; }
  public float VisualPostDelay { get; set; }
  public string MusicPath { get; set; }
  public float MusicPreDelay { get; set; }
  public float MusicPostDelay { get; set; }
  public string SoundPath { get; set; }
  public float SoundPreDelay { get; set; }
  public float SoundPostDelay { get; set; }
}

public static class DialoguesMediaHandler {
  private static Dictionary<int, DialogueMediaObject> _mappings = new Dictionary<int, DialogueMediaObject>();
  public static Dictionary<int, Dictionary<int, DialogueMediaObject>> AllMediaObjects { get; set; } = new Dictionary<int, Dictionary<int, DialogueMediaObject>>();

  private const string JSON_PATH = "res://DialogueDB/dialogueMediaDB.json";

  //becasue it's a static class, as soon as the godot editor is opened
  //it loads the mappings from the dialogueMediaDB.json file
  static DialoguesMediaHandler() {
    Load();
  }

  public static void Load() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      if (File.Exists(fullPath)) {
        string jsonString = File.ReadAllText(fullPath);
        AllMediaObjects = JsonSerializer.Deserialize<Dictionary<int, Dictionary<int, DialogueMediaObject>>>(jsonString);
      }
    } catch (Exception e) {
      GD.PrintErr($"[DialoguesMediaHandler] Error loading mappings: {e.Message}");
      AllMediaObjects = new Dictionary<int, Dictionary<int, DialogueMediaObject>>();
    }
  }

  public static void Save() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

      string jsonString = JsonSerializer.Serialize(AllMediaObjects, new JsonSerializerOptions {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
      });
      File.WriteAllText(fullPath, jsonString);
      GD.Print($"[DialoguesMediaHandler] Successfully saved {AllMediaObjects.Count} conversations");
    } catch (Exception e) {
      GD.PrintErr($"[DialoguesMediaHandler] Error saving mappings: {e.Message}");
    }
  }
}