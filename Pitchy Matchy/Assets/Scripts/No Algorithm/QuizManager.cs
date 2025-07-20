using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    [Header("QnA Settings")]
    public List<QNA> QnA;
    public GameObject[] options;
    public int currentQuestion;
    public TextMeshProUGUI questionTxt;
    public AudioSource audioSource;

    [Header("Level Complete")]
    public GameObject levelCompletePanel;

    [Header("Reference Pitch")]
    public TextMeshProUGUI referencePitchLabel;
    public AudioClip referencePitch;
    public string referencePitchName;

    [Header("Confiramtion Button")]
    public GameObject confirmButton;
    public AudioClip confirmSound;
    private int selectedAnswerIndex = -1;

    void Start()
    {
        UpdateReferencePitchLabel();
        generateQuestion();
    }

    void SetAnswers()
    {
        for (int i = 0; i < options.Length; i++)
        {
            options[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = QnA[currentQuestion].answers[i];

            var answerScript = options[i].GetComponent<AnswerScript>();
            answerScript.answerIndex = i;
            answerScript.quizManager = this;

            // Reset button colors
            options[i].GetComponent<UnityEngine.UI.Image>().color = Color.white;
        }
    }

    void generateQuestion()
    {
        if (QnA.Count == 0)
        {
            Debug.Log("All questions done!");
            levelCompletePanel.SetActive(true);
            return;
        }

        currentQuestion = Random.Range(0, QnA.Count);
        questionTxt.text = QnA[currentQuestion].questions;
        SetAnswers();
        PlayCurrentQuestionSound();
        confirmButton.SetActive(false);
    }

    public void PlayCurrentQuestionSound()
    {
        if (audioSource != null && QnA[currentQuestion].soundClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(QnA[currentQuestion].soundClip);
        }
    }

    public void PlayReferecePitch()
    {
        if (audioSource != null && referencePitch != null)
        {
            audioSource.PlayOneShot(referencePitch);
        }
    }

    void UpdateReferencePitchLabel()
    {
        if (referencePitchLabel != null)
        {
            referencePitchLabel.text = "Reference Pitch: " + referencePitchName;
        }
    }

    public void SelectAnswer(int index)
    {
        selectedAnswerIndex = index;

        for (int i = 0; i < options.Length; i++)
        {
            // Button click color settings
            options[i].GetComponent<UnityEngine.UI.Image>().color = (i == index) ? Color.green : Color.white;
        }

        confirmButton.SetActive(true);
    }

    public void ConfirmAnswer()
    {
        if (selectedAnswerIndex == -1)
            return;

        confirmButton.SetActive(false);
        bool isCorrect = (QnA[currentQuestion].correctAnswer == selectedAnswerIndex + 1);
        Debug.Log(isCorrect ? "Correct!" : "Wrong!");

        StartCoroutine(HandleAnswerConfirmation(isCorrect));
    }

    IEnumerator HandleAnswerConfirmation(bool isCorrect)
    {
        if (audioSource != null && confirmSound != null)
        {
            audioSource.PlayOneShot(confirmSound);
            yield return new WaitForSeconds(confirmSound.length);
        }

        QnA.RemoveAt(currentQuestion);

        selectedAnswerIndex = -1;
        generateQuestion();
    }
}
