using Godot;
using System;
using System.Threading.Tasks;

public static class InputBlocker {
  private static int blockCount = 0;
  private static bool isExecuting = false;

  public static async Task BlockNewInput(Func<Task> action) {
    if (isExecuting) {
      GD.Print($"Now executing {action}");
      // If we're already executing a blocked action, just run the new action
      await action();
      return;
    }


    blockCount++;
    GD.Print($"Number of async methods in cue {blockCount}");
    isExecuting = true;
    try {
      await action();
    } finally {
      GD.Print($"Now finishing {action}");
      blockCount--;
      if (blockCount < 0)
        blockCount = 0;
      isExecuting = false;
    }
  }

  public static bool IsInputBlocked => blockCount > 0;
}