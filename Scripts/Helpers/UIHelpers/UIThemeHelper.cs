using Godot;
using System;
using System.Linq;


public static class UIThemeHelper {

    public static void ChangeButtonStateColor(Button button, string state, Color newColor) {
        var style = button.GetThemeStylebox(state) as StyleBoxFlat;
        style = style != null ? style.Duplicate() as StyleBoxFlat : new StyleBoxFlat();
        style.BgColor = newColor;
        button.AddThemeStyleboxOverride(state, style);
    }


    public static void ApplyCustomStyleToPanel(Panel panel) {
        var normalStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        panel.AddThemeStyleboxOverride("panel", normalStyle);
    }

    public static void ApplyCustomStyleToButton(Button button) {
        var normalStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
        button.AddThemeStyleboxOverride("normal", normalStyle);

        Color customBlue = new Color(
            0f / 255f,  // Red component
            71f / 255f,  // Green component
            171f / 255f   // Blue component
        );

        // Hover state
        var hoverStyle = new StyleBoxFlat {
            BgColor = customBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };

        button.AddThemeStyleboxOverride("hover", hoverStyle);

        // Pressed state
        var pressedStyle = new StyleBoxFlat {
            BgColor = Colors.DarkBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };

        button.AddThemeStyleboxOverride("pressed", pressedStyle);

        var disabledStyle = new StyleBoxFlat {
            BgColor = Colors.DarkRed,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };

        button.AddThemeStyleboxOverride("disabled", pressedStyle);

        // Set font size
        button.AddThemeFontSizeOverride("font_size", 40);

        // // // // Button States:
        // // // // "normal"
        // // // // "hover"
        // // // // "pressed"
        // // // // "focus"
        // // // // "disabled"
        // // // // "hover_pressed"
        // // // // "disabled_pressed"
        // // // // "disabled_hover"

        // // // // Color Properties:
        // // // // "font_color"
        // // // // "font_pressed_color"
        // // // // "font_hover_color"
        // // // // "font_focus_color"
        // // // // "font_disabled_color"
        // // // // "icon_color_normal"
        // // // // "icon_color_pressed"
        // // // // "icon_color_hover"
        // // // // "icon_color_disabled"

        // // // // Font Size Properties:
        // // // // "font_size"
        // // // // "outline_size"

        // // // // Stylebox Properties (for use with AddThemeStyleboxOverride):
        // // // // "normal"
        // // // // "hover"
        // // // // "pressed"
        // // // // "disabled"
        // // // // "focus"

        // // // // Constant Properties:
        // // // // "h_separation"
        // // // // "outline_size"
        // // // // "icon_max_width"

        // // // // Icon Properties:
        // // // // "icon"
    }

    public static void PrintThemeProperties() {
        var button = new Button();
        var theme = button.Theme;
        if (theme == null) {
            GD.Print("No theme assigned to button.");
        }

        GD.Print("Theme properties for Button:");

        // Print color properties
        PrintProperties("Colors", theme.GetColorTypeList());

        // Print constant properties
        PrintProperties("Constants", theme.GetConstantTypeList());

        // Print font properties
        PrintProperties("Fonts", theme.GetFontTypeList());

        // Print font size properties
        PrintProperties("Font Sizes", theme.GetFontSizeTypeList());

        // Print icon properties
        PrintProperties("Icons", theme.GetIconTypeList());

        // Print stylebox properties
        PrintProperties("Styleboxes", theme.GetStyleboxTypeList());

        // Don't forget to free the button we created
        button.QueueFree();
    }

    private static void PrintProperties(string category, string[] properties) {
        GD.Print($"\n{category}:");
        foreach (var property in properties.Where(p => p.StartsWith("Button/"))) {
            GD.Print($"  {property}");
        }
    }

    public static void ApplyCustomStyleToWindowDialog(ConfirmationDialog confirmationDialog) {

        //SEEMS TO BE A GODTO BUG, REMOVING THE TITLE BAR WOULD BE THE BEST SOLUTION
        confirmationDialog.KeepTitleVisible = false;
        //confirmationDialog.ExtendToTitle = true;
        confirmationDialog.Title = ""; // Clear the title text


        // if (window is ConfirmationDialog confirmationDialog) {
        // Create a new theme for the entire dialog
        var customTheme = new Theme();
        confirmationDialog.Theme = customTheme;

        // Style for the main window (affects the entire dialog including top bar)
        var windowStyle = new StyleBoxFlat {
            BgColor = Colors.NavyBlue,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderColor = Colors.White,
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ExpandMarginTop = 28 // Add space for the title bar
        };

        //Beware! if instead of "panel" you type "window", it won't change the background color
        customTheme.SetStylebox("panel", "Window", windowStyle);

        // Style for the close button (top-right X)
        var closeButtonStyle = new StyleBoxFlat {
            BgColor = Colors.Transparent
        };
        customTheme.SetStylebox("close", "Window", closeButtonStyle);

        // Style for the OK button with added padding
        var buttonStyle = new StyleBoxFlat {
            BgColor = Colors.DarkSlateBlue,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            BorderColor = Colors.White,
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        customTheme.SetStylebox("normal", "Button", buttonStyle);
        // Set text color for the entire dialog
        customTheme.SetColor("font_color", "Label", Colors.White);
        customTheme.SetColor("font_color", "Button", Colors.White);
        customTheme.SetColor("title_color", "Window", Colors.White); // Set title text color
        // Create and set larger font for dialog text and button
        customTheme.SetFontSize("font_size", "Label", 35);
        customTheme.SetFontSize("font_size", "Button", 35);
        customTheme.SetFontSize("font_size", "Window", 35);

        // Ensure the OK button is using the correct style
        confirmationDialog.GetOkButton().AddThemeStyleboxOverride("normal", buttonStyle);

        if (confirmationDialog.Name == "ExitGameConfirmationDialog" || confirmationDialog.Name == "ExitToMainMenuConfirmationDialog") {
            //do nothing
        } else {
            LeaveOnlyOKButtonInWindow(confirmationDialog);
        }
    }

    private static void LeaveOnlyOKButtonInWindow(Window window) {
        if (window is ConfirmationDialog buttonVContainer) {
            // Center the OK button
            var buttonContainer = buttonVContainer.GetOkButton().GetParent() as BoxContainer;
            if (buttonContainer != null) {
                buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;

                // Remove all children except the OK button
                foreach (var child in buttonContainer.GetChildren()) {
                    if (child != buttonVContainer.GetOkButton()) {
                        buttonContainer.RemoveChild(child);
                        child.QueueFree();
                    }
                }
            }
        }
    }
}

