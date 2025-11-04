using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SARSAController
{
    // Q-table: maps (state, action) to value
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), float> Q 
        = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();

    // Learning parameters
    private float alpha = 0.1f;    // Learning rate
    private float gamma = 0.99f;    // Discount factor
    private float optimisticInit = 0f; // Small positive initialization for unseen pairs

    // Exploration parameters
    private float epsilon = 0.1f;
    private float minEpsilon = 0.01f;
    private float decayRate = 0.95f;

    private System.Random rng = new System.Random();

    // --- ACTION SELECTION ---
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

    // --- Q-VALUE UPDATE (Standard SARSA) ---
    public void UpdateQValue(string state, QuestionComponent.DifficultyClass action, float reward,
                             string nextState, QuestionComponent.DifficultyClass nextAction)
    {
        float currentQ = GetQValue(state, action);
        float nextQ = GetQValue(nextState, nextAction);
        float updated = currentQ + alpha * (reward + gamma * nextQ - currentQ);
        Q[(state, action)] = updated;
    }

    // --- EPSILON HANDLING ---
    public void DecayEpsilon()
    {
        epsilon = Mathf.Max(minEpsilon, epsilon * decayRate);
    }

    public void SetEpsilon(float newEpsilon)
    {
        epsilon = Mathf.Clamp(newEpsilon, minEpsilon, 1f);
    }

    public float CurrentEpsilon => epsilon;

    // --- Q-TABLE UTILITIES ---
    private float GetQValue(string state, QuestionComponent.DifficultyClass action)
    {
        if (!Q.ContainsKey((state, action)))
        {
            Q[(state, action)] = optimisticInit; // optimistic initialization
        }
        return Q[(state, action)];
    }

    private bool HasState(string state) => Q.Keys.Any(k => k.state == state);

    private QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state);
        if (!actions.Any()) return QuestionComponent.DifficultyClass.EASY;
        return actions.OrderByDescending(k => k.Value).First().Key.action;
    }

    // --- Optional Debug ---
    public Dictionary<(string, QuestionComponent.DifficultyClass), float> GetQTable()
        => new Dictionary<(string, QuestionComponent.DifficultyClass), float>(Q);

    // --- Optional I/O stubs ---
    public void SavePolicy(string path) => Debug.Log($"[SARSA] SavePolicy() stub to {path}");
    public void LoadPolicy(string path) => Debug.Log($"[SARSA] LoadPolicy() stub from {path}");
}
