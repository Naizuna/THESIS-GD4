using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DialogueLine
{
    [TextArea] public string text;
    [TextArea] public string wrongAnswerText;
    public List<AudioClip> pitchSounds = new();
    public string pitchLabel;
    public bool showRepeatButton;
    public bool showContinueButton;
    public bool allowPlayerInput = true;
    public bool waitForAudioToFinishBeforeButtons = false;

    public List<string> correctAnswer = new();
    [HideInInspector] public bool isTypingFinished = false;

    public bool showSprite;
    public Sprite spriteToShow;

    public bool CheckAnswer(List <string> playerAnswers)
    {
        if (playerAnswers.Count != correctAnswer.Count)
        {
            return false;
        }

        var playerSet = new HashSet<string>(playerAnswers);
        var correctSet = new HashSet<string>(correctAnswer);
        return playerSet.SetEquals(correctSet);
    }
}
