using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaitingPanel : MonoBehaviour
{
    [SerializeField] GameObject parentPanel;
    [SerializeField] GameObject shade;
    [SerializeField] TMP_Text correctIndicatorText;
    [SerializeField] TMP_Text correctAnswersText;
    [SerializeField] TMP_Text playerAnswersText;


    public void HideParentPanel()
    {
        UIUtils.HideUIComponents(parentPanel);
    }

    public void ShowParentPanel()
    {
        UIUtils.ShowUIComponents(parentPanel);
    }

    public void ExtractCurrentQuestionResult(QuestionComponent data)
    {
        QuestionComponent dataCopy = new QuestionComponent(data);
        playerAnswersText.text = dataCopy.ReturnPlayerAnswersAsString();
        correctAnswersText.text = dataCopy.ReturnCorrectAnswersAsString();

        if (dataCopy.isAnsweredCorrectly)
        {
            correctIndicatorText.text = "You are correct";
        }
        else
        {
            correctIndicatorText.text = "You are incorrect";
        }
    }
}
