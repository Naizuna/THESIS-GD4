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

    // FINE-GRAINED state tracking
    private Queue<bool> recentResults = new Queue<bool>();
    private const int STATE_WINDOW = 3; // Slightly larger window for stability

    public bool IsSessionFinished { get; set; } = false;
    private Coroutine runningCoroutine;
    private int totalCorrectAnswers = 0;

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
        totalCorrectAnswers = 0;
        IsSessionFinished = false;

        LoadNextQuestion();
    }

    public void Update()
    {
        // Optional debug info
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
        ctx.enablePlayerInput(false);

        var q = ctx.GetCurrentQuestion();
        if (q == null) yield break;

        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        q.CheckAnswers();

        bool correct = q.isAnsweredCorrectly;
        
        // Update recent results
        recentResults.Enqueue(correct);
        if (recentResults.Count > STATE_WINDOW) 
            recentResults.Dequeue();

        // === ORIGINAL REWARD STRUCTURE (from your thesis) ===
        int difficultyPoints = q.questionDifficulty switch
        {
            QuestionComponent.DifficultyClass.EASY => 1,
            QuestionComponent.DifficultyClass.MEDIUM => 2,
            QuestionComponent.DifficultyClass.HARD => 3,
            _ => 1
        };

        float baseReward = correct ? difficultyPoints : -difficultyPoints;
        
        // Time bonus (optional - keep if this is part of your thesis)
        float responseTime = ctx.ResponseTimes.LastOrDefault();
        float timeBonus = responseTime <= 5f ? 0.5f : (responseTime <= 10f ? 0.2f : 0f);
        
        // Track accuracy
        if (correct) totalCorrectAnswers++;
        float accuracy = (float)totalCorrectAnswers / (ctx.CurrQuestionIndex + 1);

        // ORIGINAL FORMULA: (baseReward * accuracy) - timeBonus
        // Note: Using subtraction for timeBonus seems unusual - typically bonuses are added
        // Keeping your original formula as requested
        float reward = (baseReward * accuracy) - timeBonus;

        // === STATE TRANSITIONS ===
        string currentState = lastState ?? "START";
        var currentAction = lastAction;
        string nextState = GetNextState();
        var nextAction = agent.ChooseAction(nextState);

        // SARSA update
        agent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
        agent.DecayEpsilon();

        // === GAMEPLAY EFFECTS ===
        ctx.ShowCorrectAnswers();
        ctx.keysHighlighter.GetTheKeys(new QuestionComponent(q));
        ctx.PlayCurrentQuestionPitches();
        ctx.keysHighlighter.HighlightAnsweredKeys();

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
            agent.PrintQTable();
            yield break;
        }

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        LoadNextQuestion();
        ctx.enablePlayerInput(true);
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        string state = GetNextState();
        lastState = state;
        var action = agent.ChooseAction(state);
        lastAction = action;

        var q = ctx.Bank.GetQuestionFromBank(action);
        ctx.AddQuestion(q);
        ctx.CurrQuestionIndex = ctx.QuestionsToAnswer.Count - 1;

        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();

        Debug.Log($"[SARSA] Q#{ctx.CurrQuestionIndex + 1}/{totalQuestions} | State={state} | Action={action} | ε={agent.CurrentEpsilon:F3} | Acc={((float)totalCorrectAnswers/(ctx.CurrQuestionIndex+1)):F2}");
    }

    // === FINE-GRAINED STATE REPRESENTATION ===
    private string GetNextState()
    {
        if (ctx.QuestionsToAnswer.Count == 0) 
            return "START";
        
        if (ctx.CurrQuestionIndex >= totalQuestions - 1) 
            return "TERMINAL";

        if (recentResults.Count == 0) 
            return "START";

        int correctCount = recentResults.Count(r => r);
        int totalCount = recentResults.Count;

        // Single question - binary state
        if (totalCount == 1)
        {
            return correctCount == 1 ? "IMPROVING" : "STRUGGLING";
        }

        // Multiple questions - calculate accuracy over window
        float accuracy = (float)correctCount / totalCount;
        
        // More granular state boundaries
        if (accuracy == 0f)
            return "STRUGGLING";      // 0/3 or 0/2 - all wrong
        else if (accuracy <= 0.33f)
            return "STRUGGLING";      // 1/3 - mostly wrong
        else if (accuracy < 0.5f)
            return "INCONSISTENT";    // 1/2 - even split on 2 questions
        else if (accuracy == 0.5f)
            return "INCONSISTENT";    // Exactly half
        else if (accuracy < 0.67f)
            return "IMPROVING";       // 2/3 - more right than wrong
        else if (accuracy < 1.0f)
            return "IMPROVING";       // 2/2 on window of 2
        else
            return "MASTERING";       // 3/3 or all correct
    }
}