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

    // FINE-GRAINED state tracking (window of 2-3 recent questions)
    private Queue<bool> recentResults = new Queue<bool>();
    private const int STATE_WINDOW = 2; // Small window for fast adaptation

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
        
        // Update recent results with small window
        recentResults.Enqueue(correct);
        if (recentResults.Count > STATE_WINDOW) 
            recentResults.Dequeue();

        // === SIMPLIFIED REWARD STRUCTURE ===
        // Clear, immediate feedback based on difficulty and correctness
        float reward = 0f;
        
        if (correct)
        {
            // Positive reward scaled by difficulty
            reward = q.questionDifficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => 1.0f,
                QuestionComponent.DifficultyClass.MEDIUM => 2.0f,
                QuestionComponent.DifficultyClass.HARD => 3.0f,
                _ => 1.0f
            };
            
            // Small time bonus for quick correct answers
            float responseTime = ctx.ResponseTimes.LastOrDefault();
            if (responseTime > 0 && responseTime <= 5f)
                reward += 0.5f;
                
            totalCorrectAnswers++;
        }
        else
        {
            // Negative reward - stronger penalty for harder questions failed
            reward = q.questionDifficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => -1.5f,   // Bad to fail easy
                QuestionComponent.DifficultyClass.MEDIUM => -1.0f,
                QuestionComponent.DifficultyClass.HARD => -0.5f,   // Expected to fail hard sometimes
                _ => -1.0f
            };
        }

        // === STATE TRANSITIONS ===
        string currentState = lastState ?? "START";
        var currentAction = lastAction;
        string nextState = GetNextState();
        var nextAction = agent.ChooseAction(nextState);

        // SARSA update
        agent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
        agent.DecayEpsilon(); // Decay after every question

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
            agent.PrintQTable(); // Debug output at end
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

        Debug.Log($"[SARSA] Q#{ctx.CurrQuestionIndex + 1}/{totalQuestions} | State={state} | Action={action} | ε={agent.CurrentEpsilon:F3}");
    }

    // === FINE-GRAINED STATE REPRESENTATION ===
    // 6 states instead of 3 for better discrimination
    private string GetNextState()
    {
        if (ctx.QuestionsToAnswer.Count == 0) 
            return "START";
        
        if (ctx.CurrQuestionIndex >= totalQuestions - 1) 
            return "TERMINAL";

        // No results yet
        if (recentResults.Count == 0) 
            return "START";

        int correctCount = recentResults.Count(r => r);
        int totalCount = recentResults.Count;

        // Single question only - binary state
        if (totalCount == 1)
        {
            return correctCount == 1 ? "IMPROVING" : "STRUGGLING";
        }

        // 2+ questions - more nuanced states
        float accuracy = (float)correctCount / totalCount;
        
        if (accuracy == 0f)
            return "STRUGGLING";      // All wrong
        else if (accuracy < 0.5f)
            return "INCONSISTENT";    // Mostly wrong
        else if (accuracy == 0.5f)
            return "INCONSISTENT";    // Mixed
        else if (accuracy < 1.0f)
            return "IMPROVING";       // Mostly right
        else
            return "MASTERING";       // All right

        // Alternatively, could use last 2 questions as explicit state:
        // "WW", "WC", "CW", "CC" for even finer control
    }
}