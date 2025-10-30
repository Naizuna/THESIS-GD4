using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelCompletionManager
{
    public static void UnlockNextLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = LevelDataManager.GetNextLevel(currentScene);

        if (!string.IsNullOrEmpty(nextScene))
        {
            int nextIndex = LevelDataManager.GetLevelIndex(nextScene) + 1;
            LevelProgress.UnlockNextLevel(nextIndex);
            Debug.Log($"✅ Unlocked next: {nextScene} (Level {nextIndex})");
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("🏁 Final level reached — no next level to unlock.");
        }
    }
}
