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

    private (int total, int correct) CountForDifficulty(QuestionComponent.DifficultyClass difficulty)
    {
        int total = 0;
        int correct = 0;

        foreach (var q in questionsAnswered)
        {
            if (q.questionDifficulty == difficulty)
            {
                total++;
                if (q.isAnsweredCorrectly) correct++;
            }
        }

        return (total, correct);
    }

    public double GetAccuracy(QuestionComponent.DifficultyClass difficulty)
    {
        var (total, correct) = CountForDifficulty(difficulty);
        if (total == 0) return 0.0;
        return (double)correct / total;
    }

    public int GetCorrect(QuestionComponent.DifficultyClass difficulty)
    {
        var (_, correct) = CountForDifficulty(difficulty);
        return correct;
    }

    public int GetTotal(QuestionComponent.DifficultyClass difficulty)
    {
        var (total, _) = CountForDifficulty(difficulty);
        return total;
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
