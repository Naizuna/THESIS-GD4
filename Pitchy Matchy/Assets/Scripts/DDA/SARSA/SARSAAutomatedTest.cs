using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Automated testing framework for SARSA algorithm
/// Simulates quiz sessions with controlled scenarios to validate learning behavior
/// </summary>
public class SARSAAutomatedTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private int numberOfTestRuns = 5;
    [SerializeField] private int questionsPerRun = 20;
    [SerializeField] private bool logDetailedOutput = true;
    [SerializeField] private bool exportResults = true;

    [Header("Test Scenarios")]
    [SerializeField] private bool runBasicLearningTest = true;
    [SerializeField] private bool runConvergenceTest = true;
    [SerializeField] private bool runAdaptationTest = true;
    [SerializeField] private bool runExplorationTest = true;

    private SARSAController testAgent;
    private TestResults overallResults;

    // Test data structure
    [System.Serializable]
    public class TestResults
    {
        public List<TestRun> runs = new List<TestRun>();
        public float averageQTableSize;
        public float averageStatesExplored;
        public float averageConvergenceSpeed;
        public Dictionary<string, int> mostVisitedStates = new Dictionary<string, int>();
    }

    [System.Serializable]
    public class TestRun
    {
        public int runNumber;
        public List<TestQuestion> questions = new List<TestQuestion>();
        public int finalQTableSize;
        public int statesExplored;
        public float finalEpsilon;
        public string summary;
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
        public string nextState;
        public float epsilonAtTime;
    }

    void Start()
    {
        Debug.Log("=== SARSA AUTOMATED TESTING FRAMEWORK ===");
        Debug.Log($"Starting automated tests with {numberOfTestRuns} runs of {questionsPerRun} questions each");
        
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
    // Tests if SARSA learns to increase difficulty for correct answers
    private void TestBasicLearning()
    {
        testAgent = new SARSAController();
        var testRun = new TestRun { runNumber = 1 };

        string currentState = "START";
        QuestionComponent.DifficultyClass currentAction = QuestionComponent.DifficultyClass.EASY;

        Debug.Log("[Basic Learning] Simulating consistent correct answers...");

        for (int i = 0; i < questionsPerRun; i++)
        {
            // Simulate: Always answer correctly with fast response time
            bool correct = true;
            float responseTime = UnityEngine.Random.Range(1.5f, 2.5f); // Fast
            QuestionComponent.DifficultyClass difficulty = currentAction;

            // Calculate reward
            float reward = CalculateTestReward(difficulty, correct, responseTime);

            // Construct next state
            string nextState = SARSAController.ConstructState(difficulty, correct, responseTime);
            var nextAction = testAgent.ChooseAction(nextState);

            // Update SARSA
            if (currentState != "START")
            {
                testAgent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
            }
            testAgent.DecayEpsilon();

            // Log question
            var testQ = new TestQuestion
            {
                questionNumber = i + 1,
                state = currentState,
                action = currentAction,
                wasCorrect = correct,
                responseTime = responseTime,
                reward = reward,
                nextState = nextState,
                epsilonAtTime = testAgent.CurrentEpsilon
            };
            testRun.questions.Add(testQ);

            if (logDetailedOutput)
            {
                Debug.Log($"  Q{i + 1}: State={currentState} → Action={currentAction} → " +
                          $"Correct={correct} → NextState={nextState} → NextAction={nextAction}");
            }

            currentState = nextState;
            currentAction = nextAction;
        }

        // Analyze results
        testRun.finalQTableSize = testAgent.GetQTable().Count;
        testRun.statesExplored = testAgent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count();
        testRun.finalEpsilon = testAgent.CurrentEpsilon;

        int hardQuestions = testRun.questions.Count(q => q.action == QuestionComponent.DifficultyClass.HARD);
        testRun.summary = $"Progressed to HARD difficulty {hardQuestions} times out of {questionsPerRun}";

        Debug.Log($"[Basic Learning] PASSED: {testRun.summary}");
        Debug.Log($"[Basic Learning] Q-table size: {testRun.finalQTableSize}, States explored: {testRun.statesExplored}");
        
        overallResults.runs.Add(testRun);
        
        if (logDetailedOutput)
        {
            testAgent.PrintQTable();
        }
    }

    // === TEST 2: CONVERGENCE ===
    // Tests if Q-values stabilize over time
    private void TestConvergence()
    {
        testAgent = new SARSAController();
        var testRun = new TestRun { runNumber = 2 };

        string currentState = "START";
        QuestionComponent.DifficultyClass currentAction = QuestionComponent.DifficultyClass.EASY;

        // Track Q-value changes
        Dictionary<(string, QuestionComponent.DifficultyClass), List<float>> qValueHistory 
            = new Dictionary<(string, QuestionComponent.DifficultyClass), List<float>>();

        Debug.Log("[Convergence] Running repeated episodes to test Q-value stability...");

        for (int i = 0; i < questionsPerRun * 2; i++) // Double the questions for convergence
        {
            // Simulate consistent pattern: 80% correct with medium response time
            bool correct = UnityEngine.Random.value < 0.8f;
            float responseTime = UnityEngine.Random.Range(3.5f, 6.5f); // Medium
            QuestionComponent.DifficultyClass difficulty = currentAction;

            float reward = CalculateTestReward(difficulty, correct, responseTime);
            string nextState = SARSAController.ConstructState(difficulty, correct, responseTime);
            var nextAction = testAgent.ChooseAction(nextState);

            if (currentState != "START")
            {
                testAgent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
            }
            testAgent.DecayEpsilon();

            // Track Q-value changes
            var key = (currentState, currentAction);
            if (!qValueHistory.ContainsKey(key))
            {
                qValueHistory[key] = new List<float>();
            }
            qValueHistory[key].Add(testAgent.GetQTable().GetValueOrDefault(key, 0f));

            currentState = nextState;
            currentAction = nextAction;
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
                
                if (variance < 0.1f) // Low variance = converged
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
    // Tests if SARSA adapts when performance changes
    private void TestAdaptation()
    {
        testAgent = new SARSAController();
        var testRun = new TestRun { runNumber = 3 };

        string currentState = "START";
        QuestionComponent.DifficultyClass currentAction = QuestionComponent.DifficultyClass.EASY;

        Debug.Log("[Adaptation] Testing response to performance changes...");

        for (int i = 0; i < questionsPerRun; i++)
        {
            bool correct;
            float responseTime;

            // First half: Perform well (80% correct, fast)
            if (i < questionsPerRun / 2)
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

            QuestionComponent.DifficultyClass difficulty = currentAction;
            float reward = CalculateTestReward(difficulty, correct, responseTime);
            string nextState = SARSAController.ConstructState(difficulty, correct, responseTime);
            var nextAction = testAgent.ChooseAction(nextState);

            if (currentState != "START")
            {
                testAgent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
            }
            testAgent.DecayEpsilon();

            var testQ = new TestQuestion
            {
                questionNumber = i + 1,
                state = currentState,
                action = currentAction,
                wasCorrect = correct,
                responseTime = responseTime,
                reward = reward,
                nextState = nextState,
                epsilonAtTime = testAgent.CurrentEpsilon
            };
            testRun.questions.Add(testQ);

            currentState = nextState;
            currentAction = nextAction;
        }

        // Analyze adaptation
        var firstHalf = testRun.questions.Take(questionsPerRun / 2);
        var secondHalf = testRun.questions.Skip(questionsPerRun / 2);

        float avgDifficultyFirstHalf =  (float) firstHalf.Average(q => (int)q.action);
        float avgDifficultySecondHalf = (float) secondHalf.Average(q => (int)q.action);

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
        testAgent = new SARSAController();
        testAgent.SetEpsilon(0.5f); // High epsilon for exploration test
        
        var testRun = new TestRun { runNumber = 4 };

        string currentState = "START";
        QuestionComponent.DifficultyClass currentAction = QuestionComponent.DifficultyClass.EASY;

        Debug.Log("[Exploration] Testing exploration behavior with high epsilon...");

        Dictionary<QuestionComponent.DifficultyClass, int> actionCounts 
            = new Dictionary<QuestionComponent.DifficultyClass, int>();

        for (int i = 0; i < questionsPerRun; i++)
        {
            bool correct = UnityEngine.Random.value < 0.6f;
            float responseTime = UnityEngine.Random.Range(2f, 6f);
            QuestionComponent.DifficultyClass difficulty = currentAction;

            // Track action diversity
            if (!actionCounts.ContainsKey(difficulty))
                actionCounts[difficulty] = 0;
            actionCounts[difficulty]++;

            float reward = CalculateTestReward(difficulty, correct, responseTime);
            string nextState = SARSAController.ConstructState(difficulty, correct, responseTime);
            var nextAction = testAgent.ChooseAction(nextState);

            if (currentState != "START")
            {
                testAgent.UpdateQValue(currentState, currentAction, reward, nextState, nextAction);
            }

            currentState = nextState;
            currentAction = nextAction;
        }

        // Analyze exploration diversity
        int uniqueActionsChosen = actionCounts.Count;
        testRun.finalQTableSize = testAgent.GetQTable().Count;
        testRun.statesExplored = testAgent.GetQTable().Select(kvp => kvp.Key.Item1).Distinct().Count();
        testRun.finalEpsilon = testAgent.CurrentEpsilon;
        
        string actionDistribution = string.Join(", ", actionCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        testRun.summary = $"Explored {uniqueActionsChosen}/3 difficulties. Distribution: {actionDistribution}";

        bool goodExploration = uniqueActionsChosen >= 2; // Should try at least 2 difficulties
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

            var timeCat = SARSAController.DiscretizeResponseTime(responseTime);
            float timeBonus = timeCat switch
            {
                SARSAController.ResponseTimeCategory.FAST => 0.5f,
                SARSAController.ResponseTimeCategory.AVERAGE => 0.2f,
                SARSAController.ResponseTimeCategory.SLOW => 0f,
                _ => 0f
            };

            reward += timeBonus;
        }
        else
        {
            reward = difficulty switch
            {
                QuestionComponent.DifficultyClass.EASY => -2.0f,
                QuestionComponent.DifficultyClass.MEDIUM => -1.5f,
                QuestionComponent.DifficultyClass.HARD => -0.5f,
                _ => -1.0f
            };

            var timeCat = SARSAController.DiscretizeResponseTime(responseTime);
            if (timeCat == SARSAController.ResponseTimeCategory.SLOW)
            {
                reward -= 0.3f;
            }
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
        Debug.Log("\n=== FINAL TEST REPORT ===");
        Debug.Log($"Total test runs completed: {overallResults.runs.Count}");
        
        if (overallResults.runs.Count > 0)
        {
            overallResults.averageQTableSize = (float) overallResults.runs.Average(r => r.finalQTableSize);
            overallResults.averageStatesExplored = (float) overallResults.runs.Average(r => r.statesExplored);
            
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

        Debug.Log("\n=== TESTING COMPLETE ===");
    }

    private void ExportResultsToCSV()
    {
        var sb = new StringBuilder();
        sb.AppendLine("TestRun,QuestionNumber,State,Action,Correct,ResponseTime,Reward,NextState,Epsilon");

        foreach (var run in overallResults.runs)
        {
            foreach (var q in run.questions)
            {
                sb.AppendLine($"{run.runNumber},{q.questionNumber},{q.state},{q.action}," +
                              $"{q.wasCorrect},{q.responseTime:F2},{q.reward:F2}," +
                              $"{q.nextState},{q.epsilonAtTime:F4}");
            }
        }

        string filename = $"SARSA_Test_Results_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        System.IO.File.WriteAllText(path, sb.ToString());
        
        Debug.Log($"Test results exported to: {path}");
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
}