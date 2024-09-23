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
    await Task.Delay((int)(preDelay * 1000));
    var stream = ResourceLoader.Load<AudioStream>(path);
    musicPlayer.Stream = stream;
    musicPlayer.Play();
    await Task.Delay((int)(postDelay * 1000));
  }

  public async Task PlaySound(string path, float preDelay = 0, float postDelay = 0) {
    await Task.Delay((int)(preDelay * 1000));
    var stream = ResourceLoader.Load<AudioStream>(path);
    soundPlayer.Stream = stream;
    soundPlayer.Play();
    await Task.Delay((int)(postDelay * 1000));
  }
}
