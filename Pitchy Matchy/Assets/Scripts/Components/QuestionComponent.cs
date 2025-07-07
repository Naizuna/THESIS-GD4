using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestionComponent
{
    public enum DifficultyClass {EASY, MEDIUM, HARD}
    public string questionText;
    public DifficultyClass questionDifficulty;
    public List<AudioClip> soundClips;
    public List<string> correctAnswers;
    public List<string> playerAnswers;
    public bool isAnsweredCorrectly;
}

