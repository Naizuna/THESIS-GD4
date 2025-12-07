using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    
    // Track the state when action was chosen for each question in episode
    private string stateWhenActionChosen = "START";

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
        stateWhenActionChosen = "START";
        questionsAskedInEpisode = 0;
        episode.Clear();
        totalCorrectAnswers = 0;

        Debug.Log($"[MCC] ═══════════════════════════════════════");
        Debug.Log($"[MCC] STARTING NEW QUIZ SESSION");
        Debug.Log($"[MCC] Total Questions: {totalQuestions}");
        Debug.Log($"[MCC] Questions Per Episode: {questionsPerEpisode}");
        Debug.Log($"[MCC] ═══════════════════════════════════════");

        // Pre-generate ENTIRE first episode
        GenerateEpisode();

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
        var timeCat = MonteCarloAgent.DiscretizeResponseTime(responseTime);

        float reward = CalculateReward(q.questionDifficulty, correct, responseTime);

        // Record episode step: (state when action was chosen, action taken, reward received)
        episode.Add((stateWhenActionChosen, currentAction, reward));
        questionsAskedInEpisode++;

        if (correct)
        {
            totalCorrectAnswers++;
        }

        // Calculate next state based on current question result
        string nextState = MonteCarloAgent.ConstructState(
            q.questionDifficulty,
            correct,
            responseTime
        );

        Debug.Log($"[MCC] Q#{ctx.CurrQuestionIndex + 1}/{totalQuestions} | " +
                  $"Episode Q: {questionsAskedInEpisode}/{questionsPerEpisode} | " +
                  $"State: {stateWhenActionChosen} → {nextState} | " +
                  $"Action: {currentAction} | Correct: {correct} | Reward: {reward:F2}");

        // Update current state (will be used for NEXT episode generation)
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
            ctx.ShowResponseTime(timeCat.ToString());
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

        // Check if episode is complete
        if (questionsAskedInEpisode >= questionsPerEpisode)
        {
            int episodeNumber = ctx.CurrQuestionIndex / questionsPerEpisode + 1;
            
            Debug.Log($"[MCC] ═══════════════════════════════════════");
            Debug.Log($"[MCC] EPISODE {episodeNumber} COMPLETE");
            
            // Print episode trajectory BEFORE learning
            Debug.Log($"[MCC] Episode Trajectory:");
            for (int i = 0; i < episode.Count; i++)
            {
                var step = episode[i];
                Debug.Log($"[MCC]   Step {i+1}: {step.state} → {step.action} (R: {step.reward:F2})");
            }

            // the agent learns from the complete episode
            agent.UpdatePolicy(episode);
            agent.DecayEpsilon();

            int episodeCorrect = episode.FindAll(e => e.reward > 0).Count;
            float episodeAccuracy = (float)episodeCorrect / questionsPerEpisode;

            Debug.Log($"[MCC] Episode Accuracy: {episodeAccuracy * 100:F1}% ({episodeCorrect}/{questionsPerEpisode})");
            Debug.Log($"[MCC] Epsilon after decay: {agent.CurrentEpsilon:F3}");
            Debug.Log($"[MCC] Final state of episode: {currentState}");

            // Print Q-table state (top entries only)
            Debug.Log($"[MCC] Top Q-Values after learning:");
            var qEntries = agent.GetAllQTableEntries();
            if (qEntries.Count > 0)
            {
                var sortedEntries = qEntries.OrderByDescending(e => e.QValue).Take(5);
                foreach (var entry in sortedEntries)
                {
                    Debug.Log($"[MCC]   Q({entry.State}, {entry.Action}) = {entry.QValue:F2} [visits: {entry.Visits}]");
                }
            }
            else
            {
                Debug.Log($"[MCC]   Q-table is still empty");
            }

            Debug.Log($"[MCC] ═══════════════════════════════════════");

            // Reset for next episode
            episode.Clear();
            questionsAskedInEpisode = 0;

            // If more questions remain, generate next ENTIRE episode
            if (ctx.CurrQuestionIndex + 1 < totalQuestions)
            {
                Debug.Log($"[MCC] Generating Episode {episodeNumber + 1} from state: {currentState}");
                GenerateEpisode();
            }
        }

        // Check if quiz session is complete
        if (ctx.QuestionsToAnswer.Count >= totalQuestions && 
            ctx.CurrQuestionIndex >= totalQuestions - 1)
        {
            IsSessionFinished = true;
            Debug.Log($"[MCC] ═══════════════════════════════════════");
            Debug.Log($"[MCC] QUIZ SESSION COMPLETE");
            Debug.Log($"[MCC] Total Questions: {totalQuestions}");
            Debug.Log($"[MCC] Total Correct: {totalCorrectAnswers}");
            Debug.Log($"[MCC] Overall Accuracy: {(float)totalCorrectAnswers / totalQuestions * 100:F1}%");
            Debug.Log($"[MCC] ═══════════════════════════════════════");
            agent.PrintQTable();
            agent.PrintStateSpaceInfo();
            yield break;
        }

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        ctx.ShowResponseTime("");
        // Move to next question
        if (ctx.CurrQuestionIndex + 1 < ctx.QuestionsToAnswer.Count)
        {
            ctx.CurrQuestionIndex++;
            ctx.UpdateQuestionText();
            ctx.PlayCurrentQuestionPitches();
        }

        ctx.enablePlayerInput(true);
    }

    /// Generates an entire episode of questions at once
    /// This is the correct Monte Carlo approach - no within-episode adaptation
    private void GenerateEpisode()
    {
        Debug.Log($"[MCC] --- Generating Episode ---");
        Debug.Log($"[MCC] Starting from state: {currentState}");
        Debug.Log($"[MCC] Current ε: {agent.CurrentEpsilon:F3}");

        int questionsToGenerate = Mathf.Min(questionsPerEpisode, totalQuestions - ctx.QuestionsToAnswer.Count);

        for (int i = 0; i < questionsToGenerate; i++)
        {
            LoadNextQuestion();
        }

        Debug.Log($"[MCC] Generated {questionsToGenerate} questions for this episode");
        Debug.Log($"[MCC] --- Episode Generation Complete ---");
    }

    
    /// Loads a single question based on current state
    /// Called ONLY during episode generation (pre-generation phase)
    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        QuestionComponent.DifficultyClass difficulty;

        // Store the state that will be used for this question
        // All questions in an episode use the SAME starting state
        stateWhenActionChosen = currentState;

        // Determine difficulty for this question
        if (currentState == "START")
        {
            // First question of first episode: randomized start
            Array values = System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
            difficulty = (QuestionComponent.DifficultyClass)values.GetValue(
                UnityEngine.Random.Range(0, values.Length));
            currentAction = difficulty;
            Debug.Log($"[MCC]   Q#{ctx.QuestionsToAnswer.Count + 1}: First question - random start → {difficulty}");
        }
        else
        {
            // Subsequent episodes: use epsilon-greedy with learned Q-values
            difficulty = agent.ChooseAction(currentState);
            currentAction = difficulty;
            Debug.Log($"[MCC]   Q#{ctx.QuestionsToAnswer.Count + 1}: State={currentState} → Action={difficulty}");
        }

        // Get question from bank
        var q = ctx.Bank.GetQuestionFromBank(difficulty);
        ctx.AddQuestion(q);
    }

    /// <summary>
    /// Reward function that encourages appropriate difficulty matching
    /// </summary>
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

            reward += timeBonus;
        }
        else
        {
            // Penalties: worse for easier questions (player struggling with easy = bad)
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => -1.0f,   // Small penalty
                QuestionComponent.DifficultyClass.MEDIUM => -2.0f, // Medium penalty
                QuestionComponent.DifficultyClass.HARD => -3.0f,   // Large penalty
                _ => -1.0f
            };
        }

        return reward;
    }
}