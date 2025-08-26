using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PianoHandler : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] TMP_Text keyLabel;
    [Header("Quiz Handler")]
    [SerializeField] QuizHandler qh;
    [SerializeField] MonteCarloQuizHandler mc;
    private List<string> keys = new List<string>();

    public void UpdatePianoKeyPressed(string input)
    {
        if (keys.Count >= 3)
        {
            Debug.Log("Inputs full");
            return;
        }

        keys.Add(input);
        string txt = string.Empty;
        for (int i = 0; i < keys.Count; i++)
        {
            if (i == 0)
            {
                txt += keys[i];
                continue;
            }
            txt += $", {keys[i]}";
        }

        keyLabel.text = txt;
        Debug.Log(input);
    }

    public void SendPlayerAnswerToHandler()
    {
        mc.ReceivePlayerAnswersAndProcess(keys);
        Debug.Log("Sent player answers to QuizHandler");
    }
    public List<string> GetAllPianoKeysPressed()
    {
        return keys;
    }

    public void ClearAllKeys()
    {
        keys.Clear();
        Debug.Log("Answers Cleared");
        keyLabel.text = "None";
    }
}
