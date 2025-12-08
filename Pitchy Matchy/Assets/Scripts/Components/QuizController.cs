using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class QuizController : MonoBehaviour
{
    // [Header("Mode")]
    // [SerializeField] private QuizMode quizMode;

    public enum QuizType { Mini, Final }

    [Header("Quiz Settings")]
    [SerializeField] private QuizType quizType;
    [SerializeField] private int numberOfCorrectStreakForImmunity;

    [Header("Scene References")]
    [SerializeField] private QuestionsBank bank;
    [SerializeField] private WaitingPanel wp;
    [SerializeField] private ScreensPanel sPanel;
    [SerializeField] private PlayerComponent player;
    [SerializeField] private GameObject PlayerObject;
    [SerializeField] private EnemyComponent enemy;
    [SerializeField] private ClipPlayer clipPlayer;
    [SerializeField] private TMPro.TMP_Text questText;
    [SerializeField] private TMPro.TMP_Text responseTimeText;
    [SerializeField] private int numberOfQuestions;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private KeysHighlighter keysHighlighter;
    [SerializeField] private TimerBar timerBar;


    [Header("Reference Pitch")]
    [SerializeField] private AudioClip referencePitch;
    [SerializeField] private Button referencePitchButton;
    [SerializeField] private Image referencePitchSprite;

    [Header("Monte Carlo Control Options (only valid if MCC mode)")]
    [SerializeField] private int mccQuestionsPerEpisode;
    [SerializeField] List<QuestionComponent> viewQuestions;

    [Header("Difficulty Sprite Changer")]
    [SerializeField] private DifficultySpriteChanger difficultySpriteChanger;


    public QuizContext ctx { private set; get; }
    private IQuizHandler handler;
    private bool HasVictoryOrDefeatScreensShown = false;
    public bool playerInputEnabled { get; set; }
    private GameObject enemyObject;

    void Awake()
    {
        // Create context (always the same unless MCC needs extra)
        if (GameVersionManager.Instance.SelectedVersion == GameVersionManager.VersionType.MCC)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, mccQuestionsPerEpisode, keysHighlighter);

            var mcAgent = new MonteCarloAgent();

            // Load saved experience
            bool hadPreviousData = RLPersistenceManager.Instance.LoadMonteCarloAgent(mcAgent);

            // If this isn't the first stage, bump exploration slightly
            if (hadPreviousData)
            {
                mcAgent.OnNewStageMicroBump(); // or OnNewStageConservative() / OnNewStageAggressive()
            }

            handler = new MonteCarloQuizHandler(ctx, mcAgent);
        }
        else if (GameVersionManager.Instance.SelectedVersion == GameVersionManager.VersionType.SARSA)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, keysHighlighter);

            var sarsaAgent = new SARSAController();

            // Load saved experience
            bool hadPreviousData = RLPersistenceManager.Instance.LoadSARSAAgent(sarsaAgent);

            // If this isn't the first stage, bump exploration slightly
            if (hadPreviousData)
            {
                sarsaAgent.OnNewStageMicroBump(); // or OnNewStageConservative() / OnNewStageAggressive()
            }

            handler = new SARSAQuizHandler(ctx, sarsaAgent);
        }
        else // Normal (Control Group)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, keysHighlighter);
            handler = new NormalQuizHandler(ctx);
        }

        ctx.SetCoroutineRunner(this);
        ctx.DifficultyUI = difficultySpriteChanger;
        ctx.correctStreakMAX = numberOfCorrectStreakForImmunity;
        ctx.handler = this;
        ctx.ResponseTimeText = responseTimeText;
    }
    void Start()
    {
        handler.StartQuiz();
        UpdateDifficultyVisual();
        UpdateReferencePitchButton();
        playerInputEnabled = true;
    }

    void Update()
    {
        viewQuestions = ctx.QuestionsToAnswer;
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
        UpdateDifficultyVisual();
        UpdateReferencePitchButton();
    }

    private void HandlePlayerDefeat()
    {
        enemySpawner.StopSpawn();
        HandlePlayerMetrics();
        sPanel.SetLoseScreen(ctx);
        PlayerObject.SetActive(false);
        enemyObject.SetActive(false);
        SaveRLExperience();
    }

    private void HandlePlayerVictory()
    {
        enemySpawner.StopSpawn();
        HandlePlayerMetrics();
        sPanel.SetWinScreen(ctx);
        PlayerObject.SetActive(false);
        SaveRLExperience();

        if (enemyObject != null)
            enemyObject.SetActive(false);

        LevelCompletionManager.UnlockNextLevel();
    }

    private void HandlePlayerMetrics()
    {
        string verType = "";

        if (handler is MonteCarloQuizHandler)
        {
            verType = "MCC";
        }
        else if (handler is SARSAQuizHandler)
        {
            verType = "SARSA";
        }
        else
        {
            verType = "NOALGO";
        }

        ctx.UpdatePlayerMetrics();
        ctx.ExportPlayerMetricsCSV(verType);
    }

    public void SetEnemy(EnemyComponent enemy)
    {
        ctx.SetEnemy(enemy);
        this.enemy = enemy;
        enemyObject = enemy.gameObject;
    }

    public PlayerMetric GetPlayerMetric()
    {
        return ctx.PlyrMetric;
    }

    // Called from UI to submit answers
    public void OnPlayerSubmitAnswers(List<string> answers)
    {
        if (!playerInputEnabled) return;

        Debug.Log("handler received inputs");
        // timerBar.FreezeTimer();
        handler.ReceivePlayerAnswers(answers);
    }

    public void PlayCurrentPitch()
    {
        ctx.PlayCurrentQuestionPitches();
    }

    public void PlayReferencePitch()
    {
        var difficulty = ctx.QuestionsToAnswer[ctx.CurrQuestionIndex].questionDifficulty;

        if (difficulty == QuestionComponent.DifficultyClass.HARD)
        {
            clipPlayer.PlaySingleClip(referencePitch);
        }
    }

    //use for stage3quiz and final quiz
    public void PlayReferencePitchFinals()
    {
        //only allows medium and hard difficulty to have pitch ref fr
        if (ctx.QuestionsToAnswer[ctx.CurrQuestionIndex].questionDifficulty.Equals(QuestionComponent.DifficultyClass.EASY)) return;
        clipPlayer.PlaySingleClip(referencePitch);
    }

    private void UpdateReferencePitchButton()
    {
        if (referencePitchButton == null || referencePitchSprite == null || ctx == null || ctx.QuestionsToAnswer.Count == 0)
            return;

        var difficulty = ctx.QuestionsToAnswer[ctx.CurrQuestionIndex].questionDifficulty;

        bool isAvailable = false;

        if (quizType == QuizType.Final)
        {
            // Final Quiz: Medium and Hard
            isAvailable = (difficulty == QuestionComponent.DifficultyClass.MEDIUM ||
                        difficulty == QuestionComponent.DifficultyClass.HARD);
        }
        else if (quizType == QuizType.Mini)
        {
            // Mini Quiz: Hard only
            isAvailable = (difficulty == QuestionComponent.DifficultyClass.HARD);
        }

        referencePitchButton.interactable = isAvailable;
        referencePitchSprite.color = isAvailable
            ? Color.white
            : new Color(1f, 1f, 1f, 0.4f);
    }


    // Called by UI to request next question (or handler can call it)
    public void OnRequestNextQuestion()
    {
        handler.LoadNextQuestion();
        // timerBar.UnfreezeTimer();
        // timerBar.ResetTimer(ctx.questionStartTime);
        UpdateDifficultyVisual();
        UpdateReferencePitchButton();
    }

    private void UpdateDifficultyVisual()
    {
        var q = ctx.GetCurrentQuestion();
        if (q != null && difficultySpriteChanger != null)
            difficultySpriteChanger.ApplyDifficulty(q.questionDifficulty);
    }

    private void SaveRLExperience()
    {
        if (handler is MonteCarloQuizHandler mccHandler)
        {
            // Get agent via reflection or add a getter to MonteCarloQuizHandler
            var agentField = typeof(MonteCarloQuizHandler).GetField("agent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (agentField != null)
            {
                var agent = agentField.GetValue(mccHandler) as MonteCarloAgent;
                if (agent != null)
                {
                    RLPersistenceManager.Instance.SaveMonteCarloAgent(agent);
                    Debug.Log("[QuizController] Saved MCC experience for next stage");
                }
            }
        }
        else if (handler is SARSAQuizHandler sarsaHandler)
        {
            var agentField = typeof(SARSAQuizHandler).GetField("agent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (agentField != null)
            {
                var agent = agentField.GetValue(sarsaHandler) as SARSAController;
                if (agent != null)
                {
                    RLPersistenceManager.Instance.SaveSARSAAgent(agent);
                    Debug.Log("[QuizController] Saved SARSA experience for next stage");
                }
            }
        }
    }

}
