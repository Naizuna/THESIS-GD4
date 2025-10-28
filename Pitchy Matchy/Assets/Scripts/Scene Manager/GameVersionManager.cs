using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameVersionManager : MonoBehaviour
{
    public enum VersionType { Normal, SARSA, MCC }
    public VersionType SelectedVersion;

    public static GameVersionManager Instance { get; private set; }

    // NEW: Track if the player already selected a version
    public bool HasChosenVersion { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetVersion(int v)
    {
        SelectedVersion = (VersionType)v;
        HasChosenVersion = true; // Mark version as chosen
    }

    // Optional: if you ever want a reset button in main menu later
    public void ResetVersionSelection()
    {
        HasChosenVersion = false;
    }
}
