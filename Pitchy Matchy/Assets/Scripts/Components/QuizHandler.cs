using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

//quiz handler will bridge the PianoHandler (player inputs), question answering and etc.
public class QuizHandler : MonoBehaviour
{
    [Header("Questions Loaded(visible for testing)")]
    [SerializeField] public List<QuestionComponent> questionsToAnswer; //will be dynamically filled up

    [Header("Question Bank")]
    [SerializeField] private QuestionsBank bank;
    [Header("Number of Questions")]
    [SerializeField] private int numberOfQuestions;
    [Header("Waiting Panel (Panel Shows when player submits answer)")]
    [SerializeField] private WaitingPanel wp;

    [Header("Question Texts")]
    [SerializeField] private TMP_Text questText;

    [Header("Clip Player")]
    [SerializeField] private ClipPlayer clipPlayer;

    [Header("Player & Enemy (TESTING)")]
    [SerializeField] private PlayerComponent player;
    [SerializeField] private EnemyComponent enemy;
    
    private List<string> playerAnswers = new List<string>();
    private int currQuestionIndex;

    private bool isSessionFinished;

    public void Start()
    {
        currQuestionIndex = 0;
        LoadRandomQuestions(numberOfQuestions);
        UpdateQuestionText();
    }

    public void UpdateQuestionText()
    {
        int num = questionsToAnswer[currQuestionIndex].GetNumberOfPitchesToAnswer();
        questText.text = $"Guess the {num} pitches correctly";
        PlayQuestionPitches();
    }

    public void SetEnemy(EnemyComponent enemy)
    {
        this.enemy = enemy;
    }

    public void PlayQuestionPitches()
    {
        List<AudioClip> clips = questionsToAnswer[currQuestionIndex].GetAudioClips();
        clipPlayer.PlayAllClips(clips);
    }

    public void Update()
    {
        if (currQuestionIndex == numberOfQuestions)
        {
            Debug.Log("all questions answered");
        }
    }

    public void ReceivePlayerAnswersAndProcess(List<string> answers)
    {
        if (isSessionFinished) return;

        playerAnswers = answers;
        Debug.Log("QUIZMANAGER GETPLAYERANSWERS Debug: ");
        foreach (var item in playerAnswers)
        {
            Debug.Log(item);
        }
        ProcessAnswer();
        InitiateWaitPanel();
    }

    public void LoadNextQuestion()
    {
        if (isSessionFinished) return;
        
        wp.HideParentPanel();
        currQuestionIndex++;
        this.playerAnswers.Clear();
        UpdateQuestionText();
    }

    private void InitiateWaitPanel()
    {
        wp.ExtractCurrentQuestionResult(questionsToAnswer[currQuestionIndex]);
        wp.ShowParentPanel();
    }

    private void ProcessAnswer()
    {
        questionsToAnswer[currQuestionIndex].playerAnswers = this.playerAnswers;
        questionsToAnswer[currQuestionIndex].CheckAnswers();

        if (questionsToAnswer[currQuestionIndex].isAnsweredCorrectly)
        {
            enemy.TakeDamage(player.GetAttackPower());
        }
        else
        {
            player.TakeDamage(enemy.GetAttackPower());
        }
    }

    public void LoadRandomQuestions(int numberOfQuestions)
    {

        for (int i = 0; i < numberOfQuestions; i++)
        {
            QuestionComponent.DifficultyClass randDifficulty = (QuestionComponent.DifficultyClass)UnityEngine.Random.Range(
                0,
                System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length - 1
            );

            questionsToAnswer.Add(bank.GetQuestionFromBank(randDifficulty));
        }
    }


}
