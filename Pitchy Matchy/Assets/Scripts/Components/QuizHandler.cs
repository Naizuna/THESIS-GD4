using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    //quiz handler will bridge the PianoHandler (player inputs), question answering and etc.
public class QuizHandler : MonoBehaviour
{
    [Header("Questions Loaded(visible for testing)")]
    [SerializeField] public List<QuestionComponent> questionsToAnswer; //will be dynamically filled up

    private List<string> playerAnswers = new List<string>();

    public void ReceivePlayerAnswers(List<string> answers)
    {
        playerAnswers = answers;
        Debug.Log("QUIZMANAGER GETPLAYERANSWERS Debug: ");
        foreach (var item in playerAnswers)
        {
            Debug.Log(item);
        }
    }


}
