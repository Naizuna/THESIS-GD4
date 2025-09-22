using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        QuestText.text = $"Guess the {num} pitches correctly";
    }

    public void PlayCurrentQuestionPitches()
    {
        var q = GetCurrentQuestion();
        if (q == null) return;
        ClipPlayer.PlayAllClips(q.GetAudioClips());
    }

    public void UpdatePlayerMetrics()
    {
        PlyrMetric.SetQuestionsAnswered(QuestionsToAnswer);
        PlyrMetric.CalculateTotalAccuracy();
    }

    public void PrintPlayerMetrics()
    {
        PlyrMetric.TestPrint();
    }
    public void SetEnemy(EnemyComponent enemy)
    {
        Enemy = enemy;
    }
}
