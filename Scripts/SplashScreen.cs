using Godot;
using System;
using static GameStateMachine;
using UIHelpers;
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
       // CallDeferred("DisableInput");
        CallDeferred("SetInputHandled");
        backgroundTexture = GetNode<TextureRect>("TextureRect");
        pressAnyKeyLabel = GetNode<RichTextLabel>("MarginContainer/RichTextLabel");

        backgroundTexture.GuiInput += OnBackgroundGuiInput;
        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();
    }


    public override void _Process(double delta) {
        base._Process(delta);
        while (!isExecuting) {
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
            CallDeferred("TransitionToMainMenu");
        }
    }

    public void OnBackgroundGuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed) {
            // Consume the event
            GetViewport().SetInputAsHandled();
            TransitionToMainMenu();
        }
    }

    public async void TransitionToMainMenu() {
        // Disable input processing immediately
        SetProcessInput(false);

        pressAnyKeyLabel.Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;

        //UIManager.Instance.mainMenu.ProcessMode = ProcessModeEnum.Disabled;
        // Add a small delay to ensure any pending input events are processed
        //await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        //await fadeOut.FadeOut(backgroundTexture, 1.5f);

        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);

        await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        //UIManager.Instance.mainMenu.ProcessMode = ProcessModeEnum.Inherit;
    }

}
