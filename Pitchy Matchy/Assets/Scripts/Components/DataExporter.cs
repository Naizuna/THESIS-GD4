using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class DataExporter
{
    public string filename { get; private set; }

    public void PlayerMetricWriteToCSV(string filename, PlayerMetric metricData)
    {
        this.filename = Application.dataPath + $"/csv/{filename}.csv";
        TextWriter textWriter = new StreamWriter(this.filename, false, Encoding.UTF8);

        metricData.CalculateTotalAccuracy();
        metricData.CalculateDifficultyAccuracy();
        int easyCorrect = metricData.GetCorrect(QuestionComponent.DifficultyClass.EASY);
        int easyTotal = metricData.GetTotal(QuestionComponent.DifficultyClass.EASY);
        int medCorrect = metricData.GetCorrect(QuestionComponent.DifficultyClass.MEDIUM);
        int medTotal = metricData.GetTotal(QuestionComponent.DifficultyClass.MEDIUM);
        int hardCorrect = metricData.GetCorrect(QuestionComponent.DifficultyClass.HARD);
        int hardTotal = metricData.GetTotal(QuestionComponent.DifficultyClass.HARD);
        double easyAccuracy = metricData.easyAccuracy;
        double mediumAccuracy = metricData.mediumAccuracy;
        double hardAccuracy = metricData.hardAccuracy;

        textWriter.WriteLine("PLAYER DATA");
        textWriter.WriteLine("EASY,MED,HARD");
        textWriter.WriteLine($"{easyCorrect}/{easyTotal}\t,{medCorrect}/{medTotal}\t,{hardCorrect}/{hardTotal}\t");
        textWriter.WriteLine("EASY_ACC,MED_ACC,HARD_ACC");
        textWriter.WriteLine($"{easyAccuracy},{mediumAccuracy},{hardAccuracy}");
        textWriter.WriteLine($"Total Accuracy: {metricData.totalAccuracy}");

        textWriter.Close();
    }   
}
