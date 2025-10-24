using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonteCarloControlQuizMode : IQuizMode
{
    public void StartQuiz(QuizHandler handler)
    {
        handler.CurrQuestionIndex = 0;
        handler.Start_MCC();
    }

    public void ReceiveAnswers(QuizHandler handler, List<string> answers)
    {
        handler.ReceivePlayerAnswersAndProcess_MCC(answers);
    }

    public void LoadNextQuestion(QuizHandler handler)
    {
        handler.LoadNextQuestion_MCC();
    }
}
