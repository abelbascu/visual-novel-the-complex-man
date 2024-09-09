using Godot;
using System;
using static GameStateMachine;
using System.Threading.Tasks;

public partial class SplashScreen : Control {

    public bool isExecuting = false;

    public TextureRect backgroundTexture { get; private set; }
    private RichTextLabel pressAnyKeyLabel;
    private UITextTweenFadeIn fadeIn;
    private UITextTweenFadeOut fadeOut;

    public void DisableInput() {
        SetProcessInput(false);
    }

    private void SetInputHandled() {
        GetViewport().SetInputAsHandled();
    }


    public override void _Ready() {
        CallDeferred("DisableInput");
        CallDeferred("SetInputHandled");
        backgroundTexture = GetNode<TextureRect>("TextureRect");
        backgroundTexture.Modulate = new Color(1, 1, 1, 0); // Start fully transparent
        pressAnyKeyLabel = GetNode<RichTextLabel>("MarginContainer/RichTextLabel");

        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();

        // Use CallDeferred with a lambda to call the async method
        _ = FadeInScreen();
    }

    public async Task FadeInScreen() {
        backgroundTexture.Show();
        await fadeIn.FadeIn(backgroundTexture, 1.5f);
        SetProcessInput(true);
    }

    public async Task FadeOutScreen() {
        SetProcessInput(false);
        pressAnyKeyLabel.Visible = false;
        await fadeOut.FadeOut(backgroundTexture, 0.5f);
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
        await fadeIn.FadeIn(pressAnyKeyLabel, 1.0f);
        await fadeOut.FadeOut(pressAnyKeyLabel, 1.0f);
        isExecuting = false;
    }


    public async Task TransitionToMainMenu() {


        GetViewport().SetInputAsHandled();
        MouseFilter = MouseFilterEnum.Ignore;

        InputManager.Instance.SetGamePadAndKeyboardInputEnabled(false);

        await EnsureInputIsDIsabled();

        pressAnyKeyLabel.Visible = false;

        await UIFadeHelper.FadeOutControl(this, 1.0f);

        //await fadeOut.FadeOut(backgroundTexture, 1.5f);
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);

        Visible = false;
        pressAnyKeyLabel.Visible = false;
        GetViewport().SetInputAsHandled();

        InputManager.Instance.SetGamePadAndKeyboardInputEnabled(true);
        MouseFilter = MouseFilterEnum.Stop;

    }

    private async Task EnsureInputIsDIsabled() {
        SetProcessInput(false);
        await Task.CompletedTask;
    }

}
