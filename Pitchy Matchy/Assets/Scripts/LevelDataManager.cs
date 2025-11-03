using UnityEngine;
using System.Collections.Generic;

public static class LevelDataManager
{
    // Order matters — first item = level 1, next = level 2, etc.
    public static readonly List<string> LevelOrder = new List<string>
    {
        "Stage 1 Lesson",
        "Stage 1 Mini Quiz",
        "Stage 2 Lesson",
        "Stage 2 Mini Quiz",
        "Stage 3 Lesson",
        "Stage 3 Mini Quiz",
        "Final Quiz",
    };

    public static int GetLevelIndex(string sceneName)
    {
        return LevelOrder.IndexOf(sceneName);
    }

    public static string GetNextLevel(string currentScene)
    {
        int index = GetLevelIndex(currentScene);
        if (index >= 0 && index < LevelOrder.Count - 1)
            return LevelOrder[index + 1];
        else
            return null; // no next level (last level reached)
    }
}
