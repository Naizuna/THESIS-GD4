using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// Merged QuizHandler with imported MonteCarloQuizHandler members.
// Duplicate fields/methods imported from MonteCarloQuizHandler are renamed with an "_MCC" suffix
// (MCC = Monte Carlo Conflict) so both implementations coexist and are easy to find.
// You can later remove/merge duplicates as needed or call the _MCC variants directly.

public class QuizHandlerMerge : MonoBehaviour
{
    [Header("Questions Loaded(visible for testing)")]
    [SerializeField] public List<QuestionComponent> questionsToAnswer; //will be dynamically filled up

    [Header("Question Bank")]
    [SerializeField] private QuestionsBank bank;

    [Header("Number of Questions")]
    [SerializeField] private int numberOfQuestions;

    [Header("Waiting Panel (Panel Shows when player submits answer)")]
    [SerializeField] private WaitingPanel wp;

    [Header("Victory and Lose Screens")]
    [SerializeField] private ScreensPanel sPanel;

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

    //player metrics
    private PlayerMetric playerMetric = new PlayerMetric();

    // ==========================
    // Imported from MonteCarloQuizHandler (marked with _MCC)
    // ==========================

    [Header("Episode Settings (MCC)")]
    [SerializeField] private int totalQuestions_MCC = 30; // MCC: imported field from MonteCarloQuizHandler
    [SerializeField] private int questionsPerEpisode_MCC = 5; // MCC
    private int questionsAskedInCurrentEpisode_MCC = 0; // MCC

    [Header("Question Bank (MCC)")]
    [SerializeField] private int numberOfQuestions_MCC; // MCC: note duplicate name

    [Header("Clip Player (MCC)")]
    // clipPlayer already exists above; imported duplicates are not re-declared

    // Monte Carlo agent and episode memory
    private MonteCarloAgent mcAgent_MCC = new MonteCarloAgent(); // MCC: imported
    private List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode_MCC
        = new List<(string, QuestionComponent.DifficultyClass, float)>(); // MCC

    // ==========================
    // End of imported members
    // ==========================

    public void Start()
    {
        // Original QuizHandler Start behaviour
        currQuestionIndex = 0;
        LoadRandomQuestions(numberOfQuestions);
        UpdateQuestionText();

        // If you want to initialise MonteCarlo-specific state as well, call Start_MCC();
        // Start_MCC();
    }

    public void Update()
    {
        CheckPlayerStatus();
    }

    public void CheckPlayerStatus()
    {
        if (isSessionFinished) return;

        if (player.IsPlayerDefeated())
        {
            PlayerDefeat();
            isSessionFinished = true;
            return;
        }
        else if (currQuestionIndex == numberOfQuestions)
        {
            PlayerVictory();
            isSessionFinished = true;
            return;
        }
    }

    public void PlayerDefeat()
    {
        Debug.Log("Player Defeat");

        playerMetric.SetQuestionsAnswered(questionsToAnswer);
        playerMetric.CalculateTotalAccuracy();
        playerMetric.TestPrint();
    }

    public void PlayerVictory()
    {
        Debug.Log("Player Victory");

        playerMetric.SetQuestionsAnswered(questionsToAnswer);
        playerMetric.CalculateTotalAccuracy();
        playerMetric.TestPrint();
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
        //InitiateWaitPanel();
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
        questionsToAnswer[currQuestionIndex].playerAnswers = new List<string>(this.playerAnswers);
        questionsToAnswer[currQuestionIndex].CheckAnswers();

        if (questionsToAnswer[currQuestionIndex].isAnsweredCorrectly)
        {
            enemy.TakeDamage(player.GetAttackPower());
        }
        else
        {
            player.TakeDamage(enemy.GetAttackPower());
        }

        LoadNextQuestion();
    }

    public void LoadRandomQuestions(int numberOfQuestions)
    {

        for (int i = 0; i < numberOfQuestions; i++)
        {
            QuestionComponent.DifficultyClass randDifficulty = (QuestionComponent.DifficultyClass)UnityEngine.Random.Range(
                0,
                System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length
            );
            Debug.Log(randDifficulty);
            questionsToAnswer.Add(bank.GetQuestionFromBank(randDifficulty));
        }
    }

    // ==========================
    // MonteCarlo imported methods (renamed with _MCC suffix)
    // Call these if you want MonteCarlo-driven behaviour.
    // ==========================

