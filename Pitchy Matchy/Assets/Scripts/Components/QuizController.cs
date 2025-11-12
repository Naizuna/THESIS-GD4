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
    [SerializeField] private int numberOfQuestions;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private KeysHighlighter keysHighlighter;


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
        // Before change:
        // if (quizMode == QuizMode.Normal)
        // {
        //     ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions);
        //     handler = new NormalQuizHandler(ctx);
        // }
        // else if (quizMode == QuizMode.MonteCarloControl)
        // {
        //     ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, mccQuestionsPerEpisode);
        //     handler = new MonteCarloQuizHandler(ctx, new MonteCarloAgent()); // or inject agent instance
        // }
        // else if (quizMode == QuizMode.Sarsa)
        // {
        //     ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions);
        //     handler = new SARSAQuizHandler(ctx, new SARSAController());
        // }

        // After change:
        // Create context (always the same unless MCC needs extra)
        if (GameVersionManager.Instance.SelectedVersion == GameVersionManager.VersionType.MCC)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, mccQuestionsPerEpisode, keysHighlighter);
            handler = new MonteCarloQuizHandler(ctx, new MonteCarloAgent());
        }
        else if (GameVersionManager.Instance.SelectedVersion == GameVersionManager.VersionType.SARSA)
        {
            ctx = new QuizContext(bank, wp, sPanel, player, enemy, clipPlayer, questText, numberOfQuestions, keysHighlighter);
            handler = new SARSAQuizHandler(ctx, new SARSAController());
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
        Debug.Log("is input enabled: " + playerInputEnabled);

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
    }

    private void HandlePlayerVictory()
    {
        enemySpawner.StopSpawn();
        HandlePlayerMetrics();
        sPanel.SetWinScreen(ctx);
        PlayerObject.SetActive(false);
        
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
        UpdateDifficultyVisual();
        UpdateReferencePitchButton();
    }

    private void UpdateDifficultyVisual()
    {
        var q = ctx.GetCurrentQuestion();
        if (q != null && difficultySpriteChanger != null)
            difficultySpriteChanger.ApplyDifficulty(q.questionDifficulty);
    }



}
