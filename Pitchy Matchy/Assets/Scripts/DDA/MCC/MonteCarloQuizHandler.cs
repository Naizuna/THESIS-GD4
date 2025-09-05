using System.Collections.Generic;
using UnityEngine;

public class MonteCarloQuizHandler : IQuizHandler
{
    private readonly QuizContext ctx;
    private MonteCarloAgent mcAgent;
    private List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode;
    private int totalQuestions;
    private int questionsPerEpisode;
    private int questionsAskedInEpisode = 0;

    public bool IsSessionFinished { get; set; } = false;

    public MonteCarloQuizHandler(QuizContext context, MonteCarloAgent agent = null)
    {
        ctx = context;
        mcAgent = agent ?? new MonteCarloAgent();
        totalQuestions = ctx.NumberOfQuestions;
        questionsPerEpisode = ctx.MccQuestionsPerEpisode;
        episode = new List<(string, QuestionComponent.DifficultyClass, float)>();
    }

    public void StartQuiz()
    {
        ctx.CurrQuestionIndex = 0;
        ctx.QuestionsToAnswer.Clear();
        // pre-load a question 
        LoadNextQuestion();
    }

    public void Update()
    {
        Debug.Log("Current: " + ctx.QuestionsToAnswer.Count);
    }

    public void ReceivePlayerAnswers(List<string> answers)
    {
        if (IsSessionFinished) return;
        ctx.PlayerAnswers = new List<string>(answers);
        ProcessAnswers_MCC();
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;
                
        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        string state = GetCurrentState();
        var action = mcAgent.ChooseAction(state); // returns DifficultyClass
        var nextQ = ctx.Bank.GetQuestionFromBank(action);
        ctx.AddQuestion(nextQ);
        ctx.CurrQuestionIndex = ctx.QuestionsToAnswer.Count - 1;
        questionsAskedInEpisode++;

        ctx.PlayerAnswers.Clear();

        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();
    }

    private void ProcessAnswers_MCC()
    {
        var q = ctx.GetCurrentQuestion();
        if (q == null) return;
        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        q.CheckAnswers();

        float reward = q.isAnsweredCorrectly ? 1f : -1f;
        string state = GetCurrentState();
        episode.Add((state, q.questionDifficulty, reward));

        float accuracy = (float)ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / (ctx.CurrQuestionIndex + 1);
        Debug.Log($"Current Accuracy: {accuracy * 100f}% after {ctx.CurrQuestionIndex + 1} questions.");

        if (q.isAnsweredCorrectly)
            ctx.Enemy.TakeDamage(ctx.Player.GetAttackPower());
        else
            ctx.Player.TakeDamage(ctx.Enemy.GetAttackPower());

        if (questionsAskedInEpisode >= questionsPerEpisode)
        {
            //end episode
            mcAgent.UpdatePolicy(episode);
            episode.Clear();
            Debug.Log("Episode finished. Policy updated.");

            int correct = ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
            int total = ctx.QuestionsToAnswer.Count;
            float finalAccuracy = (float)correct / total;
            Debug.Log($"Episode finished. Final Accuracy: {finalAccuracy * 100f}% ({correct}/{total} correct).");

            questionsAskedInEpisode = 0;
        }

        LoadNextQuestion();
    }

    private string GetCurrentState()
    {
        if (ctx.CurrQuestionIndex == 0) return "START";
        float accuracy = (float)ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / ctx.QuestionsToAnswer.Count;
        if (accuracy < 0.4f) return "LOW";
        if (accuracy < 0.7f) return "MEDIUM";
        return "HIGH";
    }
}
