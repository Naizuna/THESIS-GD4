using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IQuizMode
{
    void StartQuiz(QuizHandler handler);
    void ReceiveAnswers(QuizHandler handler, List<string> answers);
    void LoadNextQuestion(QuizHandler handler);
}
