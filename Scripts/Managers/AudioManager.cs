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
    var limiter = new AudioEffectHardLimiter();
    limiter.CeilingDb = -0.3f;
    AudioServer.AddBusEffect(0, limiter);
  }

  private async Task FadeAudio(AudioStreamPlayer2D player, float startVolume, float endVolume, float duration) {


    var timer = new System.Diagnostics.Stopwatch();
    timer.Start();

    while (timer.Elapsed.TotalSeconds < duration) {
      float t = (float)(timer.Elapsed.TotalSeconds / duration);
      float easedT = EaseInOutQuad(t);
      float currentVolume = Mathf.Lerp(startVolume, endVolume, easedT);
      float volumeDb = Mathf.LinearToDb(Mathf.Clamp(currentVolume, 0.0001f, 1));
      player.VolumeDb = volumeDb;

      GD.Print($"Time: {timer.Elapsed.TotalSeconds:F2}s, Volume = {currentVolume}, VolumeDb = {volumeDb}, Playing: {player.Playing}");
      await ToSignal(GetTree(), "process_frame");
    }

    player.VolumeDb = Mathf.LinearToDb(endVolume);
    GD.Print($"Fade completed. Duration: {timer.Elapsed.TotalSeconds:F2}s, Final VolumeDb: {player.VolumeDb}, Playing: {player.Playing}");
  }

  private float EaseInOutQuad(float t) {
    return t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;
  }

  public async Task PlayMusic(string path, float preDelay = 0, float postDelay = 0, float fadeDuration = 1.5f, bool loop = true) {
    var newStream = ResourceLoader.Load<AudioStream>(path);

    // Fade out current music if playing
    if (musicPlayer.Playing) {
      await FadeAudio(musicPlayer, Mathf.DbToLinear(musicPlayer.VolumeDb), 0, fadeDuration);
      musicPlayer.Stop();
    }

    musicPlayer.Stream = newStream;
    musicPlayer.Bus = "Music";

    // Remove any existing finished signal connections
    musicPlayer.Finished -= OnMusicFinished;

    if (loop) {
      // Connect the finished signal to restart playback
      musicPlayer.Finished += OnMusicFinished;
    }

    await Task.Delay((int)(preDelay * 1000));

    // Set initial volume to 0 and start playing
    musicPlayer.VolumeDb = Mathf.LinearToDb(0);
    musicPlayer.Play();

    // if (newStream is AudioStreamWav wavStream) {
    //   wavStream.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
    // } else if (newStream is AudioStreamOggVorbis oggStream) {
    //   oggStream.Loop = loop;
    // }
    // ApplyCompressorIfNeeded(musicPlayer);

    GD.Print($"musicPlayer.Playing: {musicPlayer.Playing}");
    // Fade in new music
    await FadeAudio(musicPlayer, 0, 1, fadeDuration);
    GD.Print($"musicPlayer.Playing: {musicPlayer.Playing}");

    await Task.Delay((int)(postDelay * 1000));
  }


  private void OnMusicFinished() {
    musicPlayer.Play();
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


