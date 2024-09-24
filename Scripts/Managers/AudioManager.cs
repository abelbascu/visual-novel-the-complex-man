using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// AudioManager.cs
public partial class AudioManager : Control {
  public static AudioManager Instance { get; private set; }
  private AudioStreamPlayer2D musicPlayer;
  private AudioStreamPlayer2D soundPlayer;

  public override void _EnterTree() {
    if (Instance == null) {
      Instance = this;
    } else {
      QueueFree();
    }
  }

  public override void _Ready() {
    SetupAudioBuses();
    AddLimiterToMasterBus();
    musicPlayer = new AudioStreamPlayer2D();
    soundPlayer = new AudioStreamPlayer2D();
    AddChild(musicPlayer);
    AddChild(soundPlayer);
    MouseFilter = MouseFilterEnum.Ignore;
  }

  private void SetupAudioBuses() {
    AudioServer.AddBus(1);
    AudioServer.SetBusName(1, "Music");
    AudioServer.AddBus(2);
    AudioServer.SetBusName(2, "SFX");
  }

  private void AddLimiterToMasterBus() {
    var limiter = new AudioEffectLimiter();
    limiter.ThresholdDb = -3.0f;
    limiter.CeilingDb = 0.0f;
    AudioServer.AddBusEffect(0, limiter);
  }

  public async Task PlayMusic(string path, float preDelay = 0, float postDelay = 0) {
    var stream = ResourceLoader.Load<AudioStream>(path);
    musicPlayer.Stream = stream;
    musicPlayer.Bus = "Music";
    ApplyCompressorIfNeeded(musicPlayer);
    await Task.Delay((int)(preDelay * 1000));
    musicPlayer.Play();
    await Task.Delay((int)(postDelay * 1000));
  }

  public async Task PlaySound(string path, float preDelay = 0, float postDelay = 0, bool applyReverb = false, Vector2 position = default) {
    var stream = ResourceLoader.Load<AudioStream>(path);
    soundPlayer.Stream = stream;
    soundPlayer.Bus = "SFX";
    soundPlayer.Position = position;
    if (applyReverb) {
      ApplyReverbToSFXBus();
    }
    await Task.Delay((int)(preDelay * 1000));
    soundPlayer.Play();
    await Task.Delay((int)(postDelay * 1000));
  }

  private void ApplyCompressorIfNeeded(AudioStreamPlayer2D player) {
    var peakVolume = GetPeakVolume(player);
    if (peakVolume < -10.0f) {
      var compressor = new AudioEffectCompressor();
      compressor.Threshold = -10.0f;
      compressor.Ratio = 4.0f;
      var busIndex = AudioServer.GetBusIndex(player.Bus);
      AudioServer.AddBusEffect(busIndex, compressor);
    }
  }

  private void ApplyReverbToSFXBus() {
    var reverb = new AudioEffectReverb();
    reverb.RoomSize = 0.8f;
    reverb.Damping = 0.5f;
    var sfxBusIndex = AudioServer.GetBusIndex("SFX");
    AudioServer.AddBusEffect(sfxBusIndex, reverb);
  }

  private float GetPeakVolume(AudioStream stream) {
    float peak = 0;
    double length = stream._GetLength();
    var playback = stream.InstantiatePlayback();

    if (playback != null) {
      const int sampleRate = 44100;
      float[] buffer = new float[1024];

      for (double position = 0; position < length; position += (double)buffer.Length / sampleRate) {
        playback._Seek((float)position);
        for (int i = 0; i < buffer.Length; i++) {
          double samplePosition = position + (double)i / sampleRate;
          if (samplePosition >= length) break;

          float sample = (float)playback._GetParameter(new StringName("sample"));
          buffer[i] = sample;
          peak = Mathf.Max(peak, Mathf.Abs(sample));
        }
      }
    }

    return Mathf.LinearToDb(peak);
  }

  private float GetPeakVolume(AudioStreamPlayer2D player) {
    return GetPeakVolume(player.Stream);
  }



  public async Task PlayMusicAndDelayedSound(string musicPath, string soundPath, Vector2 soundPosition) {
    await PlayMusic(musicPath);
    await Task.Delay(3000);
    await PlaySound(soundPath, applyReverb: true, position: soundPosition);
  }
}


