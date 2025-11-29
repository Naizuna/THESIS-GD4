using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonteCarloQuizHandler : MonoBehaviour, IQuizHandler
{
    private readonly QuizContext ctx;
    private MonteCarloAgent mcAgent;
    private List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode;

    private int totalQuestions;
    private int questionsPerEpisode;
    private int questionsAskedInEpisode = 0;

    public bool IsSessionFinished { get; set; } = false;

    private Coroutine runningCoroutine;

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

        for (int i = 0; i < questionsPerEpisode; i++)
        {
            AddQuestionWithEpisodeTracking();
        }

        questionsAskedInEpisode = 0;
        ctx.PlayerAnswers.Clear();
        ctx.UpdateQuestionText(); // Timer starts automatically here
        ctx.PlayCurrentQuestionPitches();
    }

    public void Update()
    {
    }
    
     private void AddQuestionWithEpisodeTracking()
    {
        string state = GetCurrentState();
        var action = mcAgent.ChooseAction(state);
        var question = ctx.Bank.GetQuestionsFromBank(action);

        // Use actual difficulty chosen after fallback
        var actualDifficulty = question.questionDifficulty;
        ctx.AddQuestion(question);

        // Store this step in the episode (reward recorded after answer)
        episode.Add((state, actualDifficulty, 0f)); // reward assigned later when answered
    }

    public void ReceivePlayerAnswers(List<string> answers)
    {
        if (IsSessionFinished) return;

        ctx.RecordResponseTime(); // Record time before processing answers
        ctx.PlayerAnswers = new List<string>(answers);
        ProcessAnswers_MCC();
    }
    private void ProcessAnswers_MCC()
    {
        if (runningCoroutine != null)
        {
            ctx.coroutineRunner.StopCoroutine(runningCoroutine);
        }
        runningCoroutine = ctx.coroutineRunner.StartCoroutine(ProcessAnswers_MCC_Coroutine());
    }

    private IEnumerator ProcessAnswers_MCC_Coroutine()
    {
        Debug.Log("Coroutine started");
        ctx.enablePlayerInput(false);

        var q = ctx.GetCurrentQuestion();
        if (q == null) yield break;

        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        q.CheckAnswers();

        /* Old
        // Base reward depending on correctness
        float baseReward = q.isAnsweredCorrectly ? 1f : -1f;
        // Difficulty multiplier
        float difficultyMultiplier = 1f;
        switch (q.questionDifficulty)
        {
            case QuestionComponent.DifficultyClass.EASY:
                difficultyMultiplier = 1f;
                break;
            case QuestionComponent.DifficultyClass.MEDIUM:
                difficultyMultiplier = 2f;
                break;
            case QuestionComponent.DifficultyClass.HARD:
                difficultyMultiplier = 3f;
                break;
        }
        */

        // New reward logic
        int difficultyPoints = 1;
        switch (q.questionDifficulty)
        {
            case QuestionComponent.DifficultyClass.EASY:
                difficultyPoints = 1;
                break;
            case QuestionComponent.DifficultyClass.MEDIUM:
                difficultyPoints = 2;
                break;
            case QuestionComponent.DifficultyClass.HARD:
                difficultyPoints = 3;
                break;
        }

        float baseReward = q.isAnsweredCorrectly ? difficultyPoints : -difficultyPoints;
        

        // Response time bonus
        float timeBonus = 0f;
        float responseTime = ctx.ResponseTimes.Count > 0 ? ctx.ResponseTimes[ctx.ResponseTimes.Count - 1] : 0f;
        if (responseTime <= 5f) timeBonus = 0.5f;
        else if (responseTime <= 10f) timeBonus = 0.2f;

        //Old float reward = baseReward * difficultyMultiplier + timeBonus;

        //accuracy
        float accuracy = (float)ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / (ctx.CurrQuestionIndex + 1);
        //reward
        float reward = (baseReward * accuracy) - timeBonus;


        // Apply reward to the **most recent** episode entry
        if (episode.Count > 0)
        {
            int last = episode.Count - 1;
            episode[last] = (episode[last].state, episode[last].action, reward);
        }
        else
        {
            Debug.LogWarning("Episode entry missing when applying reward. This means no question was added before answers were processed.");
        }

        questionsAskedInEpisode++;
        Debug.Log($"Answered {questionsAskedInEpisode}/{questionsPerEpisode} this episode.");

        Debug.Log($"Current Accuracy: {accuracy * 100f}% after {ctx.CurrQuestionIndex + 1} questions.");

        ctx.ShowCorrectAnswers();
        ctx.keysHighlighter.GetTheKeys(new QuestionComponent(q));
        ctx.PlayCurrentQuestionPitches();
        ctx.keysHighlighter.HighlightAnsweredKeys();

        Debug.Log("About to wait");
        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);

        if (q.isAnsweredCorrectly)
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

        if (ctx.AllQuestionsAnswered != null && !ctx.AllQuestionsAnswered.Contains(q))
        {
            ctx.AllQuestionsAnswered.Add(q);
        }

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        LoadNextQuestion();
        ctx.enablePlayerInput(true);
        
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        if (questionsAskedInEpisode >= questionsPerEpisode)
        {
            //end episode
            mcAgent.UpdatePolicy(episode);
            //mcAgent.DecayEpsilon();
            Debug.Log("Episode finished. Policy updated.");
            Debug.Log("Debug Epsilon: " + mcAgent.CurrentEpsilon);

            int correct = ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count;
            int total = ctx.QuestionsToAnswer.Count;
            float finalAccuracy = (float)correct / total;
            Debug.Log($"Episode finished. Final Accuracy: {finalAccuracy * 100f}% ({correct}/{total} correct).");

            episode.Clear();
            questionsAskedInEpisode = 0;
        }

        if (ctx.CurrQuestionIndex + 1 >= totalQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        if (ctx.CurrQuestionIndex + 1 < ctx.QuestionsToAnswer.Count)
        {
            ctx.CurrQuestionIndex++;
        }
        else
        {
            // Generate another mixed batch for the next episode
            for (int i = 0; i < questionsPerEpisode; i++)
                AddQuestionWithEpisodeTracking();

            ctx.CurrQuestionIndex++;
        }

        ctx.PlayerAnswers.Clear();
        ctx.UpdateQuestionText(); // Resets timer for new question
        ctx.PlayCurrentQuestionPitches();
    }

    private string GetResponseTimeCategory(float responseTime)
    {
        if (responseTime <= 5f) return "FAST";
        if (responseTime <= 10f) return "AVERAGE";
        return "SLOW";
    }

    private string GetCurrentState()
    {
        if (ctx.CurrQuestionIndex == 0) return "START";

        float accuracy = (float)ctx.QuestionsToAnswer.FindAll(q => q.isAnsweredCorrectly).Count / ctx.QuestionsToAnswer.Count;

        float latestResponseTime = ctx.ResponseTimes.Count > 0
        ? ctx.ResponseTimes[ctx.ResponseTimes.Count - 1]
        : 0f;

        string timeCategory = GetResponseTimeCategory(latestResponseTime);

        string accLabel;
        if (accuracy < 0.4f) accLabel = "LOW";
        else if (accuracy < 0.7f) accLabel =  "MEDIUM";
        else accLabel = "HIGH";

        return $"{accLabel}_{timeCategory}";
    }
}