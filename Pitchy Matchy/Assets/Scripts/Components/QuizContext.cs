using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Video;

public class QuizContext
{
    // Scene dependencies
    public QuestionsBank Bank { get; }
    public WaitingPanel WP { get; }
    public ScreensPanel SPanel { get; }
    public PlayerComponent Player { get; }
    public EnemyComponent Enemy { get; private set; }
    public ClipPlayer ClipPlayer { get; }
    public TMP_Text QuestText { get; }

    // Mutable session state that handlers share via the context
    public List<QuestionComponent> QuestionsToAnswer { get; }
    public List<string> PlayerAnswers { get; set; }
    public int CurrQuestionIndex { get; set; }
    public int NumberOfQuestions { get; }
    public PlayerMetric PlyrMetric { get; }

    //MCC related stuff
    public int MccQuestionsPerEpisode { get; }

    // Response Time Tracking
    public List<float> ResponseTimes { get; private set; }
    private float questionStartTime;

    public QuizContext(QuestionsBank bank,
                       WaitingPanel wp,
                       ScreensPanel sPanel,
                       PlayerComponent player,
                       EnemyComponent enemy,
                       ClipPlayer clipPlayer,
                       TMP_Text questText,
                       int numberOfQuestions)
    {
        Bank = bank;
        WP = wp;
        SPanel = sPanel;
        Player = player;
        Enemy = enemy;
        ClipPlayer = clipPlayer;
        QuestText = questText;
        NumberOfQuestions = numberOfQuestions;

        QuestionsToAnswer = new List<QuestionComponent>();
        PlayerAnswers = new List<string>();
        PlyrMetric = new PlayerMetric();
        CurrQuestionIndex = 0;

        ResponseTimes = new List<float>();
        questionStartTime = Time.time;
    }

    //mcc variant constructor
    public QuizContext(QuestionsBank bank,
                       WaitingPanel wp,
                       ScreensPanel sPanel,
                       PlayerComponent player,
                       EnemyComponent enemy,
                       ClipPlayer clipPlayer,
                       TMP_Text questText,
                       int numberOfQuestions,
                       int mccQuestionsPerEpisode)
    {
        Bank = bank;
        WP = wp;
        SPanel = sPanel;
        Player = player;
        Enemy = enemy;
        ClipPlayer = clipPlayer;
        QuestText = questText;
        NumberOfQuestions = numberOfQuestions;
        MccQuestionsPerEpisode = mccQuestionsPerEpisode;


        QuestionsToAnswer = new List<QuestionComponent>();
        PlayerAnswers = new List<string>();
        PlyrMetric = new PlayerMetric();
        CurrQuestionIndex = 0;

        ResponseTimes = new List<float>();
        questionStartTime = Time.time;
    }

    public QuestionComponent GetCurrentQuestion()
    {
        if (CurrQuestionIndex < 0 || CurrQuestionIndex >= QuestionsToAnswer.Count) return null;
        return QuestionsToAnswer[CurrQuestionIndex];
    }

    public void AddQuestion(QuestionComponent q) => QuestionsToAnswer.Add(q);

    public void UpdateQuestionText()
    {
        var q = GetCurrentQuestion();
        if (q == null) { QuestText.text = "No question"; return; }
        int num = q.GetNumberOfPitchesToAnswer();
        QuestText.text = $"guess the {num} pitches";

        // Reset response time tracking for the new question
        StartQuestionTimer();
    }

    public void PlayCurrentQuestionPitches()
    {
        var q = GetCurrentQuestion();
        if (q == null) return;
        ClipPlayer.PlayAllClips(q.GetAudioClips());
    }

    public void StartQuestionTimer()
    {
        questionStartTime = Time.time;
    }

    public void RecordResponseTime()
    {
        float responseTime = Time.time - questionStartTime;
        ResponseTimes.Add(responseTime);
        Debug.Log($"Response Time for Question {CurrQuestionIndex + 1}: {responseTime:F2} seconds");
    }

    public void UpdatePlayerMetrics()
    {
        PlyrMetric.SetQuestionsAnswered(QuestionsToAnswer);
        PlyrMetric.CalculateTotalAccuracy();
        PlyrMetric.CalculateDifficultyAccuracy();
    }

    public void PrintPlayerMetrics()
    {
        PlyrMetric.TestPrint();
    }

    public void ExportPlayerMetricsCSV()
    {
        PlyrMetric.levelName = "levelTest";
        PlyrMetric.WriteToFile("test");
    }
    public void SetEnemy(EnemyComponent enemy)
    {
        Enemy = enemy;
    }
}
