using Godot;
using System;
using System.Threading.Tasks;

public class InputBlocker
{
    private int blockCount = 0;
    private bool isExecuting = false;

    public async Task BlockNewInput(Func<Task> action)
    {
        if (isExecuting)
        {
            // If we're already executing a blocked action, just run the new action
            await action();
            return;
        }

        blockCount++;
        isExecuting = true;
        try
        {
            await action();
        }
        finally
        {
            blockCount--;
            isExecuting = false;
        }
    }

    public bool IsBlocked => blockCount > 0;
}