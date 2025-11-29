using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SARSAQuizHandler : MonoBehaviour, IQuizHandler
{
    private readonly QuizContext ctx;
    private readonly SARSAController agent;
    private int totalQuestions;

    // State tracking
    private string currentState = "START"; // Special initial state
    private QuestionComponent.DifficultyClass currentAction;
    
    // For constructing next state after answer
    private QuestionComponent.DifficultyClass lastQuestionDifficulty;
    private float lastResponseTime;

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
        IsSessionFinished = false;
        
        // Reset to initial state
        currentState = "START";
        totalCorrectAnswers = 0;

        LoadNextQuestion();
    }

    public void Update()
    {
        // Optional: Could display current state info
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

        // === REWARD CALCULATION ===
        float reward = CalculateReward(q.questionDifficulty, correct, responseTime);

        // === STATE TRANSITION ===
        // nextState is based on what just happened
        string nextState;
        
        if (currentState == "START")
        {
            // First question answered - construct real state
            nextState = SARSAController.ConstructState(
                q.questionDifficulty, 
                correct, 
                responseTime
            );
        }
        else
        {
            // Normal state transition
            nextState = SARSAController.ConstructState(
                q.questionDifficulty, 
                correct, 
                responseTime
            );
        }

        // Choose next action based on next state
        var nextAction = agent.ChooseAction(nextState);

        // === SARSA UPDATE ===
        if (currentState != "START") // Don't update on first question
        {
            agent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
        }
        
        agent.DecayEpsilon();

        // Update state for next iteration
        currentState = nextState;
        currentAction = nextAction;

        if (correct)
        {
            totalCorrectAnswers++;
        }

        Debug.Log($"[QUIZ] Q#{ctx.CurrQuestionIndex + 1} | Difficulty: {q.questionDifficulty} | " +
                  $"Correct: {correct} | Time: {responseTime:F1}s | State: {nextState} | " +
                  $"Next Action: {nextAction} | Reward: {reward:F2}");

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

        // Check if quiz is finished
        if (ctx.QuestionsToAnswer.Count >= totalQuestions)
        {
            IsSessionFinished = true;
            agent.PrintQTable();
            agent.PrintStateSpaceInfo();
            yield break;
        }

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        LoadNextQuestion();
        ctx.enablePlayerInput(true);
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        // For first question, use heuristic (typically EASY)
        QuestionComponent.DifficultyClass difficulty;
        
        if (currentState == "START")
        {
            // First question - start with EASY
            difficulty = QuestionComponent.DifficultyClass.EASY;
            currentAction = difficulty;
            Debug.Log($"[SARSA] First question - starting with {difficulty}");
        }
        else
        {
            // Use the action chosen in previous step
            difficulty = currentAction;
        }

        var q = ctx.Bank.GetQuestionFromBank(difficulty);
        ctx.AddQuestion(q);
        ctx.CurrQuestionIndex = ctx.QuestionsToAnswer.Count - 1;

        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();

        Debug.Log($"[SARSA] Loading Q#{ctx.CurrQuestionIndex + 1}/{totalQuestions} | " +
                  $"State: {currentState} | Chosen Difficulty: {difficulty} | ε: {agent.CurrentEpsilon:F3}");
    }

    // === REWARD FUNCTION ===
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
            var timeCat = SARSAController.DiscretizeResponseTime(responseTime);
            float timeBonus = timeCat switch
            {
                SARSAController.ResponseTimeCategory.FAST => 0.5f,
                SARSAController.ResponseTimeCategory.AVERAGE => 0.2f,
                SARSAController.ResponseTimeCategory.SLOW => 0f,
                _ => 0f
            };

            reward += timeBonus;
        }
        else
        {
            // Penalties based on difficulty (asymmetric - lower penalty for hard)
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => -2.0f,   // Should know basics
                QuestionComponent.DifficultyClass.MEDIUM => -1.5f,
                QuestionComponent.DifficultyClass.HARD => -0.5f,   // Expected to struggle
                _ => -1.0f
            };

            // Extra penalty for slow wrong answers (not even close)
            var timeCat = SARSAController.DiscretizeResponseTime(responseTime);
            if (timeCat == SARSAController.ResponseTimeCategory.SLOW)
            {
                reward -= 0.3f; // Additional penalty for slow + wrong
            }
        }

        return reward;
    }
}