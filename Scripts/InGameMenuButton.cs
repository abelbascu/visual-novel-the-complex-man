using Godot;
using System;

public partial class InGameMenuButton : MarginContainer {

    TextureButton textureButton;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        textureButton = GetNode<TextureButton>("TextureButton");
        textureButton.Pressed += OnTextureButtonPressed;
    }

    public void OnTextureButtonPressed() {
        if (UIManager.Instance.mainMenu.Visible == false) {
            GameStateManager.Instance.DISPLAY_INGAME_MENU();
        }
		else
		{
			UIManager.Instance.mainMenu.CloseInGameMenu();
		}
         UIManager.Instance.UpdateUILayout();
    }
}
