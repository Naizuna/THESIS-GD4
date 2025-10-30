using UnityEngine;

public static class LevelProgress
{
    private const string UnlockedLevelKey = "UnlockedLevel";

    public static int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UnlockedLevelKey, 1); // Default to level 1 unlocked
    }

    public static void UnlockNextLevel(int level)
    {
        int current = GetUnlockedLevel();
        if (level > current)
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, level);
            PlayerPrefs.Save();
        }
    }
}
