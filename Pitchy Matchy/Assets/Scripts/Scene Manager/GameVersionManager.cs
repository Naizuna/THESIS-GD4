using UnityEngine;
using UnityEngine.SceneManagement;

public class GameVersionManager : MonoBehaviour
{
    public enum VersionType { Normal, SARSA, MCC }
    public VersionType SelectedVersion;

    public static GameVersionManager Instance { get; private set; }

    public bool HasChosenVersion { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Press F5 to reset all unlocked levels and reload the scene
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ResetLevelProgressAndReload();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            UnlockNextLevel();
        }
    }

    public void SetVersion(int v)
    {
        SelectedVersion = (VersionType)v;
        HasChosenVersion = true;
    }

    public void ResetVersionSelection()
    {
        HasChosenVersion = false;
    }

    private void ResetLevelProgressAndReload()
    {
        // Clear PlayerPrefs progress
        PlayerPrefs.DeleteKey("UnlockedLevel");
        PlayerPrefs.Save();
        
        RLPersistenceManager.Instance.ClearAllSavedData();

        Debug.Log("🔁 Level progress reset — all levels locked again.");

        // 🔃 Try to reload scene safely
        SceneController sceneController = FindObjectOfType<SceneController>();
        if (sceneController != null)
        {
            sceneController.ReloadCurrentScene();
        }
        else
        {
            // fallback if no SceneController is in the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void UnlockNextLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = LevelDataManager.GetLevelIndex(currentScene);
        
        // If we're in a valid level scene
        if (currentIndex >= 0)
        {
            // Unlock the next level
            LevelCompletionManager.UnlockNextLevel();
            Debug.Log($"🔓 [F6 Cheat] Unlocked next level from: {currentScene}");
        }
        else
        {
            // If we're in menu or other scene, unlock all levels
            int totalLevels = LevelDataManager.LevelOrder.Count;
            LevelProgress.UnlockNextLevel(totalLevels);
            Debug.Log($"🔓 [F6 Cheat] Unlocked all {totalLevels} levels!");
        }
        
        // Reload the scene to refresh UI (especially useful in level select)
        SceneController sceneController = FindObjectOfType<SceneController>();
        if (sceneController != null)
        {
            sceneController.ReloadCurrentScene();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
