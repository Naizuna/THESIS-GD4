using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonteCarloAgent
{
    private float epsilon = 0.1f;
    private float minEpsilon = 0.01f;
    private float decayRate = 0.99f;
    
    public void DecayEpsilon()
    {
        epsilon = Mathf.Max(minEpsilon, epsilon * decayRate);
    }
    // Optional property for logging
    public float CurrentEpsilon => epsilon;

    // Q[state, action] = value
    private Dictionary<(string state, QuestionComponent.DifficultyClass action), float> Q
        = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();

    private Dictionary<(string state, QuestionComponent.DifficultyClass action), List<float>> returns
        = new Dictionary<(string, QuestionComponent.DifficultyClass), List<float>>();

    private System.Random rng = new System.Random();

    public QuestionComponent.DifficultyClass ChooseAction(string state)
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

    public void UpdatePolicy(List<(string state, QuestionComponent.DifficultyClass action, float reward)> episode)
    {
        HashSet<(string, QuestionComponent.DifficultyClass)> visited = new HashSet<(string, QuestionComponent.DifficultyClass)>();
        float G = 0; // return
        float gamma = 1f;

        for (int t = episode.Count - 1; t >= 0; t--)
        {
            var step = episode[t];
            G = step.reward + gamma * G;

            if (!visited.Contains((step.state, step.action)))
            {
                if (!returns.ContainsKey((step.state, step.action)))
                    returns[(step.state, step.action)] = new List<float>();

                returns[(step.state, step.action)].Add(G);
                Q[(step.state, step.action)] = Average(returns[(step.state, step.action)]);
                visited.Add((step.state, step.action));
            }
        }
    }

    private float Average(List<float> list) => list.Count == 0 ? 0f : (float)list.Average();

    private bool HasState(string state) => Q.Keys.Any(k => k.state == state);

    private QuestionComponent.DifficultyClass GetBestAction(string state)
    {
        var actions = Q.Where(k => k.Key.state == state);
        if (!actions.Any()) return QuestionComponent.DifficultyClass.EASY;
        return actions.OrderByDescending(k => k.Value).First().Key.action;
    }
}
