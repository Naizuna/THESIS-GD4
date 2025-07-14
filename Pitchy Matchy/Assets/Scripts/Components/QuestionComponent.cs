using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestionComponent
{
    public enum DifficultyClass { EASY, MEDIUM, HARD }
    public string questionText;
    public DifficultyClass questionDifficulty;
    public List<AudioClip> soundClips;
    public List<string> correctAnswers;
    public List<string> playerAnswers;

    //if player answered correctly or wrong
    public bool isAnsweredCorrectly = false; 

    //check if the question has been answered either wrong or correctly
    public bool hasBeenAnswered = false;

    public QuestionComponent()
    {
        
    }

    // CONSTRUCTOR FOR COPY-PURPOSES
    public QuestionComponent(QuestionComponent other)
    {
        questionText = other.questionText;
        questionDifficulty = other.questionDifficulty;
        soundClips = new List<AudioClip>(other.soundClips);
        correctAnswers = new List<string>(other.correctAnswers);
        playerAnswers = new List<string>(other.playerAnswers);
        isAnsweredCorrectly = other.isAnsweredCorrectly;
    }

    public void ResetQuestion()
    {
        playerAnswers.Clear();
        hasBeenAnswered = false;
        isAnsweredCorrectly = false;
    }
}

