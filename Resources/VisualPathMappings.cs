using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public static class VisualPathMappings {
  public static Dictionary<int, string> Mappings { get; set; } = new Dictionary<int, string>();
  private const string JSON_PATH = "res://Resources/VisualPathMappings.json";
  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
    WriteIndented = true
  };

  public static void Load() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      if (File.Exists(fullPath)) {
        string jsonString = File.ReadAllText(fullPath);
        Mappings = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonString);
        GD.Print($"Loaded {Mappings.Count} visual path mappings");
      } else {
        GD.Print("VisualPathMappings.json does not exist. Starting with empty mappings.");
      }
    } catch (Exception e) {
      GD.PrintErr($"Error loading VisualPathMappings: {e.Message}");
    }
  }


  public static void Save() {
    try {
      string fullPath = ProjectSettings.GlobalizePath(JSON_PATH);
      string jsonString = JsonSerializer.Serialize(Mappings, JsonOptions);
      File.WriteAllText(fullPath, jsonString);
      GD.Print($"Saved {Mappings.Count} visual path mappings");
    } catch (Exception e) {
      GD.PrintErr($"Error saving VisualPathMappings: {e.Message}");
    }
  }
}
