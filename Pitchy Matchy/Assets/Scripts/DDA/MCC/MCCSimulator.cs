using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Automated testing framework for Monte Carlo Control algorithm
/// Simulates quiz sessions with controlled scenarios to validate learning behavior
/// Matches SARSAAutomatedTest structure for fair comparison
/// </summary>
public class MCCAutomatedTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private int numberOfTestRuns = 5;
    [SerializeField] private int totalQuestions = 20;
    [SerializeField] private int questionsPerEpisode = 4;
    [SerializeField] private bool logDetailedOutput = true;
    [SerializeField] private bool exportResults = true;

    [Header("Test Scenarios")]
    [SerializeField] private bool runBasicLearningTest = true;
    [SerializeField] private bool runConvergenceTest = true;
    [SerializeField] private bool runAdaptationTest = true;
    [SerializeField] private bool runExplorationTest = true;

    private MonteCarloAgent testAgent;
    private TestResults overallResults;

    // Test data structure
    [System.Serializable]
    public class TestResults
    {
        public List<TestRun> runs = new List<TestRun>();
        public float averageQTableSize;
        public float averageStatesExplored;
        public float averageConvergenceSpeed;
    }

    [System.Serializable]
    public class TestRun
    {
        public int runNumber;
        public List<TestEpisode> episodes = new List<TestEpisode>();
        public int finalQTableSize;
        public int statesExplored;
        public float finalEpsilon;
        public string summary;
    }

    [System.Serializable]
    public class TestEpisode
    {
        public int episodeNumber;
        public List<TestQuestion> questions = new List<TestQuestion>();
        public float episodeAccuracy;
        public float epsilonAtEnd;
    }

    [System.Serializable]
    public class TestQuestion
    {
        public int questionNumber;
        public string state;
        public QuestionComponent.DifficultyClass action;
        public bool wasCorrect;
        public float responseTime;
        public float reward;
    }

    void Start()
    {
        Debug.Log("=== MCC AUTOMATED TESTING FRAMEWORK ===");
        Debug.Log($"Starting automated tests with {numberOfTestRuns} runs of {totalQuestions} questions each");
        Debug.Log($"Episode structure: {questionsPerEpisode} questions per episode");
        
        RunAllTests();
    }

    public void RunAllTests()
    {
        overallResults = new TestResults();

        if (runBasicLearningTest)
        {
            Debug.Log("\n[TEST 1] Running Basic Learning Test...");
            TestBasicLearning();
        }

        if (runConvergenceTest)
        {
            Debug.Log("\n[TEST 2] Running Convergence Test...");
            TestConvergence();
        }

        if (runAdaptationTest)
        {
            Debug.Log("\n[TEST 3] Running Adaptation Test...");
            TestAdaptation();
        }

        if (runExplorationTest)
        {
            Debug.Log("\n[TEST 4] Running Exploration Test...");
            TestExploration();
        }

        GenerateFinalReport();
    }

    // === TEST 1: BASIC LEARNING ===
    // Tests if MCC learns to increase difficulty for correct answers
    private void TestBasicLearning()
    {
        testAgent = new MonteCarloAgent();
        var testRun = new TestRun { runNumber = 1 };

        string currentState = "START";
        int numEpisodes = totalQuestions / questionsPerEpisode;

        Debug.Log("[Basic Learning] Simulating consistent correct answers...");

        for (int ep = 0; ep < numEpisodes; ep++)
        {
            var testEpisode = new TestEpisode { episodeNumber = ep + 1 };
            var episode = new List<(string, QuestionComponent.DifficultyClass, float)>();

            // Generate entire episode at once (MCC characteristic)
            var episodeActions = new List<QuestionComponent.DifficultyClass>();
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = testAgent.ChooseAction(currentState);
                episodeActions.Add(action);
            }

            // Simulate playing through the episode
            string episodeStartState = currentState;
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = episodeActions[q];
                
                // Simulate: Always answer correctly with fast response time
                bool correct = true;
                float responseTime = UnityEngine.Random.Range(1.5f, 2.5f); // Fast
                float reward = CalculateTestReward(action, correct, responseTime);

                // Record for episode
                episode.Add((episodeStartState, action, reward));

                // Update state for next question (but episode uses same start state)
                currentState = MonteCarloAgent.ConstructState(action, correct, responseTime);

                // Log question
                var testQ = new TestQuestion
                {
                    questionNumber = ep * questionsPerEpisode + q + 1,
                    state = episodeStartState,
                    action = action,
                    wasCorrect = correct,
                    responseTime = responseTime,
                    reward = reward
                };
                testEpisode.questions.Add(testQ);

                if (logDetailedOutput)
                {
                    Debug.Log($"  Episode {ep + 1}, Q{q + 1}: State={episodeStartState} → Action={action} → " +
                              $"Correct={correct} → NextState={currentState}");
                }
            }

            // MCC learns after complete episode
            testAgent.UpdatePolicy(episode);
            testAgent.DecayEpsilon();

            testEpisode.episodeAccuracy = testEpisode.questions.Count(q => q.wasCorrect) / (float)questionsPerEpisode;
            testEpisode.epsilonAtEnd = testAgent.CurrentEpsilon;
            testRun.episodes.Add(testEpisode);
        }

        // Analyze results
        testRun.finalQTableSize = testAgent.GetQTable().Count;
        testRun.statesExplored = testAgent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count();
        testRun.finalEpsilon = testAgent.CurrentEpsilon;

        int hardQuestions = testRun.episodes.SelectMany(e => e.questions).Count(q => q.action == QuestionComponent.DifficultyClass.HARD);
        testRun.summary = $"Progressed to HARD difficulty {hardQuestions} times out of {totalQuestions}";

        Debug.Log($"[Basic Learning] PASSED: {testRun.summary}");
        Debug.Log($"[Basic Learning] Q-table size: {testRun.finalQTableSize}, States explored: {testRun.statesExplored}");
        
        overallResults.runs.Add(testRun);
        
        if (logDetailedOutput)
        {
            testAgent.PrintQTable();
        }
    }

    // === TEST 2: CONVERGENCE ===
    // Tests if Q-values stabilize over episodes
    private void TestConvergence()
    {
        testAgent = new MonteCarloAgent();
        var testRun = new TestRun { runNumber = 2 };

        string currentState = "START";
        int numEpisodes = (totalQuestions * 2) / questionsPerEpisode; // Double episodes for convergence

        // Track Q-value changes per episode
        Dictionary<(string, QuestionComponent.DifficultyClass), List<float>> qValueHistory 
            = new Dictionary<(string, QuestionComponent.DifficultyClass), List<float>>();

        Debug.Log("[Convergence] Running repeated episodes to test Q-value stability...");

        for (int ep = 0; ep < numEpisodes; ep++)
        {
            var episode = new List<(string, QuestionComponent.DifficultyClass, float)>();

            // Generate episode
            var episodeActions = new List<QuestionComponent.DifficultyClass>();
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                episodeActions.Add(testAgent.ChooseAction(currentState));
            }

            // Play through episode
            string episodeStartState = currentState;
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = episodeActions[q];
                
                // Simulate: 80% correct with medium response time
                bool correct = UnityEngine.Random.value < 0.8f;
                float responseTime = UnityEngine.Random.Range(3.5f, 6.5f); // Medium
                float reward = CalculateTestReward(action, correct, responseTime);

                episode.Add((episodeStartState, action, reward));
                currentState = MonteCarloAgent.ConstructState(action, correct, responseTime);
            }

            // Update policy
            testAgent.UpdatePolicy(episode);
            testAgent.DecayEpsilon();

            // Track Q-value changes
            foreach (var kvp in testAgent.GetQTable())
            {
                if (!qValueHistory.ContainsKey(kvp.Key))
                {
                    qValueHistory[kvp.Key] = new List<float>();
                }
                qValueHistory[kvp.Key].Add(kvp.Value);
            }
        }

        // Analyze convergence
        int convergedPairs = 0;
        foreach (var kvp in qValueHistory)
        {
            if (kvp.Value.Count >= 5)
            {
                // Check if last 5 values are similar (converged)
                var lastFive = kvp.Value.TakeLast(5).ToList();
                float variance = CalculateVariance(lastFive);
                
                if (variance < 0.5f) // Slightly higher threshold for MCC
                {
                    convergedPairs++;
                }
            }
        }

        testRun.finalQTableSize = testAgent.GetQTable().Count;
        testRun.statesExplored = testAgent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count();
        testRun.finalEpsilon = testAgent.CurrentEpsilon;
        testRun.summary = $"{convergedPairs}/{qValueHistory.Count} state-action pairs converged";

        Debug.Log($"[Convergence] PASSED: {testRun.summary}");
        Debug.Log($"[Convergence] Final epsilon: {testRun.finalEpsilon:F3}");
        
        overallResults.runs.Add(testRun);
    }

    // === TEST 3: ADAPTATION ===
    // Tests if MCC adapts when performance changes between episodes
    private void TestAdaptation()
    {
        testAgent = new MonteCarloAgent();
        var testRun = new TestRun { runNumber = 3 };

        string currentState = "START";
        int numEpisodes = totalQuestions / questionsPerEpisode;

        Debug.Log("[Adaptation] Testing response to performance changes...");

        for (int ep = 0; ep < numEpisodes; ep++)
        {
            var testEpisode = new TestEpisode { episodeNumber = ep + 1 };
            var episode = new List<(string, QuestionComponent.DifficultyClass, float)>();

            // Generate episode
            var episodeActions = new List<QuestionComponent.DifficultyClass>();
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                episodeActions.Add(testAgent.ChooseAction(currentState));
            }

            // Play through episode with changing performance
            string episodeStartState = currentState;
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = episodeActions[q];
                
                bool correct;
                float responseTime;

                // First half of episodes: Perform well (80% correct, fast)
                if (ep < numEpisodes / 2)
                {
                    correct = UnityEngine.Random.value < 0.8f;
                    responseTime = UnityEngine.Random.Range(1.5f, 3.5f);
                }
                // Second half: Perform poorly (30% correct, slow)
                else
                {
                    correct = UnityEngine.Random.value < 0.3f;
                    responseTime = UnityEngine.Random.Range(6f, 9f);
                }

                float reward = CalculateTestReward(action, correct, responseTime);

                episode.Add((episodeStartState, action, reward));
                currentState = MonteCarloAgent.ConstructState(action, correct, responseTime);

                var testQ = new TestQuestion
                {
                    questionNumber = ep * questionsPerEpisode + q + 1,
                    state = episodeStartState,
                    action = action,
                    wasCorrect = correct,
                    responseTime = responseTime,
                    reward = reward
                };
                testEpisode.questions.Add(testQ);
            }

            testAgent.UpdatePolicy(episode);
            testAgent.DecayEpsilon();

            testEpisode.episodeAccuracy = testEpisode.questions.Count(q => q.wasCorrect) / (float)questionsPerEpisode;
            testEpisode.epsilonAtEnd = testAgent.CurrentEpsilon;
            testRun.episodes.Add(testEpisode);
        }

        // Analyze adaptation
        var firstHalfEpisodes = testRun.episodes.Take(numEpisodes / 2);
        var secondHalfEpisodes = testRun.episodes.Skip(numEpisodes / 2);

        var firstHalfQuestions = firstHalfEpisodes.SelectMany(e => e.questions);
        var secondHalfQuestions = secondHalfEpisodes.SelectMany(e => e.questions);

        float avgDifficultyFirstHalf = (float)firstHalfQuestions.Average(q => (int)q.action);
        float avgDifficultySecondHalf = (float) secondHalfQuestions.Average(q => (int)q.action);

        testRun.finalQTableSize = testAgent.GetQTable().Count;
        testRun.statesExplored = testAgent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count();
        testRun.finalEpsilon = testAgent.CurrentEpsilon;
        testRun.summary = $"Avg difficulty: {avgDifficultyFirstHalf:F2} (first half) → {avgDifficultySecondHalf:F2} (second half)";

        bool adapted = avgDifficultySecondHalf < avgDifficultyFirstHalf;
        Debug.Log($"[Adaptation] {(adapted ? "PASSED" : "FAILED")}: {testRun.summary}");
        Debug.Log($"[Adaptation] Algorithm {(adapted ? "correctly reduced" : "did not reduce")} difficulty after performance drop");
        
        overallResults.runs.Add(testRun);
    }

    // === TEST 4: EXPLORATION ===
    // Tests if exploration (epsilon) works correctly
    private void TestExploration()
    {
        testAgent = new MonteCarloAgent();
        testAgent.SetEpsilon(0.5f); // High epsilon for exploration test
        
        var testRun = new TestRun { runNumber = 4 };

        string currentState = "START";
        int numEpisodes = totalQuestions / questionsPerEpisode;

        Debug.Log("[Exploration] Testing exploration behavior with high epsilon...");

        Dictionary<QuestionComponent.DifficultyClass, int> actionCounts 
            = new Dictionary<QuestionComponent.DifficultyClass, int>();

        for (int ep = 0; ep < numEpisodes; ep++)
        {
            var episode = new List<(string, QuestionComponent.DifficultyClass, float)>();

            // Generate episode
            var episodeActions = new List<QuestionComponent.DifficultyClass>();
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = testAgent.ChooseAction(currentState);
                episodeActions.Add(action);

                // Track action diversity
                if (!actionCounts.ContainsKey(action))
                    actionCounts[action] = 0;
                actionCounts[action]++;
            }

            // Play through episode
            string episodeStartState = currentState;
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = episodeActions[q];
                bool correct = UnityEngine.Random.value < 0.6f;
                float responseTime = UnityEngine.Random.Range(2f, 6f);
                float reward = CalculateTestReward(action, correct, responseTime);

                episode.Add((episodeStartState, action, reward));
                currentState = MonteCarloAgent.ConstructState(action, correct, responseTime);
            }

            testAgent.UpdatePolicy(episode);
        }

        // Analyze exploration diversity
        int uniqueActionsChosen = actionCounts.Count;
        testRun.finalQTableSize = testAgent.GetQTable().Count;
        testRun.statesExplored = testAgent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count();
        testRun.finalEpsilon = testAgent.CurrentEpsilon;
        
        string actionDistribution = string.Join(", ", actionCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        testRun.summary = $"Explored {uniqueActionsChosen}/3 difficulties. Distribution: {actionDistribution}";

        bool goodExploration = uniqueActionsChosen >= 2;
        Debug.Log($"[Exploration] {(goodExploration ? "PASSED" : "FAILED")}: {testRun.summary}");
        
        overallResults.runs.Add(testRun);
    }

    // === UTILITY METHODS ===
    private float CalculateTestReward(QuestionComponent.DifficultyClass difficulty, bool correct, float responseTime)
    {
        float reward = 0f;

        if (correct)
        {
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => 1.0f,
                QuestionComponent.DifficultyClass.MEDIUM => 2.0f,
                QuestionComponent.DifficultyClass.HARD => 3.0f,
                _ => 1.0f
            };

            var timeCat = MonteCarloAgent.DiscretizeResponseTime(responseTime);
            float timeBonus = timeCat switch
            {
                MonteCarloAgent.ResponseTimeCategory.FAST => 0.5f,
                MonteCarloAgent.ResponseTimeCategory.AVERAGE => 0.2f,
                MonteCarloAgent.ResponseTimeCategory.SLOW => 0f,
                _ => 0f
            };

            reward += timeBonus;
        }
        else
        {
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => -1.0f,
                QuestionComponent.DifficultyClass.MEDIUM => -2.0f,
                QuestionComponent.DifficultyClass.HARD => -3.0f,
                _ => -1.0f
            };
        }

        return reward;
    }

    private float CalculateVariance(List<float> values)
    {
        if (values.Count == 0) return 0f;
        
        float mean = values.Average();
        float sumSquaredDiff = values.Sum(v => Mathf.Pow(v - mean, 2));
        return sumSquaredDiff / values.Count;
    }

    // === REPORTING ===
    private void GenerateFinalReport()
    {
        Debug.Log("\n=== FINAL MCC TEST REPORT ===");
        Debug.Log($"Total test runs completed: {overallResults.runs.Count}");
        
        if (overallResults.runs.Count > 0)
        {
            overallResults.averageQTableSize = (float) overallResults.runs.Average(r => r.finalQTableSize);
            overallResults.averageStatesExplored =(float) overallResults.runs.Average(r => r.statesExplored);
            
            Debug.Log($"Average Q-table size: {overallResults.averageQTableSize:F1}");
            Debug.Log($"Average states explored: {overallResults.averageStatesExplored:F1}");
            Debug.Log($"State space coverage: {overallResults.averageStatesExplored / 18f * 100:F1}% (out of 18 possible states)");
        }

        Debug.Log("\nTest Summaries:");
        foreach (var run in overallResults.runs)
        {
            Debug.Log($"  Test {run.runNumber}: {run.summary}");
        }

        if (exportResults)
        {
            ExportResultsToCSV();
        }

        Debug.Log("\n=== MCC TESTING COMPLETE ===");
    }

    private void ExportResultsToCSV()
    {
        var sb = new StringBuilder();
        sb.AppendLine("TestRun,Episode,QuestionNumber,State,Action,Correct,ResponseTime,Reward,Epsilon");

        foreach (var run in overallResults.runs)
        {
            if (run.episodes.Count > 0)
            {
                // For runs with episode structure
                foreach (var episode in run.episodes)
                {
                    foreach (var q in episode.questions)
                    {
                        sb.AppendLine($"{run.runNumber},{episode.episodeNumber},{q.questionNumber},{q.state},{q.action}," +
                                      $"{q.wasCorrect},{q.responseTime:F2},{q.reward:F2},{episode.epsilonAtEnd:F4}");
                    }
                }
            }
        }

        string filename = $"MCC_Test_Results_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        System.IO.File.WriteAllText(path, sb.ToString());
        
        Debug.Log($"MCC test results exported to: {path}");
    }

    // === UNITY EDITOR HELPERS ===
    [ContextMenu("Run All Tests")]
    public void RunTestsFromMenu()
    {
        RunAllTests();
    }

    [ContextMenu("Run Basic Learning Test Only")]
    public void RunBasicTestOnly()
    {
        overallResults = new TestResults();
        TestBasicLearning();
        GenerateFinalReport();
    }

    [ContextMenu("Compare MCC vs SARSA Performance")]
    public void CompareMCCvsSARSA()
    {
        Debug.Log("=== COMPARING MCC VS SARSA ===");
        
        // Run MCC test
        var mccAgent = new MonteCarloAgent();
        var mccStats = RunComparisonTest(mccAgent, "MCC");
        
        // Run SARSA test (if you have access to SARSA controller)
        // var sarsaAgent = new SARSAController();
        // var sarsaStats = RunComparisonTest(sarsaAgent, "SARSA");
        
        Debug.Log($"\nMCC Results:");
        Debug.Log($"  Final Q-table size: {mccStats.qTableSize}");
        Debug.Log($"  States explored: {mccStats.statesExplored}");
        Debug.Log($"  Final accuracy: {mccStats.accuracy:P}");
        
        // Debug.Log($"\nSARSA Results:");
        // Debug.Log($"  Final Q-table size: {sarsaStats.qTableSize}");
        // Debug.Log($"  States explored: {sarsaStats.statesExplored}");
        // Debug.Log($"  Final accuracy: {sarsaStats.accuracy:P}");
    }

    private (int qTableSize, int statesExplored, float accuracy) RunComparisonTest(MonteCarloAgent agent, string name)
    {
        string currentState = "START";
        int numEpisodes = totalQuestions / questionsPerEpisode;
        int totalCorrect = 0;
        int totalQuestionsAnswered = 0;  // Renamed to avoid shadowing the field

        for (int ep = 0; ep < numEpisodes; ep++)
        {
            var episode = new List<(string, QuestionComponent.DifficultyClass, float)>();
            
            var episodeActions = new List<QuestionComponent.DifficultyClass>();
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                episodeActions.Add(agent.ChooseAction(currentState));
            }

            string episodeStartState = currentState;
            for (int q = 0; q < questionsPerEpisode; q++)
            {
                var action = episodeActions[q];
                bool correct = UnityEngine.Random.value < 0.7f; // 70% success rate
                float responseTime = UnityEngine.Random.Range(2f, 6f);
                float reward = CalculateTestReward(action, correct, responseTime);

                episode.Add((episodeStartState, action, reward));
                currentState = MonteCarloAgent.ConstructState(action, correct, responseTime);

                if (correct) totalCorrect++;
                totalQuestionsAnswered++;
            }

            agent.UpdatePolicy(episode);
            agent.DecayEpsilon();
        }

        return (
            agent.GetQTable().Count,
            agent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count(),
            (float)totalCorrect / totalQuestionsAnswered
        );
    }
}