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
    public AnswersComponent test;
    public List<string> correctAnswers;
    public List<string> playerAnswers;
    [SerializeField] public List<IndividualPitch> playerAnswersIndiv = new List<IndividualPitch>();

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
        playerAnswersIndiv = new List<IndividualPitch>(other.playerAnswersIndiv);
    }

    public void ResetQuestion()
    {
        playerAnswers.Clear();
        playerAnswersIndiv.Clear();
        hasBeenAnswered = false;
        isAnsweredCorrectly = false;
    }

    public void CheckAnswers()
    {
        IndividualizedChecking();
        //we just assume that incomplete answers r wrong fr
        if (playerAnswers.Count != correctAnswers.Count)
        {
            isAnsweredCorrectly = false;
            hasBeenAnswered = true;
            return;
        }

        for (int i = 0; i < correctAnswers.Count; i++)
        {
            if (playerAnswers[i] != correctAnswers[i])
            {
                isAnsweredCorrectly = false;
                hasBeenAnswered = true;
                return;
            }
        }

        isAnsweredCorrectly = true;
        hasBeenAnswered = true;
    }

    public void IndividualizedChecking()
    {
        playerAnswersIndiv.Clear();
        for (int i = 0; i < correctAnswers.Count; i++)
        {
            if (playerAnswers[i] != correctAnswers[i])
            {
                playerAnswersIndiv.Add(new IndividualPitch(playerAnswers[i], false));
            }
            else
            {
                playerAnswersIndiv.Add(new IndividualPitch(playerAnswers[i], true));
            }
        }
    }

    public string ReturnPlayerAnswersAsString()
    {
        return string.Join(", ", playerAnswers);
    }

    public string ReturnCorrectAnswersAsString()
    {
        return string.Join(", ", correctAnswers);
    }

    public int GetNumberOfPitchesToAnswer()
    {
        return correctAnswers.Count;
    }

    public List<AudioClip> GetAudioClips()
    {
        return soundClips;
    }
}

