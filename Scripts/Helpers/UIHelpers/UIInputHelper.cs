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
        // else  if (parent is Node2D node)
        // {
        //     SetNodeInputState(node, isEnableInput);
        // }

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

    //  private static void SetNodeInputState(Node2D node, bool isEnableInput)
    // {
    //     node.MouseFilter = isEnableInput ? node.MouseFilterEnum.Stop : node.MouseFilterEnum.Ignore;
    //     node.FocusMode = isEnableInput ? node.FocusModeEnum.All : node.FocusModeEnum.None;
    // }
}