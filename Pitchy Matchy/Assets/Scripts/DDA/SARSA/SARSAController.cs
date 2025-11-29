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
    private float alpha = 0.5f;           // Learning rate
    private float gamma = 0.85f;          // Discount factor
    
    private const float INIT_Q_VALUE = 0f;

    // Exploration parameters
    private float epsilon = 0.15f;        
    private float minEpsilon = 0.05f;     
    private float decayRate = 0.95f;      
    
    private System.Random rng = new System.Random();

    // === RESPONSE TIME DISCRETIZATION ===
    // Based on typical reaction times for cognitive tasks
    public enum ResponseTimeCategory
    {
        FAST,      // <= 3 seconds (automatic/confident)
        AVERAGE,    // 3-7 seconds (deliberate thinking)
        SLOW       // > 7 seconds (struggling/uncertain)
    }

    public SARSAController()
    {
        Debug.Log("[SARSA] Initialized with composite state space");
    }

    // === STATE CONSTRUCTION ===
    /// <summary>
    /// Constructs state from last question's context
    /// Format: "DIFFICULTY_ACCURACY_RESPONSETIME"
    /// Example: "MEDIUM_CORRECT_FAST"
    /// </summary>
    public static string ConstructState(
        QuestionComponent.DifficultyClass lastDifficulty, 
        bool wasCorrect, 
        float responseTime)
    {
        string accuracyPart = wasCorrect ? "CORRECT" : "WRONG";
        ResponseTimeCategory timeCat = DiscretizeResponseTime(responseTime);
        
        return $"{lastDifficulty}_{accuracyPart}_{timeCat}";
    }

    /// <summary>
    /// Discretizes continuous response time into categories
    /// Based on cognitive psychology literature on reaction times
    /// </summary>
    public static ResponseTimeCategory DiscretizeResponseTime(float seconds)
    {
        if (seconds <= 3f)
            return ResponseTimeCategory.FAST;
        else if (seconds <= 7f)
            return ResponseTimeCategory.AVERAGE;
        else
            return ResponseTimeCategory.SLOW;
    }

    // === ACTION SELECTION ===
    public QuestionComponent.DifficultyClass ChooseAction(string state)
    {
        // ε-greedy policy
        if (rng.NextDouble() < epsilon)
        {
            // Exploration: random action
            Array values = Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
            var action = (QuestionComponent.DifficultyClass)values.GetValue(rng.Next(values.Length));
            Debug.Log($"[SARSA] EXPLORE: {state} → {action} (ε={epsilon:F3})");
            return action;
        }
        else
        {
            // Exploitation: best known action
            var action = GetBestAction(state);
            Debug.Log($"[SARSA] EXPLOIT: {state} → {action}");
            return action;
        }
    }

    // === HEURISTIC FALLBACK FOR UNSEEN STATES ===
    public QuestionComponent.DifficultyClass GetHeuristicAction(string state)
    {
        // Parse the state to make intelligent default decisions
        if (state.Contains("CORRECT") && state.Contains("FAST"))
        {
            // Performing well confidently → increase difficulty
            if (state.StartsWith("EASY"))
                return QuestionComponent.DifficultyClass.MEDIUM;
            else if (state.StartsWith("MEDIUM"))
                return QuestionComponent.DifficultyClass.HARD;
            else
                return QuestionComponent.DifficultyClass.HARD;
        }
        else if (state.Contains("WRONG") || state.Contains("SLOW"))
        {
            // Struggling → decrease or maintain difficulty
            if (state.StartsWith("HARD"))
                return QuestionComponent.DifficultyClass.MEDIUM;
            else if (state.StartsWith("MEDIUM"))
                return QuestionComponent.DifficultyClass.EASY;
            else
                return QuestionComponent.DifficultyClass.EASY;
        }
        else
        {
            // Mixed signals (e.g., CORRECT but SLOW) → maintain difficulty
            if (state.StartsWith("EASY"))
                return QuestionComponent.DifficultyClass.EASY;
            else if (state.StartsWith("MEDIUM"))
                return QuestionComponent.DifficultyClass.MEDIUM;
            else
                return QuestionComponent.DifficultyClass.HARD;
        }
    }

    // === Q-VALUE UPDATE (SARSA) ===
    public void UpdateQValue(
        string state, 
        QuestionComponent.DifficultyClass action, 
        float reward,
        string nextState, 
        QuestionComponent.DifficultyClass nextAction)
    {
        var key = (state, action);
        
        // Track visit counts
        visitCounts[key] = visitCounts.GetValueOrDefault(key, 0) + 1;
        
        // Adaptive learning rate (higher for new experiences)
        float adaptiveAlpha = visitCounts[key] <= 2 ? alpha : alpha * 0.7f;
        
        float currentQ = GetQValue(state, action);
        float nextQ = GetQValue(nextState, nextAction);
        
        // SARSA update: Q(s,a) ← Q(s,a) + α[r + γQ(s',a') - Q(s,a)]
        float tdError = reward + gamma * nextQ - currentQ;
        float newQ = currentQ + adaptiveAlpha * tdError;
        
        Q[key] = newQ;
        
        Debug.Log($"[SARSA] UPDATE | State: {state} | Action: {action} | Q: {currentQ:F2}→{newQ:F2} | " +
                  $"Reward: {reward:F2} | TD-Error: {tdError:F2} | α: {adaptiveAlpha:F2} | Visits: {visitCounts[key]}");
    }

    // === EPSILON DECAY ===
    public void DecayEpsilon()
    {
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
        // Keep Q-table (transfer learning across stages)
        // Slightly boost exploration for new stage context
        epsilon = Mathf.Min(0.2f, epsilon * 1.3f);
        
        Debug.Log($"[SARSA] New stage | Q-table: {Q.Count} entries | ε: {epsilon:F3}");
    }

    // === RESET ===
    public void Reset()
    {
        Q.Clear();
        visitCounts.Clear();
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

    private QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state).ToList();
        
        if (!actions.Any())
        {
            Debug.Log($"[SARSA] No Q-values for state '{state}' - using heuristic fallback");
            return GetHeuristicAction(state);
        }
        
        // Get best action, break ties randomly
        var maxValue = actions.Max(k => k.Value);
        var bestActions = actions.Where(k => Mathf.Approximately(k.Value, maxValue)).ToList();
        
        if (bestActions.Count > 1)
        {
            var chosen = bestActions[rng.Next(bestActions.Count)].Key.action;
            Debug.Log($"[SARSA] Tie-breaking among {bestActions.Count} actions with Q={maxValue:F2}");
            return chosen;
        }
        
        return bestActions[0].Key.action;
    }

    // === DEBUG & ANALYSIS ===
    public Dictionary<(string, QuestionComponent.DifficultyClass), float> GetQTable()
        => new Dictionary<(string, QuestionComponent.DifficultyClass), float>(Q);

    public void PrintQTable()
    {
        Debug.Log("=== SARSA Q-TABLE ===");
        Debug.Log($"Total entries: {Q.Count}");
        Debug.Log($"Current ε: {epsilon:F3}");
        Debug.Log("");
        
        var grouped = Q.GroupBy(kvp => kvp.Key.state)
                       .OrderBy(g => g.Key);
        
        foreach (var group in grouped)
        {
            Debug.Log($"State: {group.Key}");
            foreach (var kvp in group.OrderByDescending(x => x.Value))
            {
                int visits = visitCounts.GetValueOrDefault(kvp.Key, 0);
                string bestMarker = kvp.Value == group.Max(x => x.Value) ? "★" : " ";
                Debug.Log($"  {bestMarker} {kvp.Key.action,-8} Q={kvp.Value,6:F2}  [visits: {visits}]");
            }
            Debug.Log("");
        }
    }

    public void PrintStateSpaceInfo()
    {
        int totalStates = Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length * 
                         2 * // CORRECT/WRONG
                         Enum.GetValues(typeof(ResponseTimeCategory)).Length;
        
        int exploredStates = Q.Select(kvp => kvp.Key.state).Distinct().Count();
        
        Debug.Log($"[SARSA] State Space: {exploredStates}/{totalStates} states explored " +
                  $"({(float)exploredStates/totalStates*100:F1}%)");
    }

    // === DEBUG & ANALYSIS ===
    public Dictionary<(string, QuestionComponent.DifficultyClass), float> GetQTableAll()
        => new Dictionary<(string, QuestionComponent.DifficultyClass), float>(Q);

    /// <summary>
    /// Returns a formatted string of the entire Q-table for display/export
    /// </summary>
    public string GetQTableAsString()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("=== SARSA Q-TABLE ===");
        sb.AppendLine($"Total entries: {Q.Count}");
        sb.AppendLine($"Unique states: {Q.Select(kvp => kvp.Key.state).Distinct().Count()}");
        sb.AppendLine($"Current ε: {epsilon:F3}");
        sb.AppendLine("");
        
        if (Q.Count == 0)
        {
            sb.AppendLine("(Empty Q-table - no learning has occurred yet)");
            return sb.ToString();
        }
        
        var grouped = Q.GroupBy(kvp => kvp.Key.state)
                       .OrderBy(g => g.Key);
        
        foreach (var group in grouped)
        {
            sb.AppendLine($"State: {group.Key}");
            sb.AppendLine("  Action       Q-Value    Visits  Best?");
            sb.AppendLine("  --------     -------    ------  -----");
            
            var maxValue = group.Max(x => x.Value);
            
            foreach (var kvp in group.OrderByDescending(x => x.Value))
            {
                int visits = visitCounts.GetValueOrDefault(kvp.Key, 0);
                string bestMarker = Mathf.Approximately(kvp.Value, maxValue) ? "★" : " ";
                sb.AppendLine($"  {kvp.Key.action,-12} {kvp.Value,7:F2}    {visits,6}    {bestMarker}");
            }
            sb.AppendLine("");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Prints Q-table to Unity console (grouped by state, sorted by Q-value)
    /// </summary>
    public void PrintQTableAll()
    {
        Debug.Log(GetQTableAsString());
    }
}