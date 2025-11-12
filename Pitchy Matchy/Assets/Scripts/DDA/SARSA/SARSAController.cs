using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SARSAController
{
    // Q-table: maps (state, action) to value
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), float> Q 
        = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();

    // Visit counts for adaptive learning
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), int> visitCounts 
        = new Dictionary<(string, QuestionComponent.DifficultyClass), int>();

    // Learning parameters
    private float alpha = 0.2f;           // Higher learning rate for short quizzes
    private float gamma = 0.95f;          // Slightly lower gamma for short-term focus
    
    // === OPTIMISTIC INITIALIZATION ===
    // Encourages exploration of harder difficulties early
    private Dictionary<QuestionComponent.DifficultyClass, float> optimisticInit = new()
    {
        { QuestionComponent.DifficultyClass.EASY, 0.5f },
        { QuestionComponent.DifficultyClass.MEDIUM, 1.0f },
        { QuestionComponent.DifficultyClass.HARD, 1.5f }
    };

    // === REWARD NORMALIZATION ===
    private float rewardSum = 0f;
    private float rewardSumSquared = 0f;
    private int rewardCount = 0;
    private const float EPSILON_NORM = 1e-8f; // Prevent division by zero

    // Exploration parameters - tuned for 12-30 question quizzes
    private float epsilon = 0.2f;          // Start with higher exploration
    private float minEpsilon = 0.05f;      // Maintain some exploration
    private float decayRate = 0.98f;       // Moderate decay
    
    // Decay control - decay every N questions for stability
    private int questionsBeforeDecay = 2;
    private int questionsSinceDecay = 0;

    private System.Random rng = new System.Random();

    // === ACTION SELECTION ===
    public QuestionComponent.DifficultyClass ChooseAction(string state)
    {
        // ε-greedy exploration
        if (rng.NextDouble() < epsilon || !HasState(state))
        {
            Array values = Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
            return (QuestionComponent.DifficultyClass)values.GetValue(rng.Next(values.Length));
        }
        else
        {
            return GetBestAction(state);
        }
    }

    // === HEURISTIC FALLBACK ===
    // Returns a safe difficulty based on state when Q-table is sparse
    public QuestionComponent.DifficultyClass GetHeuristicAction(string state)
    {
        return state switch
        {
            "START" => QuestionComponent.DifficultyClass.EASY,
            "STRUGGLING" => QuestionComponent.DifficultyClass.EASY,
            "AVERAGE" => QuestionComponent.DifficultyClass.MEDIUM,
            "MASTERING" => QuestionComponent.DifficultyClass.HARD,
            "TERMINAL" => QuestionComponent.DifficultyClass.MEDIUM,
            _ => QuestionComponent.DifficultyClass.EASY
        };
    }

    // === Q-VALUE UPDATE (SARSA with Normalization) ===
    public void UpdateQValue(string state, QuestionComponent.DifficultyClass action, float reward,
                             string nextState, QuestionComponent.DifficultyClass nextAction)
    {
        var key = (state, action);
        
        // Track visit counts
        visitCounts[key] = visitCounts.GetValueOrDefault(key, 0) + 1;
        
        // === REWARD NORMALIZATION ===
        UpdateRewardStats(reward);
        float normalizedReward = NormalizeReward(reward);
        
        // Adaptive learning rate: learn faster from novel experiences
        float adaptiveAlpha = alpha / (1f + 0.1f * Mathf.Sqrt(visitCounts[key]));
        
        float currentQ = GetQValue(state, action);
        float nextQ = GetQValue(nextState, nextAction);
        
        // Standard SARSA update with normalized reward
        float tdError = normalizedReward + gamma * nextQ - currentQ;
        float updated = currentQ + adaptiveAlpha * tdError;
        
        Q[key] = updated;
        
        Debug.Log($"[SARSA] Q({state},{action}): {currentQ:F2}→{updated:F2} | r={reward:F2}→{normalizedReward:F2} | α={adaptiveAlpha:F3}");
    }

    // Update running statistics for reward normalization
    private void UpdateRewardStats(float reward)
    {
        rewardCount++;
        rewardSum += reward;
        rewardSumSquared += reward * reward;
    }

    // Normalize reward using running mean and standard deviation
    private float NormalizeReward(float reward)
    {
        if (rewardCount < 2) return reward; // Need at least 2 samples
        
        float mean = rewardSum / rewardCount;
        float variance = (rewardSumSquared / rewardCount) - (mean * mean);
        float stdDev = Mathf.Sqrt(Mathf.Max(variance, 0f)) + EPSILON_NORM;
        
        return (reward - mean) / stdDev;
    }

    // === EPSILON HANDLING ===
    public void DecayEpsilon()
    {
        questionsSinceDecay++;
        
        // Only decay every N questions for stability
        if (questionsSinceDecay >= questionsBeforeDecay)
        {
            epsilon = Mathf.Max(minEpsilon, epsilon * decayRate);
            questionsSinceDecay = 0;
        }
    }

    public void SetEpsilon(float newEpsilon)
    {
        epsilon = Mathf.Clamp(newEpsilon, minEpsilon, 1f);
    }

    public float CurrentEpsilon => epsilon;

    // === STAGE TRANSITION ===
    // Maintains Q-table across stages but boosts exploration
    public void OnNewStage()
    {
        // Keep Q-table and reward stats for transfer learning
        // Slightly increase exploration for new stage
        epsilon = Mathf.Min(epsilon * 1.2f, 0.25f);
        questionsSinceDecay = 0;
        
        Debug.Log($"[SARSA] New stage | Q-table: {Q.Count} entries | Rewards: {rewardCount} | ε: {epsilon:F3}");
    }

    // Reset everything for completely new session
    public void Reset()
    {
        Q.Clear();
        visitCounts.Clear();
        rewardSum = 0f;
        rewardSumSquared = 0f;
        rewardCount = 0;
        epsilon = 0.2f;
        questionsSinceDecay = 0;
        
        Debug.Log("[SARSA] Complete reset");
    }

    // === Q-TABLE UTILITIES ===
    private float GetQValue(string state, QuestionComponent.DifficultyClass action)
    {
        if (!Q.ContainsKey((state, action)))
        {
            // === OPTIMISTIC INITIALIZATION ===
            Q[(state, action)] = optimisticInit[action];
        }
        return Q[(state, action)];
    }

    private bool HasState(string state) => Q.Keys.Any(k => k.state == state);

    private QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state);
        
        if (!actions.Any())
        {
            // === HEURISTIC FALLBACK ===
            Debug.Log($"[SARSA] No Q-values for state '{state}' - using heuristic");
            return GetHeuristicAction(state);
        }
        
        return actions.OrderByDescending(k => k.Value).First().Key.action;
    }

    // === DEBUG & ANALYSIS ===
    public Dictionary<(string, QuestionComponent.DifficultyClass), float> GetQTable()
        => new Dictionary<(string, QuestionComponent.DifficultyClass), float>(Q);

    public void PrintQTable()
    {
        Debug.Log("=== Q-Table ===");
        foreach (var kvp in Q.OrderBy(x => x.Key.state).ThenBy(x => x.Key.action))
        {
            int visits = visitCounts.GetValueOrDefault(kvp.Key, 0);
            Debug.Log($"Q({kvp.Key.state}, {kvp.Key.action}) = {kvp.Value:F2} [visits: {visits}]");
        }
        
        if (rewardCount > 0)
        {
            float mean = rewardSum / rewardCount;
            float variance = (rewardSumSquared / rewardCount) - (mean * mean);
            float stdDev = Mathf.Sqrt(Mathf.Max(variance, 0f));
            Debug.Log($"Reward stats: μ={mean:F2}, σ={stdDev:F2}, n={rewardCount}");
        }
    }

    // === PERSISTENCE ===
    public void SavePolicy(string path) 
    {
        // TODO: Serialize Q, visitCounts, reward stats
        Debug.Log($"[SARSA] SavePolicy() to {path} - {Q.Count} entries");
    }
    
    public void LoadPolicy(string path) 
    {
        // TODO: Deserialize Q, visitCounts, reward stats
        Debug.Log($"[SARSA] LoadPolicy() from {path}");
    }
}