using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMetric
{
    public string levelName { get; set; }
    public float totalAccuracy { get; private set; }
    List<QuestionComponent> questionsAnswered;
    DataExporter dataExporter = new DataExporter();

    public float easyAccuracy { get; private set; }
    public float mediumAccuracy { get; private set; }
    public float hardAccuracy { get; private set; }

    public void WriteToFile(string filename)
    {
        if (questionsAnswered == null)
        {
            throw new InvalidOperationException("List is null.");
        }
        if (questionsAnswered.Count == 0)
        {
            throw new InvalidOperationException("List is empty.");
        }

        string realFileName = levelName + "_" + filename;
        dataExporter.PlayerMetricWriteToCSV(realFileName, this);
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

    public float GetAccuracy(QuestionComponent.DifficultyClass difficulty)
    {
        var (total, correct) = CountForDifficulty(difficulty);
        if (total == 0) return 0.0f;
        return (float) System.Math.Round((float) correct / total, 2);
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

        totalAccuracy = (float) System.Math.Round(totalCorrect / totalQuestions, 2);
    }

    public void CalculateDifficultyAccuracy()
    {
        easyAccuracy = GetAccuracy(QuestionComponent.DifficultyClass.EASY);
        mediumAccuracy = GetAccuracy(QuestionComponent.DifficultyClass.MEDIUM);
        hardAccuracy = GetAccuracy(QuestionComponent.DifficultyClass.HARD);
    }

    public void SetQuestionsAnswered(List<QuestionComponent> questions)
    {
        questionsAnswered = new List<QuestionComponent>(questions);
    }
}
