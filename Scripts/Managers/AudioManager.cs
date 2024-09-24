using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// AudioManager.cs
public partial class AudioManager : Control {
  public static AudioManager Instance { get; private set; }
  private AudioStreamPlayer musicPlayer;
  private AudioStreamPlayer soundPlayer;

  public override void _EnterTree() {
    if (Instance == null) {
      Instance = this;
    } else {
      QueueFree();
    }
  }

  public override void _Ready() {
    musicPlayer = new AudioStreamPlayer();
    soundPlayer = new AudioStreamPlayer();
    AddChild(musicPlayer);
    AddChild(soundPlayer);

    MouseFilter = MouseFilterEnum.Ignore;
  }

  public async Task PlayMusic(string path, float preDelay = 0, float postDelay = 0) {
    //GD.Print($"PlayMusic started. Path: {path}, PreDelay: {preDelay}, PostDelay: {postDelay}");
    var stream = ResourceLoader.Load<AudioStream>(path);
    musicPlayer.Stream = stream;
    //GD.Print($"Playing music at {DateTime.Now:HH:mm:ss.fff}");
    musicPlayer.Play();
    //GD.Print($"Starting post-delay at {DateTime.Now:HH:mm:ss.fff}");
    //GD.Print("PlayMusic completed");
  }

  public async Task PlaySound(string path, float preDelay = 0, float postDelay = 0) {
    var stream = ResourceLoader.Load<AudioStream>(path);
    soundPlayer.Stream = stream;
    soundPlayer.Play();
  }





}
