using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalQuizMode : IQuizMode
{
    public void StartQuiz(QuizHandler handler)
    {
        handler.CurrQuestionIndex = 0;
        handler.LoadRandomQuestions(handler.NumberOfQuestions);
        handler.UpdateQuestionText();
    }

    public void ReceiveAnswers(QuizHandler handler, List<string> answers)
    {
        handler.PlayerAnswers = answers;
        handler.ProcessAnswer();
    }

    public void LoadNextQuestion(QuizHandler handler)
    {
        handler.WP.HideParentPanel();
        handler.CurrQuestionIndex++;
        handler.PlayerAnswers.Clear();
        handler.UpdateQuestionText();
    }
}
