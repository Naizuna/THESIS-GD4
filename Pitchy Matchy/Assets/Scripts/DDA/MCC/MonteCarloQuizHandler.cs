using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonteCarloQuizHandler : MonoBehaviour, IQuizHandler
{
    private readonly QuizContext ctx;
    private readonly MonteCarloAgent agent;
    
    private int totalQuestions;
    private int questionsPerEpisode;
    private int questionsAskedInEpisode = 0;

    // Episode storage: (state, action, reward) tuples
    private List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode;

    // State tracking
    private string currentState = "START";
    private QuestionComponent.DifficultyClass currentAction;

    public bool IsSessionFinished { get; set; } = false;
    private Coroutine runningCoroutine;
    private int totalCorrectAnswers = 0;

    public MonteCarloQuizHandler(QuizContext context, MonteCarloAgent mcAgent = null)
    {
        ctx = context ?? throw new ArgumentNullException(nameof(context));
        agent = mcAgent ?? new MonteCarloAgent();
        totalQuestions = ctx.NumberOfQuestions;
        questionsPerEpisode = ctx.MccQuestionsPerEpisode;
        episode = new List<(string, QuestionComponent.DifficultyClass, float)>();
    }

    public void StartQuiz()
    {
        ctx.CurrQuestionIndex = 0;
        ctx.QuestionsToAnswer.Clear();
        ctx.PlayerAnswers.Clear();
        IsSessionFinished = false;

        currentState = "START";
        questionsAskedInEpisode = 0;
        episode.Clear();
        totalCorrectAnswers = 0;

        // Pre-generate first episode of questions
        for (int i = 0; i < questionsPerEpisode; i++)
        {
            LoadNextQuestion();
        }

        // Start with first question
        ctx.CurrQuestionIndex = 0;
        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();
    }

    public void Update()
    {
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
        float responseTime = ctx.ResponseTimes[ctx.ResponseTimes.Count - 1];

        
        float reward = CalculateReward(q.questionDifficulty, correct, responseTime);

        // episode record
        // Store (state, action, reward) tuple for this step
        episode.Add((currentState, currentAction, reward));
        questionsAskedInEpisode++;

        if (correct)
        {
            totalCorrectAnswers++;
        }

        // next state
        string nextState = MonteCarloAgent.ConstructState(
            q.questionDifficulty,
            correct,
            responseTime
        );

        Debug.Log($"[MCC] Q#{ctx.CurrQuestionIndex + 1}/{totalQuestions} | " +
                  $"Current Question in the Episode: {questionsAskedInEpisode}/{questionsPerEpisode} | " +
                  $"State: {currentState} → {nextState} | " +
                  $"Action: {currentAction} | Correct: {correct} | Reward: {reward:F2}");

        // Update state for next question
        currentState = nextState;

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

        // check if episode is done
        if (questionsAskedInEpisode >= questionsPerEpisode)
        {
            // Episode complete - update policy
            agent.UpdatePolicy(episode);
            agent.DecayEpsilon();

            int episodeCorrect = episode.FindAll(e => e.reward > 0).Count;
            float episodeAccuracy = (float)episodeCorrect / questionsPerEpisode;

            Debug.Log($"[MCC] EPISODE COMPLETE | Accuracy: {episodeAccuracy * 100:F1}% " +
                      $"({episodeCorrect}/{questionsPerEpisode}) | ε: {agent.CurrentEpsilon:F3}");

            // Reset for next episode
            episode.Clear();
            questionsAskedInEpisode = 0;

            // If more questions remain, generate next episode
            if (ctx.CurrQuestionIndex + 1 < totalQuestions)
            {
                for (int i = 0; i < questionsPerEpisode && ctx.QuestionsToAnswer.Count < totalQuestions; i++)
                {
                    LoadNextQuestion();
                }
            }
        }

        // check if quiz session is done
        if (ctx.QuestionsToAnswer.Count >= totalQuestions && 
            ctx.CurrQuestionIndex >= totalQuestions - 1)
        {
            IsSessionFinished = true;
            agent.PrintQTable();
            agent.PrintStateSpaceInfo();
            yield break;
        }

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);

        // Move to next question
        if (ctx.CurrQuestionIndex + 1 < ctx.QuestionsToAnswer.Count)
        {
            ctx.CurrQuestionIndex++;
            ctx.UpdateQuestionText();
            ctx.PlayCurrentQuestionPitches();
        }

        ctx.enablePlayerInput(true);
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

     
        QuestionComponent.DifficultyClass difficulty;

        // Determine difficulty for next question
        if (currentState == "START")
        {
            // First question randomized
            difficulty = (QuestionComponent.DifficultyClass)UnityEngine.Random.Range(
                0, System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length);
            currentAction = difficulty;
            Debug.Log($"[MCC] First question - starting with {difficulty}");
        }
        else
        {
            // Choose action based on current state
            difficulty = agent.ChooseAction(currentState);
            currentAction = difficulty;
        }

        // Get question from bank
        var q = ctx.Bank.GetQuestionFromBank(difficulty);
        ctx.AddQuestion(q);

        Debug.Log($"[MCC] Pre-generated Q#{ctx.QuestionsToAnswer.Count}/{totalQuestions} | " +
                  $"State: {currentState} | Chosen Difficulty: {difficulty} | ε: {agent.CurrentEpsilon:F3}");
    }

    // reward function
    private float CalculateReward(
        QuestionComponent.DifficultyClass difficulty,
        bool correct,
        float responseTime)
    {
        float reward = 0f;

        if (correct)
        {
            // Base reward scaled by difficulty
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => 1.0f,
                QuestionComponent.DifficultyClass.MEDIUM => 2.0f,
                QuestionComponent.DifficultyClass.HARD => 3.0f,
                _ => 1.0f
            };

            // Time bonus for fast correct answers
            var timeCat = MonteCarloAgent.DiscretizeResponseTime(responseTime);
            float timeBonus = timeCat switch
            {
                MonteCarloAgent.ResponseTimeCategory.FAST => 0.5f,
                MonteCarloAgent.ResponseTimeCategory.AVERAGE => 0.2f,
                MonteCarloAgent.ResponseTimeCategory.SLOW => 0f,
                _ => 0f
            };

            reward += timeBonus; //applied only for the correct answers fr
        }
        else
        {
            // Penalties based on difficulty 
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => -1.0f,//-2.0f,   
                QuestionComponent.DifficultyClass.MEDIUM => -2.0f,//-1.5f,
                QuestionComponent.DifficultyClass.HARD => -3.0f,//-0.5f,   
                _ => -1.0f
            };
        }

        return reward;
    }
}