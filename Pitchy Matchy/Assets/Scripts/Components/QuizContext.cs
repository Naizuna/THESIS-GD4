using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

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
    public DifficultySpriteChanger DifficultyUI;
    public KeysHighlighter keysHighlighter;

    // Mutable session state that handlers share via the context
    public List<QuestionComponent> QuestionsToAnswer { get; }
    public List<string> PlayerAnswers { get; set; }
    public int CurrQuestionIndex { get; set; }
    public int NumberOfQuestions { get; }
    public PlayerMetric PlyrMetric { get; }
    public QuizController handler { get; set; }

    //MCC related stuff
    public int MccQuestionsPerEpisode { get; }

    // Cumulative record across episodes (new)
    // stores all questions that have been presented & answered during the entire session
    public List<QuestionComponent> AllQuestionsAnswered { get; }

    // Response Time Tracking
    public List<float> ResponseTimes { get; private set; }
    private float questionStartTime;

    //Coroutine
    public MonoBehaviour coroutineRunner;

    public QuizContext(QuestionsBank bank,
                       WaitingPanel wp,
                       ScreensPanel sPanel,
                       PlayerComponent player,
                       EnemyComponent enemy,
                       ClipPlayer clipPlayer,
                       TMP_Text questText,
                       int numberOfQuestions,
                       KeysHighlighter keysHighlighter)
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
        this.keysHighlighter = keysHighlighter;

        // init cumulative list
        AllQuestionsAnswered = new List<QuestionComponent>();
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
                       int mccQuestionsPerEpisode,
                       KeysHighlighter keysHighlighter)
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
        this.keysHighlighter = keysHighlighter;

        // init cumulative list
        AllQuestionsAnswered = new List<QuestionComponent>();
    }

    public QuestionComponent GetCurrentQuestion()
    {
        if (CurrQuestionIndex < 0 || CurrQuestionIndex >= QuestionsToAnswer.Count) return null;
        return QuestionsToAnswer[CurrQuestionIndex];
    }

    public void enablePlayerInput(bool boolean)
    {
        if (handler == null) return;

        handler.playerInputEnabled = boolean;
    }

    public void AddQuestion(QuestionComponent q) => QuestionsToAnswer.Add(q);

    public void UpdateQuestionText()
    {
        var q = GetCurrentQuestion();
        if (q == null) { QuestText.text = "no question"; return; }
        int num = q.GetNumberOfPitchesToAnswer();

        if (num >= 2)
        {
            QuestText.text = $"guess the {num} pitches";
        }
        else
        {
            QuestText.text = $"guess the {num} pitch";
        }

        // Reset response time tracking for the new question
        StartQuestionTimer();
    }

    public void ShowCorrectAnswers()
    {
        QuestionComponent q = GetCurrentQuestion();
        if (q == null) return;
        int count = q.correctAnswers.Count;

        // Singular vs plural
        string label = (count == 1) ? "answer:" : "answers:";

        QuestText.text = label;

        foreach (string pitch in q.correctAnswers)
        {
            QuestText.text += " " + pitch;
        }
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
        // If cumulative list has items, use it so MCC shows total accuracy across episodes.
        if (AllQuestionsAnswered != null && AllQuestionsAnswered.Count > 0)
        {
            PlyrMetric.SetQuestionsAnswered(AllQuestionsAnswered);
        }
        else
        {
            // fallback (Normal / SARSA behaviour)
            PlyrMetric.SetQuestionsAnswered(QuestionsToAnswer);
        }

        PlyrMetric.CalculateTotalAccuracy();
        PlyrMetric.CalculateDifficultyAccuracy();
    }

    public void PrintPlayerMetrics()
    {
        PlyrMetric.TestPrint();
    }

    public void ExportPlayerMetricsCSV(string filename)
    {
        PlyrMetric.levelName = SceneManager.GetActiveScene().name;
        PlyrMetric.WriteToFile(filename);
    }
    public void SetEnemy(EnemyComponent enemy)
    {
        Enemy = enemy;
    }

    public void SetCoroutineRunner(MonoBehaviour runner)
    {
        coroutineRunner = runner;
    }
}
