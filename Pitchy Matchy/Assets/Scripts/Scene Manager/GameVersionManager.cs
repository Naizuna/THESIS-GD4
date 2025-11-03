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
}
