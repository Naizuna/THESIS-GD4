using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreensPanel : MonoBehaviour
{
    [SerializeField] GameObject parentPanel;
    [SerializeField] TMP_Text textObj;
    [SerializeField] TMP_Text resultsObj;
    [SerializeField] string winText;
    [SerializeField] string loseText;
    [SerializeField] string resultsText;

    public void HideParentPanel()
    {
        UIUtils.HideUIComponents(parentPanel);
    }

    public void ShowParentPanel()
    {
        UIUtils.ShowUIComponents(parentPanel);
    }

    //change accuracy total later
    public void SetResultsText(QuizContext ctx)
    {
        PlayerMetric plyrmetric = ctx.PlyrMetric;
        resultsObj.text =
         "Results:\n" +
         $"Easy: {plyrmetric.GetCorrect(QuestionComponent.DifficultyClass.EASY)}/{plyrmetric.GetTotal(QuestionComponent.DifficultyClass.EASY)}\n" +
         $"Medium: {plyrmetric.GetCorrect(QuestionComponent.DifficultyClass.MEDIUM)}/{plyrmetric.GetTotal(QuestionComponent.DifficultyClass.MEDIUM)}\n" +
         $"Hard: {plyrmetric.GetCorrect(QuestionComponent.DifficultyClass.HARD)}/{plyrmetric.GetTotal(QuestionComponent.DifficultyClass.HARD)}\n" +
         $"Easy Accuracy: {plyrmetric.easyAccuracy}\n" +
         $"Medium Accuracy: {plyrmetric.mediumAccuracy}\n" +
         $"Hard Accuracy: {plyrmetric.hardAccuracy}\n" ;
    }
    public void SetWinScreen()
    {
        textObj.text = winText;
    }

    public void SetLoseScreen()
    {
        textObj.text = loseText;
    }
}
