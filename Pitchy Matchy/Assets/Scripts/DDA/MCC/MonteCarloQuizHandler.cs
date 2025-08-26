using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MonteCarloQuizHandler : MonoBehaviour
{
    [Header("Questions Loaded")]
    [SerializeField] public List<QuestionComponent> questionsToAnswer;

    [Header("Question Bank")]
    [SerializeField] private QuestionsBank bank;
    [SerializeField] private int numberOfQuestions;
    [SerializeField] private WaitingPanel wp;

    [Header("Episode Settings")]
    [SerializeField] private int totalQuestions = 30;
    [SerializeField] private int questionsPerEpisode = 5;
    private int questionsAskedInCurrentEpisode = 0;

    [Header("Question Texts")]
    [SerializeField] private TMP_Text questText;

    [Header("Clip Player")]
    [SerializeField] private ClipPlayer clipPlayer;

    [Header("Player & Enemy")]
    [SerializeField] private PlayerComponent player;
    [SerializeField] private EnemyComponent enemy;

    private List<string> playerAnswers = new List<string>();
    private int currQuestionIndex;
    private bool isSessionFinished;

    private MonteCarloAgent mcAgent = new MonteCarloAgent();
    private List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode
        = new List<(string, QuestionComponent.DifficultyClass, float)>();

    void Start()
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

    public void ReceivePlayerAnswersAndProcess(List<string> answers)
    {
        if (isSessionFinished) return;

        playerAnswers = answers;
        ProcessAnswers();
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
        var action = mcAgent.ChooseAction(state);

        var nextQuestion = bank.GetQuestionFromBank(action);
        questionsToAnswer.Add(nextQuestion);

        currQuestionIndex = questionsToAnswer.Count - 1;
        questionsAskedInCurrentEpisode++;

        wp.HideParentPanel();
        this.playerAnswers.Clear();
        UpdateQuestionText();
        
    }

    private void InitiateWaitPanel()
    {
        wp.ExtractCurrentQuestionResult(questionsToAnswer[currQuestionIndex]);
        wp.ShowParentPanel();
    }

    private void ProcessAnswers()
    {
        var currQ = questionsToAnswer[currQuestionIndex];
        currQ.playerAnswers = playerAnswers;
        currQ.CheckAnswers();

        float reward = currQ.isAnsweredCorrectly ? 1f : -1f;
        string state = GetCurrentState();
        episode.Add((state, currQ.questionDifficulty, reward));

        // Track accuracy for debugging/analysis purposes
        float accuracy = (float)questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / (currQuestionIndex + 1);
         Debug.Log($"Current Accuracy: {accuracy * 100f}% after {currQuestionIndex + 1} questions.");

        if (currQ.isAnsweredCorrectly)
            enemy.TakeDamage(player.GetAttackPower());
        else
            player.TakeDamage(enemy.GetAttackPower());
        if (questionsAskedInCurrentEpisode >= questionsPerEpisode)
        {
            EndEpisode();
            questionsAskedInCurrentEpisode = 0;
        }

        if (currQuestionIndex >= totalQuestions)
        {
            isSessionFinished = true;
            if (episode.Count > 0)
            {
                EndEpisode();
            }    
            Debug.Log("All questions answered. Session finished.");
            return;
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

    private void EndEpisode()
    {
        mcAgent.UpdatePolicy(episode);
        episode.Clear();
        Debug.Log("Episode finished. Policy updated.");

        int correct = questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
        int total = questionsToAnswer.Count;
        float finalAccuracy = (float)correct / total;
        Debug.Log($"Episode finished. Final Accuracy: {finalAccuracy * 100f}% ({correct}/{total} correct).");
    }

}
