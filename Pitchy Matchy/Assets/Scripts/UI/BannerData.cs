using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BannerData : MonoBehaviour
{
    [SerializeField] TMP_Text totals;
    [SerializeField] TMP_Text accuracies;
    [SerializeField] TMP_Text score;

    private PlayerMetric plyrmetric;

    //change accuracy total later
    public void SetResultsText(QuizContext ctx)
    {
        plyrmetric = ctx.PlyrMetric;

        totals.text = $"easy: {plyrmetric.GetCorrect(QuestionComponent.DifficultyClass.EASY)}/{plyrmetric.GetTotal(QuestionComponent.DifficultyClass.EASY)}\n" +
        $"medium: {plyrmetric.GetCorrect(QuestionComponent.DifficultyClass.MEDIUM)}/{plyrmetric.GetTotal(QuestionComponent.DifficultyClass.MEDIUM)}\n" +
        $"hard: {plyrmetric.GetCorrect(QuestionComponent.DifficultyClass.HARD)}/{plyrmetric.GetTotal(QuestionComponent.DifficultyClass.HARD)}\n";

        accuracies.text = $"{plyrmetric.easyAccuracy * 100f}%\n" +
         $"{plyrmetric.mediumAccuracy * 100f}%\n" +
         $"{plyrmetric.hardAccuracy * 100f}%\n";

        score.text = $"score: {plyrmetric.totalAccuracy * 100f}%";
    }
}
