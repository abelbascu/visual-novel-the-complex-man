using System;
using Godot;

public static class UIInputHelper
{
    public static void EnableParentChildrenInput(Node parent)
    {
        SetInputState(parent, true);
    }

    public static void DisableParentChildrenInput(Node parent)
    {
        SetInputState(parent, false);
    }

    private static void SetInputState(Node parent, bool isEnableInput)
    {
        if (parent is Control controlParent)
        {
            SetControlInputState(controlParent, isEnableInput);
        }
        else if (parent is Window window)
        {
            SetWindowInputState(window, isEnableInput);
        }

        foreach (var child in parent.GetChildren())
        {
            SetInputState(child, isEnableInput);
        }
    }

    private static void SetControlInputState(Control control, bool isEnableInput)
    {
        control.MouseFilter = isEnableInput ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        control.FocusMode = isEnableInput ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
    }

    private static void SetWindowInputState(Window window, bool isEnableInput)
    {
        window.GuiDisableInput = !isEnableInput;
        window.Exclusive = !isEnableInput;
    }
}