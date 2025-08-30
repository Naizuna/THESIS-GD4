using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

//quiz handler will bridge the PianoHandler (player inputs), question answering and etc.
public class SARSAQuizHandler : MonoBehaviour
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

    private SARSAController sarsaAgent = new SARSAController();
    private List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode
        = new List<(string, QuestionComponent.DifficultyClass, float)>();

    [SerializeField] private int totalQuestions = 5;

    public void Start()
    {
        currQuestionIndex = 0;
        LoadNextQuestion();
    }

    public void UpdateQuestionText()
    {
        int num = questionsToAnswer[currQuestionIndex].GetNumberOfPitchesToAnswer();
        questText.text = $"Guess the {num} pitches correctly";
        PlayQuestionPitches();
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
        ProcessAnswer();
        InitiateWaitPanel();
    }

    public void LoadNextQuestion()
    {
        if (isSessionFinished) return;

        if (currQuestionIndex >= totalQuestions)
        {
            isSessionFinished = true;
            return;
        }


        string state = GetCurrentState();
        var action = sarsaAgent.ChooseAction(state);

        var nextQuestion = bank.GetQuestionFromBank(action);
        questionsToAnswer.Add(nextQuestion);
        currQuestionIndex = questionsToAnswer.Count - 1;

        wp.HideParentPanel();
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
        // questionsToAnswer[currQuestionIndex].playerAnswers = this.playerAnswers;
        // questionsToAnswer[currQuestionIndex].CheckAnswers();
        var currQ = questionsToAnswer[currQuestionIndex];
        currQ.playerAnswers = playerAnswers;
        currQ.CheckAnswers();
        float reward = currQ.isAnsweredCorrectly ? 1f : -1f;
        string state = GetCurrentState();

        // Store current state and action for SARSA update
        var currentAction = questionsToAnswer[currQuestionIndex].questionDifficulty;

        // Get next state and action
        string nextState = GetNextState();
        var nextAction = sarsaAgent.ChooseAction(nextState);

        // Update SARSA agent
        sarsaAgent.UpdateQValue(state, currentAction, reward, nextState, nextAction);

        if (questionsToAnswer[currQuestionIndex].isAnsweredCorrectly)
        {
            enemy.TakeDamage(player.GetAttackPower());
        }
        else
        {
            player.TakeDamage(enemy.GetAttackPower());
        }
    }


    private string GetCurrentState()
    {
        if (currQuestionIndex == 0) return "START";

        float accuracy = (float)questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / currQuestionIndex;
        if (accuracy < 0.4f) return "LOW";
        if (accuracy < 0.7f) return "MEDIUM";
        return "HIGH";
    }
    private string GetNextState()
    {
        if (currQuestionIndex >= totalQuestions - 1) return "TERMINAL";

        // Predict next state based on current performance
        int correctAnswers = questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
        float projectedAccuracy = (float)(correctAnswers + 1) / (currQuestionIndex + 2); // +1 for optimistic projection

        if (projectedAccuracy < 0.4f) return "LOW";
        if (projectedAccuracy < 0.7f) return "MEDIUM";
        return "HIGH";
    }

}
