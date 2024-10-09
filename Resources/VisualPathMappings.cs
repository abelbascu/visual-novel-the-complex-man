using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public class DialogueObjectMediaInfo {
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

public static class VisualPathMappings {
  private static Dictionary<int, DialogueObjectMediaInfo> _mappings = new Dictionary<int, DialogueObjectMediaInfo>();
  public static Dictionary<int, DialogueObjectMediaInfo> Mappings {
    get {
      if (_mappings == null) {
        _mappings = new Dictionary<int, DialogueObjectMediaInfo>();
        Load();
      }
      return _mappings;
    }
    set { _mappings = value; }
  }

  private const string JSON_PATH = "res://Resources/VisualPathMappings.json";

  static VisualPathMappings() {
    Load();
  }

  public static void Load() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      GD.Print($"[VisualPathMappings] Attempting to load mappings from: {fullPath}");

      if (File.Exists(fullPath)) {
        string jsonString = File.ReadAllText(fullPath);
        var loadedMappings = JsonSerializer.Deserialize<Dictionary<int, DialogueObjectMediaInfo>>(jsonString);
        if (loadedMappings != null) {
          Mappings = loadedMappings;
          GD.Print($"[VisualPathMappings] Successfully loaded {Mappings.Count} mappings");
        } else {
          GD.PrintErr("[VisualPathMappings] Loaded mappings were null, initializing empty dictionary");
          Mappings = new Dictionary<int, DialogueObjectMediaInfo>();
        }
      } else {
        GD.Print("[VisualPathMappings] Mappings file does not exist, initializing empty dictionary");
        Mappings = new Dictionary<int, DialogueObjectMediaInfo>();
      }
    } catch (Exception e) {
      GD.PrintErr($"[VisualPathMappings] Error loading mappings: {e.Message}");
      Mappings = new Dictionary<int, DialogueObjectMediaInfo>();
    }
  }

  public static void Save() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      GD.Print($"[VisualPathMappings] Attempting to save mappings to: {fullPath}");

      Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

      string jsonString = JsonSerializer.Serialize(Mappings, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(fullPath, jsonString);
      GD.Print($"[VisualPathMappings] Successfully saved {Mappings.Count} mappings");
    } catch (Exception e) {
      GD.PrintErr($"[VisualPathMappings] Error saving mappings: {e.Message}");
    }
  }
}
