using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum QuizMode {Normal, MonteCarloControl, Sarsa}
public interface IQuizHandler
{
    void StartQuiz();
    void Update();

    //called when player submits answers
    void ReceivePlayerAnswers(List<string> answers);
    void LoadNextQuestion();
    bool IsSessionFinished { get; set; }
}
