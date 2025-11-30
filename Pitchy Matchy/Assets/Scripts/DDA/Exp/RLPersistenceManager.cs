using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages persistence of RL agent Q-tables across scenes using JSON files.
/// Singleton pattern ensures one instance persists throughout the game.
/// </summary>
public class RLPersistenceManager : MonoBehaviour
{
    private static RLPersistenceManager _instance;
    public static RLPersistenceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("RLPersistenceManager");
                _instance = go.AddComponent<RLPersistenceManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // File paths
    private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "RLData");
    private static string MCC_FilePath => Path.Combine(SaveDirectory, "mcc_agent.json");
    private static string SARSA_FilePath => Path.Combine(SaveDirectory, "sarsa_agent.json");

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure save directory exists
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
            Debug.Log($"[Persistence] Created save directory: {SaveDirectory}");
        }
    }

    #region Monte Carlo Control Persistence

    /// <summary>
    /// Saves Monte Carlo agent's Q-table, visit counts, and epsilon to file
    /// </summary>
    public void SaveMonteCarloAgent(MonteCarloAgent agent)
    {
        try
        {
            var entries = agent.GetAllQTableEntries();

            // Serialize Q-table
            QTableData data = new QTableData
            {
                entries = new List<QEntry>(),
                epsilon = agent.CurrentEpsilon,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            foreach (var entry in entries)
            {
                data.entries.Add(new QEntry
                {
                    state = entry.State,
                    action = (int)entry.Action,
                    qValue = entry.QValue,
                    visits = entry.Visits,
                    returnCount = entry.ReturnCount
                });
            }

            string json = JsonUtility.ToJson(data, true); // true = pretty print
            File.WriteAllText(MCC_FilePath, json);

            Debug.Log($"[Persistence] Saved MCC agent to: {MCC_FilePath}\n" +
                      $"  Entries: {data.entries.Count} | ε: {agent.CurrentEpsilon:F3}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to save MCC agent: {e.Message}");
        }
    }

    /// <summary>
    /// Loads saved data into a Monte Carlo agent
    /// </summary>
    public bool LoadMonteCarloAgent(MonteCarloAgent agent)
    {
        try
        {
            if (!File.Exists(MCC_FilePath))
            {
                Debug.Log("[Persistence] No saved MCC data found - starting fresh");
                return false;
            }

            string json = File.ReadAllText(MCC_FilePath);
            QTableData data = JsonUtility.FromJson<QTableData>(json);

            if (data == null || data.entries == null)
            {
                Debug.LogWarning("[Persistence] Invalid MCC data format");
                return false;
            }

            // Load Q-table using reflection (since Q-table is private)
            var qTableField = typeof(MonteCarloAgent).GetField("Q",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var returnsField = typeof(MonteCarloAgent).GetField("returns",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var visitCountsField = typeof(MonteCarloAgent).GetField("visitCounts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (qTableField != null && returnsField != null && visitCountsField != null)
            {
                var Q = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();
                var returns = new Dictionary<(string, QuestionComponent.DifficultyClass), List<float>>();
                var visitCounts = new Dictionary<(string, QuestionComponent.DifficultyClass), int>();

                foreach (var entry in data.entries)
                {
                    var key = (entry.state, (QuestionComponent.DifficultyClass)entry.action);
                    Q[key] = entry.qValue;
                    visitCounts[key] = entry.visits;

                    // Initialize returns list (actual return values aren't saved, just counts)
                    returns[key] = new List<float>();
                }

                qTableField.SetValue(agent, Q);
                returnsField.SetValue(agent, returns);
                visitCountsField.SetValue(agent, visitCounts);
            }

            // Load epsilon
            agent.SetEpsilon(data.epsilon);

            Debug.Log($"[Persistence] Loaded MCC agent from: {MCC_FilePath}\n" +
                      $"  Entries: {data.entries.Count} | ε: {agent.CurrentEpsilon:F3} | Saved: {data.timestamp}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to load MCC agent: {e.Message}");
            return false;
        }
    }

    #endregion

    #region SARSA Persistence

    /// <summary>
    /// Saves SARSA agent's Q-table, visit counts, and epsilon to file
    /// </summary>
    public void SaveSARSAAgent(SARSAController agent)
    {
        try
        {
            var qTable = agent.GetQTable();

            // Serialize Q-table
            QTableData data = new QTableData
            {
                entries = new List<QEntry>(),
                epsilon = agent.CurrentEpsilon,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Get visit counts via reflection
            var visitCountsField = typeof(SARSAController).GetField("visitCounts", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Dictionary<(string, QuestionComponent.DifficultyClass), int> visitCounts = null;
            
            if (visitCountsField != null)
            {
                visitCounts = visitCountsField.GetValue(agent) as Dictionary<(string, QuestionComponent.DifficultyClass), int>;
            }

            // Iterate through Q-table entries correctly
            foreach (var kvp in qTable)
            {
                string state = kvp.Key.Item1;  // First element of tuple
                QuestionComponent.DifficultyClass action = kvp.Key.Item2;  // Second element of tuple
                
                int visits = 0;
                if (visitCounts != null && visitCounts.ContainsKey(kvp.Key))
                {
                    visits = visitCounts[kvp.Key];
                }

                data.entries.Add(new QEntry
                {
                    state = state,
                    action = (int)action,
                    qValue = kvp.Value,
                    visits = visits
                });
            }

            string json = JsonUtility.ToJson(data, true); // true = pretty print
            File.WriteAllText(SARSA_FilePath, json);

            Debug.Log($"[Persistence] Saved SARSA agent to: {SARSA_FilePath}\n" +
                      $"  Entries: {data.entries.Count} | ε: {agent.CurrentEpsilon:F3}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to save SARSA agent: {e.Message}");
        }
    }

    /// <summary>
    /// Loads saved data into a SARSA agent
    /// </summary>
    public bool LoadSARSAAgent(SARSAController agent)
    {
        try
        {
            if (!File.Exists(SARSA_FilePath))
            {
                Debug.Log("[Persistence] No saved SARSA data found - starting fresh");
                return false;
            }

            string json = File.ReadAllText(SARSA_FilePath);
            QTableData data = JsonUtility.FromJson<QTableData>(json);

            if (data == null || data.entries == null)
            {
                Debug.LogWarning("[Persistence] Invalid SARSA data format");
                return false;
            }

            // Load Q-table using reflection
            var qTableField = typeof(SARSAController).GetField("Q",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var visitCountsField = typeof(SARSAController).GetField("visitCounts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (qTableField != null && visitCountsField != null)
            {
                var Q = new Dictionary<(string, QuestionComponent.DifficultyClass), float>();
                var visitCounts = new Dictionary<(string, QuestionComponent.DifficultyClass), int>();

                foreach (var entry in data.entries)
                {
                    var key = (entry.state, (QuestionComponent.DifficultyClass)entry.action);
                    Q[key] = entry.qValue;
                    visitCounts[key] = entry.visits;
                }

                qTableField.SetValue(agent, Q);
                visitCountsField.SetValue(agent, visitCounts);
            }

            // Load epsilon
            agent.SetEpsilon(data.epsilon);

            Debug.Log($"[Persistence] Loaded SARSA agent from: {SARSA_FilePath}\n" +
                      $"  Entries: {data.entries.Count} | ε: {agent.CurrentEpsilon:F3} | Saved: {data.timestamp}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to load SARSA agent: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Clear/Reset Functions

    /// <summary>
    /// Clears all saved RL data (deletes files)
    /// </summary>
    public void ClearAllSavedData()
    {
        try
        {
            if (File.Exists(MCC_FilePath))
                File.Delete(MCC_FilePath);
            
            if (File.Exists(SARSA_FilePath))
                File.Delete(SARSA_FilePath);
            
            Debug.Log("[Persistence] Cleared all saved RL data");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to clear data: {e.Message}");
        }
    }

    /// <summary>
    /// Clears only Monte Carlo saved data
    /// </summary>
    public void ClearMonteCarloData()
    {
        try
        {
            if (File.Exists(MCC_FilePath))
                File.Delete(MCC_FilePath);
            
            Debug.Log("[Persistence] Cleared MCC saved data");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to clear MCC data: {e.Message}");
        }
    }

    /// <summary>
    /// Clears only SARSA saved data
    /// </summary>
    public void ClearSARSAData()
    {
        try
        {
            if (File.Exists(SARSA_FilePath))
                File.Delete(SARSA_FilePath);
            
            Debug.Log("[Persistence] Cleared SARSA saved data");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Persistence] Failed to clear SARSA data: {e.Message}");
        }
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Returns the full path to the save directory
    /// </summary>
    public string GetSaveDirectory()
    {
        return SaveDirectory;
    }

    /// <summary>
    /// Opens the save directory in file explorer
    /// </summary>
    public void OpenSaveDirectory()
    {
        if (Directory.Exists(SaveDirectory))
        {
            Application.OpenURL("file://" + SaveDirectory);
            Debug.Log($"[Persistence] Opened save directory: {SaveDirectory}");
        }
        else
        {
            Debug.LogWarning("[Persistence] Save directory doesn't exist yet");
        }
    }

    /// <summary>
    /// Checks if any saved data exists
    /// </summary>
    public bool HasAnySavedData()
    {
        return File.Exists(MCC_FilePath) || File.Exists(SARSA_FilePath);
    }

    #endregion

    #region Helper Classes

    [Serializable]
    private class QTableData
    {
        public List<QEntry> entries;
        public float epsilon;
        public string timestamp;
    }

    [Serializable]
    private class QEntry
    {
        public string state;
        public int action; // DifficultyClass as int
        public float qValue;
        public int visits;
        public int returnCount;
    }

    #endregion
}