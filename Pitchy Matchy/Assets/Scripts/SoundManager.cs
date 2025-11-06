using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

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
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private AudioSource eventSource; // for special one-time musics

    private string currentSceneName;
    private AudioClip currentBGM;


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

            LoadVolumeSettings();
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
        // Stop any event music when switching scenes
        if (eventSource.isPlaying)
        {
            eventSource.Stop();
            eventSource.clip = null;
        }


        // Reattach button sounds for new UI
        AttachButtonSounds();

        // 🎵 Play the correct BGM for this scene
        PlaySceneBGM(scene.name);
    }

    // Volume control
    private void ApplyVolumeSettings()
    {
        sfxSource.volume = sfxVolume * masterVolume;
        bgmSource.volume = bgmVolume * masterVolume;
        eventSource.volume = bgmVolume * masterVolume;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        SaveVolumeSettings();
        ApplyVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        SaveVolumeSettings();
        ApplyVolumeSettings();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        SaveVolumeSettings();
        ApplyVolumeSettings();
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVol", masterVolume);
        PlayerPrefs.SetFloat("SFXVol", sfxVolume);
        PlayerPrefs.SetFloat("BGMVol", bgmVolume);
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVol", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVol", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVol", 0.5f);
    }

    // General SFX
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    // Auto-attach click sounds
    private void AttachButtonSounds()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isStageLesson = sceneName.Contains("Lesson");

        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (Button btn in allButtons)
        {

            if (isStageLesson && btn.GetComponent<AnswerButton>() != null)
                continue; // skip answer buttons in lessons

            btn.onClick.RemoveListener(PlayButtonClick);
            btn.onClick.AddListener(PlayButtonClick);
        }
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    // Per-scene BGM
    public void PlaySceneBGM(string sceneName)
    {
        currentSceneName = sceneName;
        SceneBGM found = sceneBGMs.Find(s => s.sceneName == sceneName);

        if (found == null || found.bgmClip == null)
        {
            if (bgmSource.isPlaying)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }
            currentBGM = null;
            return;
        }

        // Skip if currently playing same clip
        if (currentBGM == found.bgmClip && bgmSource.isPlaying)
            return;

        currentBGM = found.bgmClip;
        CrossfadeBGM(found.bgmClip);
    }
        

    // Crossfade
    private void CrossfadeBGM(AudioClip newClip)
    {
        StartCoroutine(CrossfadeRoutine(newClip));
    }

   private IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        float fadeTime = 1f;
        float startVolume = bgmSource.volume;

        bool hasExistingBGM = bgmSource.isPlaying && bgmSource.clip != null;

        // Only fade out if something is already playing
        if (hasExistingBGM)
        {
            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
                yield return null;
            }
        }

        // Switch and play immediately
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.volume = hasExistingBGM ? 0f : bgmVolume * masterVolume; // If no old BGM, start full
        bgmSource.Play();

        // Fade in only if we had to fade out before
        if (hasExistingBGM)
        {
            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0f, bgmVolume * masterVolume, t / fadeTime);
                yield return null;
            }
        }

        bgmSource.volume = bgmVolume * masterVolume;
    }

    // Restart current BGM
    public void RestartCurrentBGM()
    {
        if (currentBGM != null)
        {
            bgmSource.Stop();
            bgmSource.clip = currentBGM;
            bgmSource.Play();
        }
    }

    // Event Music Handlers ---------------------------------------

    public void PlayTutorialCompleteMusic() => PlayEventMusicInstant(tutorialCompleteMusic);
    public void PlayWinMusic() => PlayEventMusicInstant(winMusic);
    public void PlayLoseMusic() => PlayEventMusicInstant(loseMusic);

    private void PlayEventMusicInstant(AudioClip clip)
    {
        if (clip == null) return;

        // Immediately stop any BGM
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        // Instantly play event music
        eventSource.Stop();
        eventSource.clip = clip;
        eventSource.volume = bgmVolume * masterVolume;
        eventSource.loop = false;
        eventSource.Play();

        // Automatically resume previous BGM when done
        StartCoroutine(ResumeBGMWhenEventEnds());
    }

    private IEnumerator ResumeBGMWhenEventEnds()
    {
        yield return new WaitWhile(() => eventSource.isPlaying);

        // Resume last scene’s BGM
        if (currentBGM != null)
        {
            bgmSource.clip = currentBGM;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }
}
