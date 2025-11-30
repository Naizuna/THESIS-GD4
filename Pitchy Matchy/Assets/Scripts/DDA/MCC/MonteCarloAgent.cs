using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonteCarloAgent
{
    // Q-table: maps (state, action) to value
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), float> Q
        = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();

    // Returns array for averaging
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), List<float>> returns
        = new Dictionary<(string, QuestionComponent.DifficultyClass), List<float>>();

    private Dictionary<(string state, QuestionComponent.DifficultyClass action), int> visitCounts
        = new Dictionary<(string, QuestionComponent.DifficultyClass), int>();

    // Learning parameters
    private float gamma = 0.95f;          // Discount factor 
    private const float INIT_Q_VALUE = 0f;

    // Exploration parameters
    private float epsilon = 0.15f;
    private float minEpsilon = 0.05f;
    private float decayRate = 0.95f;      

    private System.Random rng = new System.Random();

    //response time enums
    public enum ResponseTimeCategory
    {
        FAST,      
        AVERAGE,    
        SLOW       
    }

    public MonteCarloAgent()
    {
        Debug.Log("[MCC] Initialized with composite state space");
    }

    //state construct
    public static string ConstructState(
        QuestionComponent.DifficultyClass lastDifficulty,
        bool wasCorrect,
        float responseTime)
    {
        string accuracyPart = wasCorrect ? "CORRECT" : "WRONG";
        ResponseTimeCategory timeCat = DiscretizeResponseTime(responseTime);

        return $"{lastDifficulty}_{accuracyPart}_{timeCat}";
    }

    public static ResponseTimeCategory DiscretizeResponseTime(float seconds)
    {
        if (seconds <= 5f)
            return ResponseTimeCategory.FAST;
        else if (seconds <= 10f)
            return ResponseTimeCategory.AVERAGE;
        else
            return ResponseTimeCategory.SLOW;
    }

    // action select
    public QuestionComponent.DifficultyClass ChooseAction(string state)
    {
        // epsilon-greedy policy
        if (rng.NextDouble() < epsilon)
        {
            // Exploration: random action
            Array values = Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
            var action = (QuestionComponent.DifficultyClass)values.GetValue(rng.Next(values.Length));
            Debug.Log($"[MCC] EXPLORE: {state} → {action} (ε={epsilon:F3})");
            return action;
        }
        else
        {
            // Exploitation: best known action
            var action = GetBestAction(state);
            Debug.Log($"[MCC] EXPLOIT: {state} → {action}");
            return action;
        }
    }

    public QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state).ToList();

        if (!actions.Any())
        {
            Debug.Log($"[MCC] No Q-values for state '{state}' - using heuristic fallback");
            return GetHeuristicAction(state);
        }

        // Get best action, break ties randomly
        var maxValue = actions.Max(k => k.Value);
        var bestActions = actions.Where(k => Mathf.Approximately(k.Value, maxValue)).ToList();

        if (bestActions.Count > 1)
        {
            var chosen = bestActions[rng.Next(bestActions.Count)].Key.action;
            Debug.Log($"[MCC] Tie-breaking among {bestActions.Count} actions with Q={maxValue:F2}");
            return chosen;
        }

        return bestActions[0].Key.action;
    }

    // mcc policy update
    /// Updates Q-values based on complete episode using first-visit MC
    public void UpdatePolicy(List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode)
    {
        if (episode.Count == 0)
        {
            Debug.LogWarning("[MCC] Empty episode - no update performed");
            return;
        }

        HashSet<(string, QuestionComponent.DifficultyClass)> visited 
            = new HashSet<(string, QuestionComponent.DifficultyClass)>();
        
        float G = 0f; // Cumulative return

        // Iterate backwards through episode
        for (int t = episode.Count - 1; t >= 0; t--)
        {
            var step = episode[t];
            G = step.reward + gamma * G; // Accumulate discounted return

            var key = (step.state, step.action);

            // First-visit MC: only update first occurrence of state-action pair
            if (!visited.Contains(key))
            {
                // Initialize returns list if needed
                if (!returns.ContainsKey(key))
                    returns[key] = new List<float>();

                // Add this episode's return
                returns[key].Add(G);

                // Update Q-value as average of all returns
                Q[key] = returns[key].Average();

                // Track visit count
                visitCounts[key] = visitCounts.GetValueOrDefault(key, 0) + 1;

                visited.Add(key);

                Debug.Log($"[MCC] UPDATE | State: {step.state} | Action: {step.action} | " +
                          $"Return: {G:F2} | Q: {Q[key]:F2} | Visits: {visitCounts[key]}");
            }
        }

        Debug.Log($"[MCC] Episode complete | Updated {visited.Count} state-action pairs | " +
                  $"Q-table size: {Q.Count}");
    }

    // fallback for unknown states
    public QuestionComponent.DifficultyClass GetHeuristicAction(string state)
    {

        if (state.Contains("CORRECT") && state.Contains("FAST"))
        {
            // Performing well confidently then increase difficulty
            if (state.StartsWith("EASY"))
                return QuestionComponent.DifficultyClass.MEDIUM;
            else if (state.StartsWith("MEDIUM"))
                return QuestionComponent.DifficultyClass.HARD;
            else
                return QuestionComponent.DifficultyClass.HARD;
        }
        else if (state.Contains("WRONG") || state.Contains("SLOW"))
        {
            // Struggling then decrease or maintain difficulty
            if (state.StartsWith("HARD"))
                return QuestionComponent.DifficultyClass.MEDIUM;
            else if (state.StartsWith("MEDIUM"))
                return QuestionComponent.DifficultyClass.EASY;
            else
                return QuestionComponent.DifficultyClass.EASY;
        }
        else
        {
            // Mixed signals then maintain difficulty
            if (state.StartsWith("EASY"))
                return QuestionComponent.DifficultyClass.EASY;
            else if (state.StartsWith("MEDIUM"))
                return QuestionComponent.DifficultyClass.MEDIUM;
            else
                return QuestionComponent.DifficultyClass.HARD;
        }
    }



    // epsilon decay
    public void DecayEpsilon()
    {
        epsilon = Mathf.Max(minEpsilon, epsilon * decayRate);
    }

    public void SetEpsilon(float newEpsilon)
    {
        epsilon = Mathf.Clamp(newEpsilon, minEpsilon, 1f);
    }

    public float CurrentEpsilon => epsilon;

    // stage transitions
    public void OnNewStage()
    {
        // Keep Q-table (transfer learning)
        // Slightly boost exploration for new stage
        epsilon = Mathf.Min(0.2f, epsilon * 1.3f);

        Debug.Log($"[MCC] New stage | Q-table: {Q.Count} entries | ε: {epsilon:F3}");
    }

    // === RESET ===
    public void Reset()
    {
        Q.Clear();
        returns.Clear();
        visitCounts.Clear();
        epsilon = 0.15f;

        Debug.Log("[MCC] Complete reset");
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

    public float GetQValue((string state, QuestionComponent.DifficultyClass action) key)
    {
        return Q.GetValueOrDefault(key, INIT_Q_VALUE);
    }

    private bool HasState(string state) => Q.Keys.Any(k => k.state == state);



    // debugging
    public Dictionary<(string, QuestionComponent.DifficultyClass), float> GetQTable()
        => new Dictionary<(string, QuestionComponent.DifficultyClass), float>(Q);


    public string GetQTableAsString()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== MONTE CARLO CONTROL Q-TABLE ===");
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
            sb.AppendLine("  Action       Q-Value    Visits  Returns  Best?");
            sb.AppendLine("  --------     -------    ------  -------  -----");

            var maxValue = group.Max(x => x.Value);

            foreach (var kvp in group.OrderByDescending(x => x.Value))
            {
                int visits = visitCounts.GetValueOrDefault(kvp.Key, 0);
                int returnCount = returns.GetValueOrDefault(kvp.Key, new List<float>()).Count;
                string bestMarker = Mathf.Approximately(kvp.Value, maxValue) ? "★" : " ";
                sb.AppendLine($"  {kvp.Key.action,-12} {kvp.Value,7:F2}    {visits,6}  {returnCount,7}    {bestMarker}");
            }
            sb.AppendLine("");
        }

        return sb.ToString();
    }

    public void PrintQTable()
    {
        Debug.Log(GetQTableAsString());
    }

    public string GetQTableAsCSV()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("State,Action,QValue,Visits,ReturnCount");

        foreach (var kvp in Q.OrderBy(x => x.Key.state).ThenBy(x => x.Key.action))
        {
            int visits = visitCounts.GetValueOrDefault(kvp.Key, 0);
            int returnCount = returns.GetValueOrDefault(kvp.Key, new List<float>()).Count;
            sb.AppendLine($"{kvp.Key.state},{kvp.Key.action},{kvp.Value:F4},{visits},{returnCount}");
        }

        return sb.ToString();
    }

    public List<QTableEntry> GetAllQTableEntries()
    {
        var entries = new List<QTableEntry>();

        foreach (var kvp in Q)
        {
            entries.Add(new QTableEntry
            {
                State = kvp.Key.state,
                Action = kvp.Key.action,
                QValue = kvp.Value,
                Visits = visitCounts.GetValueOrDefault(kvp.Key, 0),
                ReturnCount = returns.GetValueOrDefault(kvp.Key, new List<float>()).Count
            });
        }

        return entries;
    }

    public void PrintStateSpaceInfo()
    {
        int totalStates = Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length *
                         2 * // CORRECT/WRONG
                         Enum.GetValues(typeof(ResponseTimeCategory)).Length;

        int exploredStates = Q.Select(kvp => kvp.Key.state).Distinct().Count();

        Debug.Log($"[MCC] State Space: {exploredStates}/{totalStates} states explored " +
                  $"({(float)exploredStates / totalStates * 100:F1}%)");
    }

    // Data structure for Q-table entries
    [System.Serializable]
    public struct QTableEntry
    {
        public string State;
        public QuestionComponent.DifficultyClass Action;
        public float QValue;
        public int Visits;
        public int ReturnCount;

        public override string ToString()
        {
            return $"Q({State}, {Action}) = {QValue:F2} [visits: {Visits}, returns: {ReturnCount}]";
        }
    }

}