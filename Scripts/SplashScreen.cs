using Godot;
using System;
using static GameStateMachine;
using UIHelpers;
using System.Threading.Tasks;

public partial class SplashScreen : Control {
    private TextureRect backgroundTexture;
    private RichTextLabel pressAnyKeyLabel;
    private UITextTweenFadeIn fadeIn;
    private UITextTweenFadeOut fadeOut;

    public override void _Ready() {
        backgroundTexture = GetNode<TextureRect>("TextureRect");
        pressAnyKeyLabel = GetNode<RichTextLabel>("MarginContainer/RichTextLabel");

        backgroundTexture.GuiInput += OnBackgroundGuiInput;
        fadeIn = new UITextTweenFadeIn();
        fadeOut = new UITextTweenFadeOut();
    }

    public bool isExecuting = false;

    public override void _Process(double delta) {
        base._Process(delta);
        while (!isExecuting) {
            _ = TaskContinousFadeInout();
        }
    }

    public async Task TaskContinousFadeInout() {
        isExecuting = true;
        await fadeIn.FadeIn(pressAnyKeyLabel);
        await fadeOut.FadeOut(pressAnyKeyLabel);
        isExecuting = false;
    }

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("action_key")) {

            TransitionToMainMenu();
        }
    }

    public void OnBackgroundGuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed) {

            TransitionToMainMenu();
        }
    }

    public void TransitionToMainMenu() {
        pressAnyKeyLabel.Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        GameStateManager.Instance.Fire(Trigger.DISPLAY_MAIN_MENU);

    }
}
