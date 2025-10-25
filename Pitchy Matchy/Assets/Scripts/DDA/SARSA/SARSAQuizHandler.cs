using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SARSAQuizHandler : IQuizHandler
{
    private readonly QuizContext ctx;
    private readonly SARSAController sarsaAgent;
    private readonly List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode;

    private int totalQuestions;
    private int questionsAskedInEpisode = 0;

    // --- NEW: store the state/action used when presenting the current question ---
    private string lastStateForCurrentQuestion = null;
    private QuestionComponent.DifficultyClass lastActionForCurrentQuestion;

    public bool IsSessionFinished { get; set; } = false;

    public SARSAQuizHandler(QuizContext context, SARSAController agent = null)
    {
        ctx = context ?? throw new ArgumentNullException(nameof(context));
        sarsaAgent = agent ?? new SARSAController();

        totalQuestions = ctx.NumberOfQuestions;
        episode = new List<(string, QuestionComponent.DifficultyClass, float)>();
    }

    public void StartQuiz()
    {
        IsSessionFinished = false;
        ctx.CurrQuestionIndex = 0;
        ctx.QuestionsToAnswer.Clear();
        ctx.PlayerAnswers = new List<string>();

        // Load the first question (if capacity allows)
        LoadNextQuestion();
    }

    public void Update()
    {
        // Handler-level per-frame logic (diagnostics)
        Debug.Log($"[SARSA] Questions loaded: {ctx.QuestionsToAnswer.Count}/{totalQuestions}");
    }

    public void ReceivePlayerAnswers(List<string> answers)
    {
        if (IsSessionFinished) return;

        // Record response time for the question (important before learning/metrics)
        ctx.RecordResponseTime();

        ctx.PlayerAnswers = new List<string>(answers ?? new List<string>());
        ProcessAnswerAndLearn();

        // Optionally show wait/result panel
        // InitiateWaitPanel();
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        // If we've already loaded enough questions, finish the session.
        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        // Compute the state *at the moment of presenting the question*.
        // This uses the latest recorded response-time (i.e. from the previous question).
        string stateWhenAsked = GetCurrentState();
        lastStateForCurrentQuestion = stateWhenAsked;

        // Choose action using that state and remember it so the SARSA update later uses the same (s,a).
        var action = sarsaAgent.ChooseAction(stateWhenAsked);
        lastActionForCurrentQuestion = action;

        var nextQ = ctx.Bank.GetQuestionFromBank(action);
        ctx.AddQuestion(nextQ);
        ctx.CurrQuestionIndex = ctx.QuestionsToAnswer.Count - 1;
        questionsAskedInEpisode++;

        ctx.PlayerAnswers.Clear();

        ctx.UpdateQuestionText(); // also calls StartQuestionTimer()
        ctx.PlayCurrentQuestionPitches();

        Debug.Log($"[SARSA] Presented question idx={ctx.CurrQuestionIndex} stateAtAsk={stateWhenAsked} actionChosen={action}");
    }

    private void ProcessAnswerAndLearn()
    {
        var q = ctx.GetCurrentQuestion();
        if (q == null) return;

        // store answers and check correctness
        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        q.CheckAnswers();

        // reward as before
        float reward = q.isAnsweredCorrectly ? 1f : -1f;

        string state = lastStateForCurrentQuestion ?? GetCurrentState(); // fallback just in case
        var currentAction = lastActionForCurrentQuestion; // if null/unset, it'll be default enum (shouldn't happen normally)
        Debug.Log($"[SARSA] Update using s={state}, a={currentAction}, reward={reward}");

        // next state/action (for SARSA update)
        string nextState = GetNextState();
        var nextAction = sarsaAgent.ChooseAction(nextState);
        Debug.Log($"[SARSA] s'={nextState}, a'={nextAction}");

        // perform SARSA update on agent
        sarsaAgent.UpdateQValue(state, currentAction, reward, nextState, nextAction);

        // record episode step for debug/analysis
        episode.Add((state, currentAction, reward));

        // apply gameplay effects
        if (q.isAnsweredCorrectly)
            ctx.Enemy?.TakeDamage(ctx.Player.GetAttackPower());
        else
            ctx.Player?.TakeDamage(ctx.Enemy.GetAttackPower());

        // update player metrics if present in context
        ctx.PlyrMetric?.SetQuestionsAnswered(ctx.QuestionsToAnswer);
        ctx.PlyrMetric?.CalculateTotalAccuracy();

        // If finished loading the maximum number of questions, end session after processing
        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        // otherwise auto-load next question
        LoadNextQuestion();
    }

    private void InitiateWaitPanel()
    {
        var q = ctx.GetCurrentQuestion();
        if (q == null) return;
        ctx.WP.ExtractCurrentQuestionResult(q);
        ctx.WP.ShowParentPanel();
    }

    //Categorize response time
    private string GetResponseTimeCategory(float responseTime)
    {
        // thresholds: <=5 fast, <=10 average, >10 slow
        if (responseTime <= 5f) return "FAST";
        if (responseTime <= 10f) return "AVERAGE";
        return "SLOW";
    }

    
    private string GetCurrentState()
    {
        // Treat START only when there are no questions answered/presented yet
        if (ctx.QuestionsToAnswer.Count == 0) return "START";

        int correctCount = ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
        int totalAnswered = Math.Max(1, ctx.CurrQuestionIndex + 1); // number of questions up to current index inclusive

        float accuracy = (float)correctCount / totalAnswered;

        string accLabel;
        if (accuracy < 0.4f) accLabel = "EASY";
        else if (accuracy < 0.7f) accLabel = "MEDIUM";
        else accLabel = "HARD";

        // Use last recorded response time (from previous question) when presenting a new question.
        // If none, default to AVERAGE (safer).
        float latestResponseTime = ctx.ResponseTimes.Count > 0
            ? ctx.ResponseTimes[ctx.ResponseTimes.Count - 1]
            : float.NaN;

        string timeCat = float.IsNaN(latestResponseTime)
            ? "AVERAGE"
            : GetResponseTimeCategory(latestResponseTime);

        return $"{accLabel}_{timeCat}";
    }

    private string GetNextState()
    {
        // terminal check
        if (ctx.CurrQuestionIndex >= totalQuestions - 1) return "TERMINAL";

        // Use conservative projection: assume the next answer may be incorrect.
        // This makes the agent respond to failures quicker (less optimistic).
        int correctAnswers = ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
        // Conservative projection: project that next question will be incorrect
        float projectedAccuracy = (float)(correctAnswers) / (ctx.CurrQuestionIndex + 2); // no +1 optimistic

        string accLabel;
        if (projectedAccuracy < 0.4f) accLabel = "EASY";
        else if (projectedAccuracy < 0.7f) accLabel = "MEDIUM";
        else accLabel = "HARD";

        float latestResponseTime = ctx.ResponseTimes.Count > 0
            ? ctx.ResponseTimes[ctx.ResponseTimes.Count - 1]
            : float.NaN;

        string timeCat = float.IsNaN(latestResponseTime)
            ? "AVERAGE"
            : GetResponseTimeCategory(latestResponseTime);

        return $"{accLabel}_{timeCat}";
    }
}
