using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SARSAQuizHandler : MonoBehaviour, IQuizHandler
{
    private readonly QuizContext ctx;
    private readonly SARSAController agent;
    private int totalQuestions;

    private string lastState = null;
    private QuestionComponent.DifficultyClass lastAction;

    private int consecutiveFails = 0;
    private Queue<bool> recentResults = new Queue<bool>();
    private int accuracyWindow = 5;

    public bool IsSessionFinished { get; set; } = false;
    private Coroutine runningCoroutine;

    public SARSAQuizHandler(QuizContext context, SARSAController sarsaAgent = null)
    {
        ctx = context ?? throw new ArgumentNullException(nameof(context));
        agent = sarsaAgent ?? new SARSAController();
        totalQuestions = ctx.NumberOfQuestions;
    }

    public void StartQuiz()
    {
        ctx.CurrQuestionIndex = 0;
        ctx.QuestionsToAnswer.Clear();
        ctx.PlayerAnswers.Clear();
        recentResults.Clear();
        consecutiveFails = 0;
        IsSessionFinished = false;

        LoadNextQuestion();
    }
     public void Update()
    {
        // Optional debug info for runtime monitoring
        Debug.Log($"[SARSA] Progress {ctx.QuestionsToAnswer.Count}/{totalQuestions} | ε={agent.CurrentEpsilon:F3} | Fails={consecutiveFails}");
    }
    public void ReceivePlayerAnswers(List<string> answers)
    {
        if (IsSessionFinished) return;

        ctx.RecordResponseTime();
        ctx.PlayerAnswers = new List<string>(answers ?? new List<string>());
        ProcessAnswerAndLearn();
    }

    private void ProcessAnswerAndLearn()
    {
        if (runningCoroutine != null)
        {
            ctx.coroutineRunner.StopCoroutine(runningCoroutine);
        }
        runningCoroutine = ctx.coroutineRunner.StartCoroutine(ProcessAnswerAndLearnCoroutine());
    }

    private IEnumerator ProcessAnswerAndLearnCoroutine()
    {
        Debug.Log("Coroutine started");
        ctx.enablePlayerInput(false);

        var q = ctx.GetCurrentQuestion();
        if (q == null) yield break;

        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        q.CheckAnswers();

        bool correct = q.isAnsweredCorrectly;
        recentResults.Enqueue(correct);
        if (recentResults.Count > accuracyWindow) recentResults.Dequeue();
        consecutiveFails = correct ? 0 : consecutiveFails + 1;

        // --- Reward ---
        int difficultyPoints = q.questionDifficulty switch
        {
            QuestionComponent.DifficultyClass.EASY => 1,
            QuestionComponent.DifficultyClass.MEDIUM => 2,
            QuestionComponent.DifficultyClass.HARD => 3,
            _ => 1
        };

        float baseReward = correct ? difficultyPoints : -difficultyPoints;
        float responseTime = ctx.ResponseTimes.LastOrDefault();
        float timeBonus = responseTime <= 5f ? 0.5f : (responseTime <= 10f ? 0.2f : 0f);
        float reward = baseReward + timeBonus;

        // --- State transitions ---
        string currentState = lastState ?? GetCurrentState();
        var currentAction = lastAction;
        string nextState = GetNextState();
        var nextAction = agent.ChooseAction(nextState);

        agent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);

        // --- Epsilon behavior ---
        if (consecutiveFails >= 3)
        {
            float newEps = Mathf.Max(agent.CurrentEpsilon * 0.5f, 0.02f);
            agent.SetEpsilon(newEps);
            Debug.Log($"[SARSA] High failure streak — reducing ε to {agent.CurrentEpsilon:F3}");
        }
        else
        {
            agent.DecayEpsilon();
        }

        // --- Gameplay effects ---
        ctx.ShowCorrectAnswers();
        ctx.keysHighlighter.GetTheKeys(new QuestionComponent(q));
        ctx.PlayCurrentQuestionPitches();
        ctx.keysHighlighter.HighlightAnsweredKeys();

        Debug.Log("About to wait");
        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);

        if (correct)
        {
            ctx.Player.PlayAttack();
            ctx.Enemy.TakeDamage(ctx.Player.GetAttackPower());
            ctx.correctStreak++;
        }
        else
        {
            ctx.Enemy.PlayAttack();
            ctx.Player.TakeDamage(ctx.Enemy.GetAttackPower());
            ctx.correctStreak = 0;
            ctx.TogglePlayerImmunity(false);
        }

        ctx.CheckCorrectStreak();

        ctx.PlyrMetric?.SetQuestionsAnswered(ctx.QuestionsToAnswer);
        ctx.PlyrMetric?.CalculateTotalAccuracy();

        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            yield break;
        }

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        LoadNextQuestion();
        ctx.enablePlayerInput(true);
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        string state = GetCurrentState();
        lastState = state;
        var action = agent.ChooseAction(state);
        lastAction = action;

        var q = ctx.Bank.GetQuestionFromBank(action);
        ctx.AddQuestion(q);
        ctx.CurrQuestionIndex = ctx.QuestionsToAnswer.Count - 1;

        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();

        Debug.Log($"[SARSA] Q#{ctx.CurrQuestionIndex} | state={state} | action={action}");
    }

    private string GetCurrentState()
    {
        if (ctx.QuestionsToAnswer.Count == 0 && recentResults.Count == 0) return "START";

        int correctCount = recentResults.Count(r => r);
        float acc = (float)correctCount / Math.Max(1, recentResults.Count);

        if (acc < 0.4f) return "EASY";
        if (acc < 0.7f) return "MEDIUM";
        return "HARD";
    }

    private string GetNextState()
    {
        if (ctx.CurrQuestionIndex >= totalQuestions - 1) return "TERMINAL";
        return GetCurrentState();
    }
}
