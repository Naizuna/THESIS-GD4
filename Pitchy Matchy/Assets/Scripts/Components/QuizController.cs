using UnityEngine;
using System.Collections.Generic;

public class QuizController : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private QuizMode quizMode;

    [Header("Scene References")]
    [SerializeField] private QuestionsBank bank;
    [SerializeField] private WaitingPanel wp;
    [SerializeField] private ScreensPanel sPanel;
    [SerializeField] private PlayerComponent player;
    [SerializeField] private EnemyComponent enemy;
    [SerializeField] private ClipPlayer clipPlayer;
    [SerializeField] private TMPro.TMP_Text questText;
    [SerializeField] private int numberOfQuestions;

    [Header("Monte Carlo Control Options (only valid if MCC mode)")]
    [SerializeField] private int mccQuestionsPerEpisode;

    private QuizContext ctx;
    private IQuizHandler handler;
    private bool HasVictoryOrDefeatScreensShown = false;

    void Awake()
    {
        if (quizMode == QuizMode.Normal)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions);
            handler = new NormalQuizHandler(ctx);
        }
        else if (quizMode == QuizMode.MonteCarloControl)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, mccQuestionsPerEpisode);
            handler = new MonteCarloQuizHandler(ctx, new MonteCarloAgent()); // or inject agent instance
        }
        else if (quizMode == QuizMode.Sarsa)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions);
            handler = new SARSAQuizHandler(ctx, new SARSAController());
        }
    }

    void Start()
    {
        sPanel.HideParentPanel();
        handler.StartQuiz();
    }

    void Update()
    {
        // central checks (player death / victory)
        if (HasVictoryOrDefeatScreensShown) return;

        if (player.IsPlayerDefeated())
        {
            HandlePlayerDefeat();
            HasVictoryOrDefeatScreensShown = true;
            handler.IsSessionFinished = true;
            return;
        }

        if (handler.IsSessionFinished)
        {
            HandlePlayerVictory();
            HasVictoryOrDefeatScreensShown = true;
            handler.IsSessionFinished = true;
            return;
        }
        handler.Update();
    }

    private void HandlePlayerDefeat()
    {
        sPanel.SetLoseScreen();
        sPanel.ShowParentPanel();
        ctx.UpdatePlayerMetrics();
        ctx.PrintPlayerMetrics();
    }

    private void HandlePlayerVictory()
    {
        sPanel.SetWinScreen();
        sPanel.ShowParentPanel();
        ctx.UpdatePlayerMetrics();
        ctx.PrintPlayerMetrics();
    }
    
    public void SetEnemy(EnemyComponent enemy)
    {
        ctx.SetEnemy(enemy);
    }

    // Called from UI to submit answers
    public void OnPlayerSubmitAnswers(List<string> answers)
    {
        handler.ReceivePlayerAnswers(answers);
    }

    public void PlayCurrentPitch()
    {
        ctx.PlayCurrentQuestionPitches();
    }

    // Called by UI to request next question (or handler can call it)
    public void OnRequestNextQuestion() => handler.LoadNextQuestion();
}
