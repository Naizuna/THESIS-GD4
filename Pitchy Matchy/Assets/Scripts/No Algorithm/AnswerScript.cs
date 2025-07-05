using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnswerScript : MonoBehaviour
{
    public int answerIndex;
    public QuizManager quizManager;

    void Start()
    {
        if (quizManager == null)
        {
            quizManager = FindObjectOfType<QuizManager>();
        }
    }

    public void OnSelectAnswer()
    {
       quizManager.SelectAnswer(answerIndex);
    }
}
