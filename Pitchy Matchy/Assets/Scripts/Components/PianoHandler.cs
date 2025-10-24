using System.Collections.Generic;
using UnityEngine;

public class PianoHandler : MonoBehaviour
{
    [Header("Quiz Controller")]
    [SerializeField] QuizController qController;

    private List<string> keys = new List<string>();

    public void AddKey(string input)
    {
        if (keys.Count >= 3)
        {
            Debug.Log("Limit reached (3 keys).");
            return;
        }

        keys.Add(input);
        Debug.Log("Key Added: " + input);
    }

    public void RemoveKey(string input)
    {
        if (keys.Contains(input))
        {
            keys.Remove(input);
            Debug.Log("Key Removed: " + input);
        }
    }

    public List<string> GetAllPianoKeysPressed()
    {
        return keys;
    }

    public void SendPlayerAnswerToHandler()
    {
        qController.OnPlayerSubmitAnswers(new List<string>(keys));
        Debug.Log("Sent player answers to QuizHandler");
        ClearAllKeys();
    }

    public void ClearAllKeys()
    {
        keys.Clear();

        // Deselect all visual buttons
        foreach (var btn in FindObjectsOfType<AnswerButtons>())
            btn.Deselect();

        Debug.Log("Keys Cleared");
    }
}
