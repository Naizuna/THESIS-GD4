using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public string sceneName;
    public Button button;
    public GameObject lockIcon;

    void Start()
    {
        int unlockedLevel = LevelProgress.GetUnlockedLevel();
        int myIndex = LevelDataManager.GetLevelIndex(sceneName) + 1;

        if (myIndex <= unlockedLevel)
        {
            button.interactable = true;
            lockIcon.SetActive(false);
        }
        else
        {
            button.interactable = false;
            lockIcon.SetActive(true);
        }

        button.onClick.AddListener(() => LoadLevel());
    }

    void LoadLevel()
    {
        SceneManager.LoadScene(sceneName);
    }
}
