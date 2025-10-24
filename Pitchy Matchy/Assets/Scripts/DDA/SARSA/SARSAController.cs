using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SARSAController
{
    // Q[state, action] = value
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), float> Q
        = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();

    // Learning parameters
    private float alpha = 0.1f; // Learning rate
    private float gamma = 0.9f; // Discount factor
    private System.Random rng = new System.Random();

    public QuestionComponent.DifficultyClass ChooseAction(string state, float epsilon = 0.1f)
    {
        // Explore
        if (rng.NextDouble() < epsilon || !HasState(state))
        {
            Array values = Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
            return (QuestionComponent.DifficultyClass)values.GetValue(rng.Next(values.Length));
        }
        else
        {
            // Exploit
            return GetBestAction(state);
        }
    }

    public void UpdateQValue(string state, QuestionComponent.DifficultyClass action, float reward, 
                           string nextState, QuestionComponent.DifficultyClass nextAction)
    {
        // Get current Q-value (or 0 if not exists)
        float currentQ = GetQValue(state, action);
        
        // Get next Q-value (or 0 if not exists)
        float nextQ = GetQValue(nextState, nextAction);
        
        // SARSA update formula: Q(s,a) = Q(s,a) + α[r + γQ(s',a') - Q(s,a)]
        float newQ = currentQ + alpha * (reward + gamma * nextQ - currentQ);
        
        Q[(state, action)] = newQ;
    }

    // Helper method to get Q-value with default value for unseen state-action pairs
    private float GetQValue(string state, QuestionComponent.DifficultyClass action)
    {
        if (Q.TryGetValue((state, action), out float value))
        {
            return value;
        }
        return 0f; // Default value for unseen state-action pairs
    }

    private bool HasState(string state) => Q.Keys.Any(k => k.state == state);

    private QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state);
        if (!actions.Any()) return QuestionComponent.DifficultyClass.EASY;
        return actions.OrderByDescending(k => k.Value).First().Key.action;
    }

    // Optional: Method to get all Q-values for debugging/analysis
    public Dictionary<(string, QuestionComponent.DifficultyClass), float> GetQTable()
    {
        return new Dictionary<(string, QuestionComponent.DifficultyClass), float>(Q);
    }

    // Optional: Parameter setters
    public void SetLearningRate(float newAlpha) => alpha = Mathf.Clamp(newAlpha, 0f, 1f);
    public void SetDiscountFactor(float newGamma) => gamma = Mathf.Clamp(newGamma, 0f, 1f);
}