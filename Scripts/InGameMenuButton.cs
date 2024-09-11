using Godot;
using System;
using System.Threading.Tasks;
using static GameStateMachine;

public partial class InGameMenuButton : MarginContainer {

    InteractableUITextureButton textureButton;
    public bool IsInteractable => Visible;

    public override void _Ready() {
        textureButton = GetNode<InteractableUITextureButton>("TextureButton");
        textureButton.Pressed += () => _ = OnTextureButtonPressed();
        textureButton.Visible = false;
    }

    public async Task OnTextureButtonPressed() {

        DisableIngameMenuButton();
        InputManager.Instance.SetGamePadAndKeyboardInputEnabled(false);

        if (UIManager.Instance.mainMenu.MainOptionsContainer.Visible == false &&
                GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
            //we pause timer here and not withing display menu method because when we show some submenus
            //we hide the ingame menu and then show it again when closing the submenus.
            //we just want to pause it every time that we are not in dialogue mode for now.
            LoadSaveManager.Instance.PauseGameTimer();
            //await UIManager.Instance.mainMenu.DisplayInGameMenu();
            GameStateManager.Instance.Fire(Trigger.DISPLAY_INGAME_MENU);
            DisableIngameMenuButton();
            await UIFadeHelper.FadeInControl(textureButton, 0.3f);
            EnableIngameMenuButton();
        } else {
            //LoadSaveManager.Instance.ResumeGameTimer();
            // await UIManager.Instance.mainMenu.CloseInGameMenu();
            await UIManager.Instance.mainMenu.CloseInGameMenu();
            // UIManager.Instance.mainMenu.Visible = false;
            GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
            DisableIngameMenuButton();
            await UIFadeHelper.FadeOutControl(textureButton, 0.3f);
            EnableIngameMenuButton();
            InputManager.Instance.SetGamePadAndKeyboardInputEnabled(true);

        }

        EnableIngameMenuButton();

        UIManager.Instance.UpdateUILayout();
    }


    public void DisableIngameMenuButton() {
        textureButton.SetProcessInput(false);
        textureButton.MouseFilter = MouseFilterEnum.Ignore;
        textureButton.FocusMode = FocusModeEnum.None;
        textureButton.SetProcessInput(false);
        //textureButton.Visible = false;
    }

    public void EnableIngameMenuButton() {
        textureButton.SetProcessInput(true);
        textureButton.MouseFilter = MouseFilterEnum.Stop;
        textureButton.FocusMode = FocusModeEnum.All;
        textureButton.SetProcessInput(true);
        textureButton.Visible = true;
    }


}
