using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//quiz handler will bridge the PianoHandler (player inputs), question answering and etc.
public class QuizHandler : MonoBehaviour
{
    [Header("Questions Loaded(visible for testing)")]
    [SerializeField] public List<QuestionComponent> questionsToAnswer; //will be dynamically filled up

    [Header("Question Bank")]
    [SerializeField] private QuestionsBank bank;
    [Header("Number of Questions")]
    [SerializeField] private int numberOfQuestions;

    private List<string> playerAnswers = new List<string>();

    public void Start()
    {
        LoadRandomQuestions(numberOfQuestions);
    }

    public void ReceivePlayerAnswers(List<string> answers)
    {
        playerAnswers = answers;
        Debug.Log("QUIZMANAGER GETPLAYERANSWERS Debug: ");
        foreach (var item in playerAnswers)
        {
            Debug.Log(item);
        }
    }

    public void LoadRandomQuestions(int numberOfQuestions)
    {

        for (int i = 0; i < numberOfQuestions; i++)
        {
            QuestionComponent.DifficultyClass randDifficulty = (QuestionComponent.DifficultyClass) UnityEngine.Random.Range(
                0,
                System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length - 1
            );

            questionsToAnswer.Add(bank.GetQuestionFromBank(randDifficulty));
        }
    }


}
