using Godot;
using System;
using static GameStateMachine;
using System.Threading.Tasks;

public partial class SplashScreen : Control {

    public bool isExecuting = false;

    private TextureRect backgroundTexture;
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

        backgroundTexture.GuiInput += OnBackgroundGuiInput;
        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();

        // Use CallDeferred with a lambda to call the async method
        _ =FadeInScreen();
    }

    public async Task FadeInScreen() {
        backgroundTexture.Show();
        await fadeIn.FadeIn(backgroundTexture, 1.5f);
        SetProcessInput(true);
    }

    public async Task FadeOutScreen()
{
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

    public override void _Input(InputEvent @event) {

        if (@event.IsActionPressed("action_key")) {
            // Consume the event
            GetViewport().SetInputAsHandled();
            // CallDeferred("TransitionToMainMenu");
            TransitionToMainMenu();
        }
    }

    public void OnBackgroundGuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed) {
            // Consume the event
            GetViewport().SetInputAsHandled();
            TransitionToMainMenu();
        }
    }

    public async Task TransitionToMainMenu() {

        // becasue even if we hide the scen it still processes input behind.
        SetProcessInput(false);

        pressAnyKeyLabel.Visible = false;

        await UIFadeHelper.FadeOutControl(this, 1.0f);

        //await fadeOut.FadeOut(backgroundTexture, 1.5f);
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);

        Visible = false;
    }

}
