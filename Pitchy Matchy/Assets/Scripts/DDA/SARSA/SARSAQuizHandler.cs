using System;
using System.Collections.Generic;
using UnityEngine;

public class SARSAQuizHandler : IQuizHandler
{
    private readonly QuizContext ctx;
    private readonly SARSAController sarsaAgent;
    private readonly List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode;

    private int totalQuestions;
    private int questionsAskedInEpisode = 0;

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

        ctx.PlayerAnswers = new List<string>(answers ?? new List<string>());
        ProcessAnswerAndLearn();

        // Show the result/wait panel for the current question
        //InitiateWaitPanel();
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        // If we've already loaded enough questions, finish the session.
        // Check BEFORE adding a new question so the final question remains answerable.
        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        string state = GetCurrentState();
        var action = sarsaAgent.ChooseAction(state);

        var nextQ = ctx.Bank.GetQuestionFromBank(action);
        ctx.AddQuestion(nextQ);
        ctx.CurrQuestionIndex = ctx.QuestionsToAnswer.Count - 1;
        questionsAskedInEpisode++;

        ctx.PlayerAnswers.Clear();

        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();
    }

    private void ProcessAnswerAndLearn()
    {
        var q = ctx.GetCurrentQuestion();
        if (q == null) return;

        // store answers and check correctness
        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        q.CheckAnswers();

        float reward = q.isAnsweredCorrectly ? 1f : -1f;

        // current state/action
        string state = GetCurrentState();
        var currentAction = q.questionDifficulty;
        Debug.Log($"CurrentState:{state} CurrentAction{currentAction}");
        // next state/action (for SARSA update)
        string nextState = GetNextState();
        var nextAction = sarsaAgent.ChooseAction(nextState);
        Debug.Log($"NextState:{nextState} NextAction:{nextAction}");
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

        // If we've finished loading the maximum number of questions, end session after processing
        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        // otherwise auto-load next question (matches previous behavior)
        LoadNextQuestion();
    }

    private void InitiateWaitPanel()
    {
        var q = ctx.GetCurrentQuestion();
        if (q == null) return;
        ctx.WP.ExtractCurrentQuestionResult(q);
        ctx.WP.ShowParentPanel();
    }

    private string GetCurrentState()
    {
        if (ctx.CurrQuestionIndex == 0) return "START";

        int answeredCount = ctx.QuestionsToAnswer.Count;
        if (answeredCount == 0) return "START";

        float accuracy = (float)ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / Math.Max(1, ctx.CurrQuestionIndex);
        if (accuracy < 0.4f) return "LOW";
        if (accuracy < 0.7f) return "MEDIUM";
        return "HIGH";
    }

    private string GetNextState()
    {
        if (ctx.CurrQuestionIndex >= totalQuestions - 1) return "TERMINAL";

        int correctAnswers = ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
        float projectedAccuracy = (float)(correctAnswers + 1) / (ctx.CurrQuestionIndex + 2); // optimistic projection

        if (projectedAccuracy < 0.4f) return "LOW";
        if (projectedAccuracy < 0.7f) return "MEDIUM";
        return "HIGH";
    }
}
