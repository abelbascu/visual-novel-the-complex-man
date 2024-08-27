using Godot;
using System;
using static GameStateMachine;

public partial class InGameMenuButton : MarginContainer {

    TextureButton textureButton;
    
    public override void _Ready() {
        textureButton = GetNode<TextureButton>("TextureButton");
        textureButton.Pressed += OnTextureButtonPressed;
    }

    public void OnTextureButtonPressed() {
        if (UIManager.Instance.mainMenu.Visible == false) {
            GameStateManager.Instance.Fire(Trigger.DISPLAY_INGAME_MENU);
        }
		else
		{
			UIManager.Instance.mainMenu.CloseInGameMenu();
            GameStateManager.Instance.Fire(Trigger.ENTER_DIALOGUE_MODE);
		}
         UIManager.Instance.UpdateUILayout();
    }
}
