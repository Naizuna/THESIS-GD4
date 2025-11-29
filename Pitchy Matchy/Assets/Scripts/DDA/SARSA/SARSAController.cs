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

    // Learning parameters - TUNED FOR FAST ADAPTATION
    private float alpha = 0.5f;           // HIGH learning rate for quick updates
    private float gamma = 0.85f;          // Lower discount - prioritize immediate feedback
    
    // CONSERVATIVE initialization - start neutral, let experience guide quickly
    private const float INIT_Q_VALUE = 0f;

    // Exploration parameters - AGGRESSIVE GREEDY EXPLOITATION
    private float epsilon = 0.085f;        // Start with some exploration
    private float minEpsilon = 0.05f;     // Minimal exploration floor
    private float decayRate = 0.92f;      // Decay after EVERY question
    
    private System.Random rng = new System.Random();

    // === RECENCY-WEIGHTED EXPERIENCE ===
    // Track recent performance per difficulty to make quick adjustments
    private Dictionary<QuestionComponent.DifficultyClass, Queue<bool>> recentPerformance 
        = new Dictionary<QuestionComponent.DifficultyClass, Queue<bool>>();
    private const int RECENCY_WINDOW = 3; // Only last 3 attempts per difficulty

    public SARSAController()
    {
        // Initialize recency tracking
        foreach (QuestionComponent.DifficultyClass diff in Enum.GetValues(typeof(QuestionComponent.DifficultyClass)))
        {
            recentPerformance[diff] = new Queue<bool>();
        }
    }

    // === ACTION SELECTION ===
    public QuestionComponent.DifficultyClass ChooseAction(string state)
    {
        // ε-greedy with fast exploitation
        if (rng.NextDouble() < epsilon)
        {
            // Exploration: random action
            Array values = Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
            return (QuestionComponent.DifficultyClass)values.GetValue(rng.Next(values.Length));
        }
        else
        {
            // Exploitation: best known action
            return GetBestAction(state);
        }
    }

    // === HEURISTIC FALLBACK ===
    public QuestionComponent.DifficultyClass GetHeuristicAction(string state)
    {
        return state switch
        {
            "START" => QuestionComponent.DifficultyClass.EASY,
            "STRUGGLING" => QuestionComponent.DifficultyClass.EASY,
            "INCONSISTENT" => QuestionComponent.DifficultyClass.EASY,
            "IMPROVING" => QuestionComponent.DifficultyClass.MEDIUM,
            "CONSISTENT" => QuestionComponent.DifficultyClass.MEDIUM,
            "MASTERING" => QuestionComponent.DifficultyClass.HARD,
            "TERMINAL" => QuestionComponent.DifficultyClass.MEDIUM,
            _ => QuestionComponent.DifficultyClass.EASY
        };
    }

    // === Q-VALUE UPDATE (SARSA with Recency Bonus) ===
    public void UpdateQValue(string state, QuestionComponent.DifficultyClass action, float reward,
                             string nextState, QuestionComponent.DifficultyClass nextAction)
    {
        var key = (state, action);
        
        // Track visit counts
        visitCounts[key] = visitCounts.GetValueOrDefault(key, 0) + 1;
        
        // Track recent performance for this difficulty
        bool wasCorrect = reward > 0;
        recentPerformance[action].Enqueue(wasCorrect);
        if (recentPerformance[action].Count > RECENCY_WINDOW)
        {
            recentPerformance[action].Dequeue();
        }
        
        // RECENCY BONUS: Amplify learning for recent patterns
        float recencyBonus = CalculateRecencyBonus(action);
        float adjustedReward = reward * (1f + recencyBonus);
        
        // HIGH learning rate for new states, moderate for visited states
        float adaptiveAlpha = visitCounts[key] <= 2 ? alpha : alpha * 0.7f;
        
        float currentQ = GetQValue(state, action);
        float nextQ = GetQValue(nextState, nextAction);
        
        // SARSA update with adjusted reward
        float tdError = adjustedReward + gamma * nextQ - currentQ;
        float updated = currentQ + adaptiveAlpha * tdError;
        
        Q[key] = updated;
        
        Debug.Log($"[SARSA] Q({state},{action}): {currentQ:F2}→{updated:F2} | r={reward:F2} (adj:{adjustedReward:F2}) | α={adaptiveAlpha:F2} | visits={visitCounts[key]}");
    }

    // Calculate bonus based on recent consistency in performance
    private float CalculateRecencyBonus(QuestionComponent.DifficultyClass action)
    {
        var recent = recentPerformance[action];
        if (recent.Count < 2) return 0f;
        
        // If all recent attempts are same outcome (all correct or all wrong), amplify signal
        bool allSame = recent.All(r => r == recent.First());
        return allSame ? 0.3f : 0f; // 30% bonus for consistent patterns
    }

    // === EPSILON HANDLING ===
    public void DecayEpsilon()
    {
        // Decay after EVERY question for fast convergence
        epsilon = Mathf.Max(minEpsilon, epsilon * decayRate);
    }

    public void SetEpsilon(float newEpsilon)
    {
        epsilon = Mathf.Clamp(newEpsilon, minEpsilon, 1f);
    }

    public float CurrentEpsilon => epsilon;

    // === STAGE TRANSITION ===
    public void OnNewStage()
    {
        // Keep Q-table but reset recency tracking
        foreach (var queue in recentPerformance.Values)
        {
            queue.Clear();
        }
        
        // Boost exploration slightly for new stage
        epsilon = Mathf.Min(0.2f, epsilon * 1.5f);
        
        Debug.Log($"[SARSA] New stage | Q-table: {Q.Count} entries | ε: {epsilon:F3}");
    }

    // Reset everything for new session
    public void Reset()
    {
        Q.Clear();
        visitCounts.Clear();
        foreach (var queue in recentPerformance.Values)
        {
            queue.Clear();
        }
        epsilon = 0.15f;
        
        Debug.Log("[SARSA] Complete reset");
    }

    // === Q-TABLE UTILITIES ===
    private float GetQValue(string state, QuestionComponent.DifficultyClass action)
    {
        if (!Q.ContainsKey((state, action)))
        {
            Q[(state, action)] = INIT_Q_VALUE;
        }
        return Q[(state, action)];
    }

    private bool HasState(string state) => Q.Keys.Any(k => k.state == state);

    private QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state);
        
        if (!actions.Any())
        {
            Debug.Log($"[SARSA] No Q-values for state '{state}' - using heuristic");
            return GetHeuristicAction(state);
        }
        
        // Break ties randomly to avoid getting stuck
        var maxValue = actions.Max(k => k.Value);
        var bestActions = actions.Where(k => Mathf.Approximately(k.Value, maxValue)).ToList();
        
        if (bestActions.Count > 1)
        {
            return bestActions[rng.Next(bestActions.Count)].Key.action;
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
            var recent = recentPerformance[kvp.Key.action];
            string recentStr = recent.Count > 0 ? 
                string.Join("", recent.Select(r => r ? "✓" : "✗")) : "-";
            Debug.Log($"Q({kvp.Key.state}, {kvp.Key.action}) = {kvp.Value:F2} [visits: {visits}, recent: {recentStr}]");
        }
    }

    // === PERSISTENCE ===
    public void SavePolicy(string path) 
    {
        Debug.Log($"[SARSA] SavePolicy() to {path} - {Q.Count} entries");
    }
    
    public void LoadPolicy(string path) 
    {
        Debug.Log($"[SARSA] LoadPolicy() from {path}");
    }
}