using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMetric
{
    string levelName;
    float totalAccuracy;
    List<QuestionComponent> questionsAnswered;

    public void WriteToFile()
    {
        //todo
    }

    public void TestPrint()
    {
        Debug.Log("Level: " + levelName);
        Debug.Log("Accuracy: " + totalAccuracy);

        for (int i = 0; i < questionsAnswered.Count; i++)
        {
            Debug.Log("Question " + i + "\n" + 
            "Correct Pitches: " + questionsAnswered[i].ReturnCorrectAnswersAsString() + "\n" +
            "Player Answers: " + questionsAnswered[i].ReturnPlayerAnswersAsString());
        }
    }

    public void CalculateTotalAccuracy()
    {
        float totalQuestions = questionsAnswered.Count;
        float totalCorrect = 0;

        foreach (var question in questionsAnswered)
        {
            if (question.isAnsweredCorrectly)
                totalCorrect++;
        }

        totalAccuracy = totalCorrect / totalQuestions;
    }

    public void SetQuestionsAnswered(List<QuestionComponent> questions)
    {
        questionsAnswered = new List<QuestionComponent>(questions);
    }
}
