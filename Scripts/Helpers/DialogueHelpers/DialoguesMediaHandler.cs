using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public class DialogueMediaObject {
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
  public static Dictionary<int, DialogueMediaObject> AllMediaObjects {
    get {
      if (_mappings == null) {
        _mappings = new Dictionary<int, DialogueMediaObject>();
        Load();
      }
      return _mappings;
    }
    set { _mappings = value; }
  }

  private const string JSON_PATH = "res://DialogueDB/dialogueMediaDB.json";

  static DialoguesMediaHandler() {
    Load();
  }

  public static void Load() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      GD.Print($"[DialoguesMediaHandler] Attempting to load mappings from: {fullPath}");

      if (File.Exists(fullPath)) {
        string jsonString = File.ReadAllText(fullPath);
        var loadedMappings = JsonSerializer.Deserialize<Dictionary<int, DialogueMediaObject>>(jsonString);
        if (loadedMappings != null) {
          AllMediaObjects = loadedMappings;
          GD.Print($"[DialoguesMediaHandler] Successfully loaded {AllMediaObjects.Count} mappings");
        } else {
          GD.PrintErr("[DialoguesMediaHandler] Loaded mappings were null, initializing empty dictionary");
          AllMediaObjects = new Dictionary<int, DialogueMediaObject>();
        }
      } else {
        GD.Print("[DialoguesMediaHandler] AllMediaObjects file does not exist, initializing empty dictionary");
        AllMediaObjects = new Dictionary<int, DialogueMediaObject>();
      }
    } catch (Exception e) {
      GD.PrintErr($"[DialoguesMediaHandler] Error loading mappings: {e.Message}");
      AllMediaObjects = new Dictionary<int, DialogueMediaObject>();
    }
  }

  public static void Save() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      GD.Print($"[DialoguesMediaHandler] Attempting to save mappings to: {fullPath}");

      Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

      string jsonString = JsonSerializer.Serialize(AllMediaObjects, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(fullPath, jsonString);
      GD.Print($"[DialoguesMediaHandler] Successfully saved {AllMediaObjects.Count} mappings");
    } catch (Exception e) {
      GD.PrintErr($"[DialoguesMediaHandler] Error saving mappings: {e.Message}");
    }
  }
}
