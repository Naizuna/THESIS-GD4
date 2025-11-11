using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    [Header("Dialogue Data")]
    public List<DialogueLine> lines = new();

    [Header("UI Components")]
    public TextMeshProUGUI textComponent;
    public TextMeshProUGUI pitchText;
    public Button repeatButton;
    public Button continueButton;
    public GameObject tutorialCompletePanel;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public ClipPlayer clipPlayer;

    [Header("Player Input Buttons")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private List<Button> pianoButtons;

    [Header("Answer Buttons")]
    public List<AnswerButton> answerButtons = new();

    [Header("Input Settings")]
    public int maxSelections = 3;
    public float textSpeed = 0.05f;

    [Header("Disabled Overlay")]
    [SerializeField] private GameObject pianoDisabledPanel;

    [Header("HealthBar UI")]
    [SerializeField] public Image healthBarUI;

    private int index;
    private bool isTyping;
    private List<string> selectedNotes = new();
    private bool showWrongMessage = false;


    void Start()
    {
        index = 0;
        textComponent.text = string.Empty;
        pitchText.text = string.Empty;

        repeatButton.onClick.AddListener(Repeat);
        continueButton.onClick.AddListener(SubmitAnswers);
        tutorialCompletePanel.SetActive(false);

        foreach (var btn in answerButtons)
        {
            btn.controller = this;
        }

        StartCoroutine(TypeLine());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopTyping();
            }
            else if (!AnyActionsButtonsVisible())
            {
                if (lines[index].waitForAudioToFinishBeforeButtons && clipPlayer.IsPlaying)
                    return;
                    
                NextLine();
            }
        }
    }

    //======== Typing ========
    IEnumerator TypeLine()
    {
        isTyping = true;

        SetPlayerInputInteractable(false);

        pitchText.text = string.Empty;
        textComponent.text = string.Empty;

        string lineToShow = showWrongMessage
        ? lines[index].wrongAnswerText 
        : lines[index].text;

        foreach (char c in lineToShow)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        lines[index].isTypingFinished = true;

        ApplyLineSettings();
    }

    void StopTyping()
    {
        StopAllCoroutines();
        textComponent.text = lines[index].text;
        isTyping = false;
        lines[index].isTypingFinished = true;

        ApplyLineSettings();
    }

    
    void NextLine()
    {
        clipPlayer.StopAllAudio();
        // Only move to the next line if not in wrong answer state
        if (showWrongMessage) return;

        if (index < lines.Count - 1)
        {
            index++;
            repeatButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);

            // ADD NULL CHECK
            if (healthBarUI != null)
            {
                healthBarUI.gameObject.SetActive(false);
            }

            ClearSelection();
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            gameObject.SetActive(false);
            EndTutorial();
        }
    }

    void ApplyLineSettings()
    {
        DialogueLine line = lines[index];

        // Audio
        if (line.pitchSounds != null && line.pitchSounds.Count > 0)
        {
            clipPlayer.PlayAllClips(line.pitchSounds);
        }

        // Pitch Label
        pitchText.text = string.IsNullOrEmpty(line.pitchLabel) ? string.Empty : line.pitchLabel;

        if (healthBarUI != null)
        {
            if (line.showSprite && line.spriteToShow != null)
            {
                healthBarUI.sprite = line.spriteToShow;
                healthBarUI.gameObject.SetActive(true);
            }
            else
            {
                healthBarUI.gameObject.SetActive(false);
            }
        }

        // Buttons
        if (line.waitForAudioToFinishBeforeButtons)
        {
            StartCoroutine(ShowButtonsWhenSoundIsDone(line));
        }
        else
        {
            // normal behavior (mini quiz)
            repeatButton.gameObject.SetActive(line.showRepeatButton);
            continueButton.gameObject.SetActive(line.showContinueButton);
        }

        SetPlayerInputInteractable(line.allowPlayerInput);
    }
    
    private IEnumerator ShowButtonsWhenSoundIsDone(DialogueLine line)
    {
        // Hide buttons first
        repeatButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);

        // Wait until ALL clip playback is finished
        while (clipPlayer.IsPlaying)
            yield return null;

        // Now show buttons
        repeatButton.gameObject.SetActive(line.showRepeatButton);
        continueButton.gameObject.SetActive(line.showContinueButton);
    }

    private void SetPlayerInputInteractable(bool value)
    {
        // Enable or disable submit and clear buttons
        submitButton.interactable = value;
        clearButton.interactable = value;

        // Toggle disabled overlay panel (this handles piano input lockout)
        if (pianoDisabledPanel != null)
            pianoDisabledPanel.SetActive(!value);
    }


    //========= Answer Selection ========
    public bool CanSelectMoreAnswers()
    {
        return selectedNotes.Count < maxSelections;
    }

    public void AddSelectedAnswer(string note)
    {
        if (!selectedNotes.Contains(note) && selectedNotes.Count < maxSelections)
        {
            selectedNotes.Add(note);
        }
    }

    public void RemoveSelectedAnswer(string note)
    {
        if (selectedNotes.Contains(note))
        {
            selectedNotes.Remove(note);
        }
    }

    public void ClearSelection()
    {
        selectedNotes.Clear();
        foreach (var btn in answerButtons)
        {
            btn.Deselect();
        }
    }

    public void SubmitAnswers()
    {
        bool correct = lines[index].CheckAnswer(selectedNotes);
        if (correct)
        {
            showWrongMessage = false;
            NextLine();
        }
        else
        {
            // Show the wrong-answer message and let the player try again
            showWrongMessage = true;
            ClearSelection();
            StartCoroutine(TypeLine());
            Debug.Log("Wrong! Try again.");
        }
    }

    //========= Utility ========
    bool AnyActionsButtonsVisible()
    {
        return repeatButton.gameObject.activeSelf || continueButton.gameObject.activeSelf;
    }

    void Repeat()
    {
        if (lines[index].pitchSounds != null && lines[index].pitchSounds.Count > 0)
        {
            clipPlayer.PlayAllClips(lines[index].pitchSounds);
        }
    }
    
    void EndTutorial()
    {
        SoundManager.Instance.PlayTutorialCompleteMusic();
        tutorialCompletePanel.SetActive(true);
        
        LevelCompletionManager.UnlockNextLevel();
    }
}
