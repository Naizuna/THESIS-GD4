using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed = 0.05f;

    [Header("Pitch Sound Settings")]
    public AudioClip[] pitchSounds;
    private AudioClip lastPlayedClip;
    public string[] pitchLabels;

    [Header("Per-Line UI Controls")]
    public bool[] showRepeatButton;
    public bool[] showContinueButton;

    [Header("UI Elements")]
    public TextMeshProUGUI pitchText;
    public Button repeatButton;
    public Button continueButton;
    public GameObject tutorialCompletePanel;


    [Header("Audio")]
    public AudioSource audioSource;

    private int index;
    private bool isTyping = false;

    void Start()
    {
        index = 0;
        textComponent.text = string.Empty;
        pitchText.text = string.Empty;

        repeatButton.onClick.AddListener(Repeat);
        continueButton.onClick.AddListener(Continue);
        tutorialCompletePanel.SetActive(false);

        StartCoroutine(TypeLine());
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
                isTyping = false;

                PlayCurrentLineSound();
                UpdatePitchLabel();
                HandleOptionalButtons();
            }
            else
            {
                if (continueButton.gameObject.activeSelf || repeatButton.gameObject.activeSelf)
                    return;

                NextLine();
            }
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;

        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;

        PlayCurrentLineSound();
        UpdatePitchLabel();
        HandleOptionalButtons();
    }
    
    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            repeatButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            gameObject.SetActive(false);
            EndTutorial();
        }
    }

    void HandleOptionalButtons()
    {
        bool showRepeat = showRepeatButton != null && index < showRepeatButton.Length && showRepeatButton[index];
        bool showContinue = showContinueButton != null && index < showContinueButton.Length && showContinueButton[index];

        repeatButton.gameObject.SetActive(showRepeat);
        continueButton.gameObject.SetActive(showContinue);
    }

    void Repeat()
    {
        if (lastPlayedClip != null)
        {
            audioSource.Stop();
            audioSource.clip = lastPlayedClip;
            audioSource.Play(); 
        }
    }

    void Continue()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            textComponent.text = lines[index];
            isTyping = false;

            PlayCurrentLineSound();
            UpdatePitchLabel();
        }
        else
        {
            NextLine();
        }
    }

    void PlayCurrentLineSound()
    {
        if (pitchSounds != null && index < pitchSounds.Length && pitchSounds[index] != null)
        {
            lastPlayedClip = pitchSounds[index];
            audioSource.Stop();
            audioSource.clip = pitchSounds[index];
            audioSource.Play();
        }
    }

    void UpdatePitchLabel()
    {
        if (pitchLabels != null && index < pitchLabels.Length)
        {
            pitchText.text = pitchLabels[index];
        }
        else
        {
            pitchText.text = string.Empty;
        }
    }

    void EndTutorial()
    {
        tutorialCompletePanel.SetActive(true);
    }
}
