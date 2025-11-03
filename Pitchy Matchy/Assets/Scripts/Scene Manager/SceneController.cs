using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void Play()
    {
        // If player already selected a version previously → skip Version Select
        if (GameVersionManager.Instance != null && GameVersionManager.Instance.HasChosenVersion)
        {
            SceneManager.LoadSceneAsync("Level Select");
        }
        else
        {
            SceneManager.LoadSceneAsync("Version Select");
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
        Time.timeScale = 1;
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
