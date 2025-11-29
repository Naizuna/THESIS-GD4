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

    // Learning parameters - TUNED FOR FAST BUT STABLE ADAPTATION
    private float alpha = 0.4f;           // Moderate-high learning rate
    private float gamma = 0.9f;           // Balance immediate and future rewards
    
    // CONSERVATIVE initialization - start neutral
    private const float INIT_Q_VALUE = 0f;

    // Exploration parameters
    private float epsilon = 0.15f;        
    private float minEpsilon = 0.05f;     
    private float decayRate = 0.95f;      
    
    private System.Random rng = new System.Random();

    // === DIFFICULTY MOMENTUM ===
    // Track recent difficulty selections to prevent wild swings
    private Queue<QuestionComponent.DifficultyClass> recentDifficulties = new Queue<QuestionComponent.DifficultyClass>();
    private const int DIFFICULTY_MEMORY = 3; // Remember last 3 difficulties

    // === RECENCY-WEIGHTED EXPERIENCE ===
    private Dictionary<QuestionComponent.DifficultyClass, Queue<bool>> recentPerformance 
        = new Dictionary<QuestionComponent.DifficultyClass, Queue<bool>>();
    private const int RECENCY_WINDOW = 3;

    public SARSAController()
    {
        // Initialize recency tracking
        foreach (QuestionComponent.DifficultyClass diff in Enum.GetValues(typeof(QuestionComponent.DifficultyClass)))
        {
            recentPerformance[diff] = new Queue<bool>();
        }
    }

    // === ACTION SELECTION WITH SMOOTHING ===
    public QuestionComponent.DifficultyClass ChooseAction(string state)
    {
        // ε-greedy exploration
        if (rng.NextDouble() < epsilon)
        {
            // Exploration: but favor adjacent difficulties for smoother transitions
            return GetSmoothedRandomAction();
        }
        else
        {
            // Exploitation: best action with stability check
            return GetBestActionWithSmoothing(state);
        }
    }

    // Get random action but weighted toward current difficulty level
    private QuestionComponent.DifficultyClass GetSmoothedRandomAction()
    {
        if (recentDifficulties.Count == 0)
        {
            // First question: start easy
            return QuestionComponent.DifficultyClass.EASY;
        }

        var currentDiff = recentDifficulties.Last();
        
        // 60% chance: stay at current level or move one step
        // 40% chance: any difficulty
        if (rng.NextDouble() < 0.6)
        {
            int choice = rng.Next(3);
            return currentDiff switch
            {
                QuestionComponent.DifficultyClass.EASY => choice switch
                {
                    0 => QuestionComponent.DifficultyClass.EASY,
                    1 => QuestionComponent.DifficultyClass.EASY,
                    _ => QuestionComponent.DifficultyClass.MEDIUM
                },
                QuestionComponent.DifficultyClass.MEDIUM => choice switch
                {
                    0 => QuestionComponent.DifficultyClass.EASY,
                    1 => QuestionComponent.DifficultyClass.MEDIUM,
                    _ => QuestionComponent.DifficultyClass.HARD
                },
                QuestionComponent.DifficultyClass.HARD => choice switch
                {
                    0 => QuestionComponent.DifficultyClass.MEDIUM,
                    1 => QuestionComponent.DifficultyClass.HARD,
                    _ => QuestionComponent.DifficultyClass.HARD
                },
                _ => QuestionComponent.DifficultyClass.EASY
            };
        }

        // Full random
        Array values = Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
        return (QuestionComponent.DifficultyClass)values.GetValue(rng.Next(values.Length));
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
        
        // Track difficulty history
        recentDifficulties.Enqueue(action);
        if (recentDifficulties.Count > DIFFICULTY_MEMORY)
        {
            recentDifficulties.Dequeue();
        }
        
        // RECENCY BONUS: Amplify learning for consistent patterns
        float recencyBonus = CalculateRecencyBonus(action);
        float adjustedReward = reward * (1f + recencyBonus);
        
        // Adaptive learning rate: faster for new experiences
        float adaptiveAlpha = visitCounts[key] <= 2 ? alpha : alpha * 0.75f;
        
        float currentQ = GetQValue(state, action);
        float nextQ = GetQValue(nextState, nextAction);
        
        // SARSA update
        float tdError = adjustedReward + gamma * nextQ - currentQ;
        float updated = currentQ + adaptiveAlpha * tdError;
        
        Q[key] = updated;
        
        Debug.Log($"[SARSA] Q({state},{action}): {currentQ:F2}→{updated:F2} | r={reward:F2} (adj:{adjustedReward:F2}) | α={adaptiveAlpha:F2} | visits={visitCounts[key]}");
    }

    // Calculate bonus based on recent consistency
    private float CalculateRecencyBonus(QuestionComponent.DifficultyClass action)
    {
        var recent = recentPerformance[action];
        if (recent.Count < 2) return 0f;
        
        // Strong consistent pattern = higher bonus
        bool allSame = recent.All(r => r == recent.First());
        return allSame ? 0.25f : 0f;
    }

    // === EPSILON HANDLING ===
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
        // Keep Q-table and difficulty history for smoother transitions
        foreach (var queue in recentPerformance.Values)
        {
            queue.Clear();
        }
        
        epsilon = Mathf.Min(0.2f, epsilon * 1.5f);
        
        Debug.Log($"[SARSA] New stage | Q-table: {Q.Count} entries | ε: {epsilon:F3}");
    }

    // Reset everything for new session
    public void Reset()
    {
        Q.Clear();
        visitCounts.Clear();
        recentDifficulties.Clear();
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

    private QuestionComponent.DifficultyClass GetBestActionWithSmoothing(string state)
    {
        var actions = Q.Where(k => k.Key.state == state);
        
        if (!actions.Any())
        {
            Debug.Log($"[SARSA] No Q-values for state '{state}' - using heuristic");
            return GetHeuristicAction(state);
        }
        
        var bestAction = actions.OrderByDescending(k => k.Value).First().Key.action;
        
        // === STABILITY CHECK ===
        // If we're about to make a big jump (e.g., EASY → HARD), check if it's justified
        if (recentDifficulties.Count > 0)
        {
            var currentDiff = recentDifficulties.Last();
            int diffJump = Math.Abs((int)bestAction - (int)currentDiff);
            
            // If jumping 2 levels (EASY ↔ HARD), require strong evidence
            if (diffJump >= 2)
            {
                var actionQValue = Q[(state, bestAction)];
                var actionVisits = visitCounts.GetValueOrDefault((state, bestAction), 0);
                
                // Need at least 2 visits and significantly better Q-value to justify big jump
                var otherActions = actions.Where(a => a.Key.action != bestAction);
                if (actionVisits < 2 || !otherActions.Any())
                {
                    // Not enough confidence - take intermediate step instead
                    return GetIntermediateDifficulty(currentDiff, bestAction);
                }
                
                var secondBestQ = otherActions.Max(k => k.Value);
                float qDifference = actionQValue - secondBestQ;
                
                // Require significant Q-value advantage (at least 1.0 difference)
                if (qDifference < 1.0f)
                {
                    return GetIntermediateDifficulty(currentDiff, bestAction);
                }
            }
        }
        
        return bestAction;
    }

    // Get intermediate difficulty between current and target
    private QuestionComponent.DifficultyClass GetIntermediateDifficulty(
        QuestionComponent.DifficultyClass current, 
        QuestionComponent.DifficultyClass target)
    {
        int currentLevel = (int)current;
        int targetLevel = (int)target;
        
        if (currentLevel < targetLevel)
        {
            // Moving up: take one step
            return (QuestionComponent.DifficultyClass)(currentLevel + 1);
        }
        else if (currentLevel > targetLevel)
        {
            // Moving down: take one step
            return (QuestionComponent.DifficultyClass)(currentLevel - 1);
        }
        
        return current;
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
        
        if (recentDifficulties.Count > 0)
        {
            Debug.Log($"Recent difficulties: {string.Join(" → ", recentDifficulties)}");
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