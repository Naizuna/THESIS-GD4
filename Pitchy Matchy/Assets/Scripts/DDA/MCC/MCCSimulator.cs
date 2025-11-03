using System.Collections.Generic;
using UnityEngine;

public class MCCSimulator : MonoBehaviour
{
    void Start()
    {
        RunAllSimulations();
    }

    void RunAllSimulations()
    {
        // Perfect, average, and struggling simulated players
        System.Func<QuestionComponent.DifficultyClass, (bool, float)> perfectPlayer = (d) => (true, 1f);
        System.Func<QuestionComponent.DifficultyClass, (bool, float)> averagePlayer = (d) =>
        {
            if (d == QuestionComponent.DifficultyClass.EASY) return (true, 3f);
            if (d == QuestionComponent.DifficultyClass.MEDIUM) return (Random.value < 0.7f, 5f);
            return (Random.value < 0.4f, 8f);
        };
        System.Func<QuestionComponent.DifficultyClass, (bool, float)> strugglingPlayer = (d) =>
        {
            if (d == QuestionComponent.DifficultyClass.EASY) return (Random.value < 0.6f, 6f);
            if (d == QuestionComponent.DifficultyClass.MEDIUM) return (Random.value < 0.2f, 9f);
            return (false, 12f);
        };

        Debug.Log("===== MCC SIMULATION START =====");
        RunSimulation(new MonteCarloAgent(), perfectPlayer, "Perfect Player");
        RunSimulation(new MonteCarloAgent(), averagePlayer, "Average Player");
        RunSimulation(new MonteCarloAgent(), strugglingPlayer, "Struggling Player");
        Debug.Log("===== MCC SIMULATION END =====");
    }

    void RunSimulation(MonteCarloAgent agent,
                       System.Func<QuestionComponent.DifficultyClass, (bool, float)> playerSim,
                       string label)
    {
        int episodes = 5;
        int questionsPerEpisode = 6;

        for (int ep = 0; ep < episodes; ep++)
        {
            var episode = new List<(string, QuestionComponent.DifficultyClass, float)>();
            string state = "START";

            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = agent.ChooseAction(state);
                var (isCorrect, time) = playerSim(action);

                // Reward logic (mirrors your quiz version)
                int difficultyPoints = action == QuestionComponent.DifficultyClass.EASY ? 1 :
                                       action == QuestionComponent.DifficultyClass.MEDIUM ? 2 : 3;

                float baseReward = isCorrect ? difficultyPoints : -difficultyPoints;
                float timeBonus = time <= 5f ? 0.5f : (time <= 10f ? 0.2f : 0f);
                float reward = baseReward + timeBonus;

                episode.Add((state, action, reward));
            }

            agent.UpdatePolicy(episode);
            agent.DecayEpsilon();
        }

        // Display final Q-values summary
        Debug.Log($"--- Results for {label} ---");
        var actions = System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass));
        foreach (QuestionComponent.DifficultyClass a in actions)
        {
            var key = ("START", a);
            float qValue = agent.GetQValue(key);
            Debug.Log($"{label} | {a}: {qValue:F2}");
        }

        var best = agent.GetBestAction("START");
        Debug.Log($"{label} best learned difficulty: {best}");
        Debug.Log("--------------------------------");
    }
}
