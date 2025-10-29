using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Clips")]
    public AudioClip buttonClickSound;

    [Tooltip("Assign scene names with their respective BGMs.")]
    public List<SceneBGM> sceneBGMs = new List<SceneBGM>();

    [Header("Special Event Music")]
    public AudioClip tutorialCompleteMusic;
    public AudioClip winMusic;
    public AudioClip loseMusic;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private AudioSource eventSource; // for special one-time musics

    private string currentSceneName;
    private AudioClip currentBGM;
    private bool isEventMusicPlaying = false;

    [System.Serializable]
    public class SceneBGM
    {
        public string sceneName;
        public AudioClip bgmClip;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create audio sources
            sfxSource = gameObject.AddComponent<AudioSource>();
            bgmSource = gameObject.AddComponent<AudioSource>();
            eventSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            bgmSource.playOnAwake = false;
            eventSource.playOnAwake = false;

            bgmSource.loop = true;
            eventSource.loop = false;

            ApplyVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AttachButtonSounds();
        PlaySceneBGM(SceneManager.GetActiveScene().name);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachButtonSounds();
        PlaySceneBGM(scene.name);
    }

    private void ApplyVolumeSettings()
    {
        sfxSource.volume = sfxVolume;
        bgmSource.volume = bgmVolume;
        eventSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        sfxSource.volume = volume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        bgmSource.volume = volume;
        eventSource.volume = volume;
    }

    // 🔊 General SFX
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // 🖱️ Auto-attach click sounds
    private void AttachButtonSounds()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (Button btn in allButtons)
        {
            btn.onClick.RemoveListener(PlayButtonClick);
            btn.onClick.AddListener(PlayButtonClick);
        }
    }

    private void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    // 🎵 Per-scene BGM
    public void PlaySceneBGM(string sceneName)
    {
        if (isEventMusicPlaying) return; // prevent override while event music is playing

        currentSceneName = sceneName;
        SceneBGM found = sceneBGMs.Find(s => s.sceneName == sceneName);

        if (found != null && found.bgmClip != null)
        {
            if (currentBGM == found.bgmClip && bgmSource.isPlaying)
            {
                // Same clip — just keep playing
                return;
            }
            // Otherwise crossfade into new clip
            currentBGM = found.bgmClip;
            CrossfadeBGM(found.bgmClip);
        }
        else
        {
            // No BGM for this scene → fade out
            if (bgmSource.isPlaying)
                StartCoroutine(FadeOutAndStop(1f));
            currentBGM = null;
        }
    }

    // 🎶 Crossfade
    private void CrossfadeBGM(AudioClip newClip)
    {
        StartCoroutine(CrossfadeRoutine(newClip));
    }

    private System.Collections.IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        float fadeTime = 1f;
        float startVolume = bgmSource.volume;

        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t / fadeTime);
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }

    // 🌙 Fade out for silent scenes
    private System.Collections.IEnumerator FadeOutAndStop(float fadeTime)
    {
        float startVolume = bgmSource.volume;
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
    }

    // 🔁 Restart current BGM
    public void RestartCurrentBGM()
    {
        if (currentBGM != null)
        {
            bgmSource.Stop();
            bgmSource.clip = currentBGM;
            bgmSource.Play();
        }
    }

    // 🏁 Event Music Handlers ---------------------------------------

    /// <summary>
    /// Plays tutorial complete music once, pausing current BGM.
    /// </summary>
    public void PlayTutorialCompleteMusic()
    {
        PlayEventMusic(tutorialCompleteMusic);
    }

    /// <summary>
    /// Plays win music once, pausing current BGM.
    /// </summary>
    public void PlayWinMusic()
    {
        PlayEventMusic(winMusic);
    }

    /// <summary>
    /// Plays lose music once, pausing current BGM.
    /// </summary>
    public void PlayLoseMusic()
    {
        PlayEventMusic(loseMusic);
    }

    private void PlayEventMusic(AudioClip clip)
    {
        if (clip == null) return;

        StartCoroutine(PlayEventRoutine(clip));
    }

    private System.Collections.IEnumerator PlayEventRoutine(AudioClip clip)
    {
        isEventMusicPlaying = true;

        // Fade out background music first
        yield return StartCoroutine(FadeOutAndPauseBGM(0.5f));

        // Play the event clip
        eventSource.clip = clip;
        eventSource.Play();

        // Wait until event music finishes
        yield return new WaitWhile(() => eventSource.isPlaying);

        isEventMusicPlaying = false;

        // Resume background music if available
        if (currentBGM != null)
        {
            bgmSource.UnPause();
        }
    }

    private System.Collections.IEnumerator FadeOutAndPauseBGM(float fadeTime)
    {
        float startVolume = bgmSource.volume;

        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        bgmSource.Pause();
        bgmSource.volume = bgmVolume;
    }
}
