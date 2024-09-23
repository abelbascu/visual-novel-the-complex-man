using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public static class VisualPathMappings {
  private static Dictionary<int, string> _mappings = new Dictionary<int, string>();
  public static Dictionary<int, string> Mappings {
    get {
      if (_mappings == null) {
        _mappings = new Dictionary<int, string>();
        Load(); // Try to load existing mappings when first accessed
      }
      return _mappings;
    }
    set { _mappings = value; }
  }

  private const string JSON_PATH = "res://Resources/VisualPathMappings.json";

  static VisualPathMappings() {
    Load(); // Try to load existing mappings when the class is first used
  }

  public static void Load() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      GD.Print($"[VisualPathMappings] Attempting to load mappings from: {fullPath}");

      if (File.Exists(fullPath)) {
        string jsonString = File.ReadAllText(fullPath);
        var loadedMappings = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonString);
        if (loadedMappings != null) {
          Mappings = loadedMappings;
          GD.Print($"[VisualPathMappings] Successfully loaded {Mappings.Count} mappings");
        } else {
          GD.PrintErr("[VisualPathMappings] Loaded mappings were null, initializing empty dictionary");
          Mappings = new Dictionary<int, string>();
        }
      } else {
        GD.Print("[VisualPathMappings] Mappings file does not exist, initializing empty dictionary");
        Mappings = new Dictionary<int, string>();
      }
    } catch (Exception e) {
      GD.PrintErr($"[VisualPathMappings] Error loading mappings: {e.Message}");
      Mappings = new Dictionary<int, string>();
    }
  }

  public static void Save() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      GD.Print($"[VisualPathMappings] Attempting to save mappings to: {fullPath}");

      string jsonString = JsonSerializer.Serialize(Mappings, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(fullPath, jsonString);
      GD.Print($"[VisualPathMappings] Successfully saved {Mappings.Count} mappings");
    } catch (Exception e) {
      GD.PrintErr($"[VisualPathMappings] Error saving mappings: {e.Message}");
    }
  }
}