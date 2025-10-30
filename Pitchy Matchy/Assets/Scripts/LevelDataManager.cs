using UnityEngine;
using System.Collections.Generic;

public static class LevelDataManager
{
    // Order matters — first item = level 1, next = level 2, etc.
    public static readonly List<string> LevelOrder = new List<string>
    {
        "Stage 1 Lesson",
        "Quiz",
        "Stage 2 Lesson",
        "MCCTestScene2",
        "BossStage"
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
