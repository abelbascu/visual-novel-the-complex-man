using Godot;
using System;
using static GameStateMachine;
using System.Threading.Tasks;

public partial class SplashScreen : Control {

  public bool isExecuting = false;

  public TextureRect backgroundTexture { get; private set; }
  private RichTextLabel pressAnyKeyLabel;
  private ShaderMaterial shaderMaterial;

  public void DisableInput() {
    SetProcessInput(false);
  }

  private void SetInputHandled() {
    GetViewport().SetInputAsHandled();
  }

  public override async void _Ready() {

    GetTree().Root.SetContentScaleSize(new Vector2I(1920, 1080));
    GetTree().Root.SetContentScaleFactor(1.0f);

    CallDeferred("DisableInput");
    CallDeferred("SetInputHandled");
    backgroundTexture = GetNode<TextureRect>("TextureRect");
    backgroundTexture.Modulate = new Color(1, 1, 1, 0); // Start fully transparent
    backgroundTexture.Texture = GD.Load<Texture2D>("res://Visuals/splash screen.png");
    backgroundTexture.SetAnchorsPreset(LayoutPreset.FullRect);
    backgroundTexture.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
    backgroundTexture.StretchMode = TextureRect.StretchModeEnum.Scale;
    backgroundTexture.Visible = true;
    pressAnyKeyLabel = GetNode<RichTextLabel>("MarginContainer/RichTextLabel");

    CallDeferred("SetupShader");
    //CallDeferred with a lambda to call the async method
    _ = FadeInScreen();
    _ = AudioManager.Instance.PlayMusic("res://Audio/Music/splash screen.wav", 0, 0, 5f, true);
  }

  public async Task FadeInScreen() {
    backgroundTexture.Show();
    await UIFadeHelper.FadeInControl(backgroundTexture, 1.5f);
    SetProcessInput(true);
  }

  private void SetupShader() {
    Shader shader = GD.Load<Shader>("res://Shaders/background_sinwavefx.gdshader");
    shaderMaterial = new ShaderMaterial { Shader = shader };
    backgroundTexture.Material = shaderMaterial;

    // Set initial shader parameters
    shaderMaterial.SetShaderParameter("speed", 0.5f);
    shaderMaterial.SetShaderParameter("amplitude", 3.0f);
    shaderMaterial.SetShaderParameter("wave_width", 0.5f);
  }

  public async Task FadeOutScreen() {
    SetProcessInput(false);
    pressAnyKeyLabel.Visible = false;
    await UIFadeHelper.FadeOutControl(backgroundTexture, 0.5f);
    Visible = false;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (!isExecuting) {
      _ = TaskContinousFadeInout();
    }
  }

  public async Task TaskContinousFadeInout() {
    isExecuting = true;
    await UIFadeHelper.FadeInControl(pressAnyKeyLabel, 1.0f);
    await UIFadeHelper.FadeOutControl(pressAnyKeyLabel, 1.0f);
    isExecuting = false;
  }

  public async Task TransitionToMainMenu() {
    GetViewport().SetInputAsHandled();
    MouseFilter = MouseFilterEnum.Ignore;

    await EnsureInputIsDIsabled();

    pressAnyKeyLabel.Visible = false;

    // Deactivate the shader before fading out
    DeactivateShader();

    await UIFadeHelper.FadeOutControl(this, 1.0f);

    GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);

    Visible = false;
    pressAnyKeyLabel.Visible = false;
    GetViewport().SetInputAsHandled();

    MouseFilter = MouseFilterEnum.Stop;
  }

  private async Task EnsureInputIsDIsabled() {
    SetProcessInput(false);
    await Task.CompletedTask;
  }

  private void DeactivateShader() {
    if (backgroundTexture.Material != null) {
      backgroundTexture.Material = null;
    }
  }

  // Optional: Method to reactivate the shader if needed
  private void ReactivateShader() {
    if (backgroundTexture.Material == null) {
      backgroundTexture.Material = shaderMaterial;
    }
  }
}