using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    
    public static PauseMenu instance {get; private set; }
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject pauseButton;
    public static bool isPaused;

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        isPaused = false;
        pauseMenu.SetActive(false);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Quiz")
        {
            pauseButton.SetActive(true);

            if (Input.GetKeyDown(KeyCode.Escape))
            { //not main menu
                if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
        else
        {
            pauseButton.SetActive(false);
        }
            
    }

    public void Pause()
    {
        isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0;
        // FindObjectOfType<AudioManager>().Play("UI Button");
    }
    public void Resume()
    {
        isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
        // FindObjectOfType<AudioManager>().Play("UI Button");
    }
    public void Restart()
    {
        isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1;

        // KeyManager.keysCollected = 0;
        
        // FindObjectOfType<AudioManager>().Play("UI Button");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // FindObjectOfType<AudioManager>().PlayBackgroundMusic("Background");
    }
    public void Settings()
    {
        isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0;
        // FindObjectOfType<AudioManager>().Play("UI Button");
    }
    public void MainMenu()
    {
        isPaused = false;
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
        // FindObjectOfType<AudioManager>().Play("UI Button");
        // FindObjectOfType<SceneController>().LoadMainMenu();
    }
}