    // MCC: original MonteCarlo Start renamed to avoid collision with MonoBehaviour.Start
    public void Start_MCC()
    {
        currQuestionIndex = 0;
        LoadNextQuestion_MCC();
    }

    // MCC: UpdateQuestionText (duplicate) -> renamed
    public void UpdateQuestionText_MCC()
    {
        int num = questionsToAnswer[currQuestionIndex].GetNumberOfPitchesToAnswer();
        questText.text = $"Guess the {num} pitches correctly";
        PlayQuestionPitches_MCC();
    }

    // MCC: PlayQuestionPitches (duplicate) -> renamed
    public void PlayQuestionPitches_MCC()
    {
        List<AudioClip> clips = questionsToAnswer[currQuestionIndex].GetAudioClips();
        clipPlayer.PlayAllClips(clips);
    }

    // MCC: ReceivePlayerAnswersAndProcess (duplicate) -> renamed
    public void ReceivePlayerAnswersAndProcess_MCC(List<string> answers)
    {
        if (isSessionFinished) return;

        playerAnswers = answers;
        ProcessAnswers_MCC();
        InitiateWaitPanel();
    }

    // MCC: LoadNextQuestion (duplicate) -> renamed and changed to use MonteCarlo agent
    public void LoadNextQuestion_MCC()
    {
        if (isSessionFinished) return;

        if (currQuestionIndex >= totalQuestions_MCC)
        {
            isSessionFinished = true;
            return;
        }

        string state = GetCurrentState_MCC();
        var action = mcAgent_MCC.ChooseAction(state);

        var nextQuestion = bank.GetQuestionFromBank(action);
        questionsToAnswer.Add(nextQuestion);

        currQuestionIndex = questionsToAnswer.Count - 1;
        questionsAskedInCurrentEpisode_MCC++;

        wp.HideParentPanel();
        this.playerAnswers.Clear();
        UpdateQuestionText_MCC();

    }

    private void InitiateWaitPanel_MCC()
    {
        wp.ExtractCurrentQuestionResult(questionsToAnswer[currQuestionIndex]);
        wp.ShowParentPanel();
    }

    private void ProcessAnswers_MCC()
    {
        var currQ = questionsToAnswer[currQuestionIndex];
        currQ.playerAnswers = playerAnswers;
        currQ.CheckAnswers();

        float reward = currQ.isAnsweredCorrectly ? 1f : -1f;
        string state = GetCurrentState_MCC();
        episode_MCC.Add((state, currQ.questionDifficulty, reward));

        // Track accuracy for debugging/analysis purposes
        float accuracy = (float)questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / (currQuestionIndex + 1);
        Debug.Log($"Current Accuracy: {accuracy * 100f}% after {currQuestionIndex + 1} questions.");

        if (currQ.isAnsweredCorrectly)
            enemy.TakeDamage(player.GetAttackPower());
        else
            player.TakeDamage(enemy.GetAttackPower());
        if (questionsAskedInCurrentEpisode_MCC >= questionsPerEpisode_MCC)
        {
            EndEpisode_MCC();
            questionsAskedInCurrentEpisode_MCC = 0;
        }

        if (currQuestionIndex >= totalQuestions_MCC)
        {
            isSessionFinished = true;
            if (episode_MCC.Count > 0)
            {
                EndEpisode_MCC();
            }
            Debug.Log("All questions answered. Session finished.");
            return;
        }
    }

    private string GetCurrentState_MCC()
    {
        if (currQuestionIndex == 0) return "START";

        float accuracy = (float)questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / currQuestionIndex;
        if (accuracy < 0.4f) return "LOW";
        if (accuracy < 0.7f) return "MEDIUM";
        return "HIGH";
    }

    private void EndEpisode_MCC()
    {
        mcAgent_MCC.UpdatePolicy(episode_MCC);
        episode_MCC.Clear();
        Debug.Log("Episode finished. Policy updated.");

        int correct = questionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
        int total = questionsToAnswer.Count;
        float finalAccuracy = (float)correct / total;
        Debug.Log($"Episode finished. Final Accuracy: {finalAccuracy * 100f}% ({correct}/{total} correct).");
    }

    // ==========================
    // End MonteCarlo imported methods
    // ==========================

}
