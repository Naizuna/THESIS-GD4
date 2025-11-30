using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    
    public static PauseMenu instance {get; private set; }
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject pauseButton;
    [SerializeField] GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    public static bool isPaused;

    public void Awake()
    {
        // if (instance != null && instance != this)
        // {
        //     Destroy(this.gameObject);
        // }
        // else
        // {
        //     instance = this;
        //     DontDestroyOnLoad(this.gameObject);
        // }

        isPaused = false;
        pauseMenu.SetActive(false);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Stage 1 Lesson" ||
            SceneManager.GetActiveScene().name == "Stage 2 Lesson" ||
            SceneManager.GetActiveScene().name == "Stage 3 Lesson" ||
            SceneManager.GetActiveScene().name == "Stage 1 Mini Quiz" ||
            SceneManager.GetActiveScene().name == "Stage 2 Mini Quiz" ||
            SceneManager.GetActiveScene().name == "Stage 3 Mini Quiz" ||
            SceneManager.GetActiveScene().name == "Final Quiz")
        {

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                pauseButton.SetActive(false);
                pausePanel.SetActive(false);
            }
            else
            {
                pauseButton.SetActive(true);
                pausePanel.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayButtonClick();

                // 1. If Options panel is open → close it first
                if (optionsPanel != null && optionsPanel.activeSelf)
                {
                    optionsPanel.SetActive(false);
                    return;  // top here, do NOT toggle pause
                }
                    
                //not main menu
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

    public void PauseButton()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
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
