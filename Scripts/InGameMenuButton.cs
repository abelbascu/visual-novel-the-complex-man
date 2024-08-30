using Godot;
using System;
using System.Threading.Tasks;
using static GameStateMachine;

public partial class InGameMenuButton : MarginContainer {

    TextureButton textureButton;

    public override void _Ready() {
        textureButton = GetNode<TextureButton>("TextureButton");
        textureButton.Pressed += () => _ = OnTextureButtonPressed();
    }

    public async Task OnTextureButtonPressed() {
        textureButton.SetProcessInput(false); //WE NEED TO PUT THIS TO LINES AFTER THE INGAME MENU HAS BEEN COMPLETELY DISPLAYED
        textureButton.MouseFilter = MouseFilterEnum.Ignore; //OTHERWISE IF THE USER CLICKS THE INGAME BUTTON REPEATEDLY IT BREAKS

        if (UIManager.Instance.mainMenu.MainOptionsContainer.Visible == false &&
                GameStateManager.Instance.IsInState(State.InDialogueMode, SubState.None)) {
            // await UIManager.Instance.mainMenu.DisplayInGameMenu();
            GameStateManager.Instance.Fire(Trigger.DISPLAY_INGAME_MENU);
            textureButton.SetProcessInput(true); //WE NEED TO PUT THIS TO LINES AFTER THE INGAME MENU HAS BEEN COMPLETELY DISPLAYED
            textureButton.MouseFilter = MouseFilterEnum.Stop; //OTHERWISE IF THE USER CLICKS THE INGAME BUTTON REPEATEDLY IT BREAKS

        } else {
            textureButton.SetProcessInput(false);
            textureButton.MouseFilter = MouseFilterEnum.Ignore;
            await UIManager.Instance.mainMenu.CloseInGameMenu();
            UIManager.Instance.mainMenu.Visible = false;
            GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
            textureButton.SetProcessInput(true);
            textureButton.MouseFilter = MouseFilterEnum.Stop;
        }
        UIManager.Instance.UpdateUILayout();
    }
}
