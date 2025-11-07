using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeysHighlighter : MonoBehaviour
{
    [Header("KEYS")]
    [SerializeField] List<GameObject> keysObj;
    Dictionary<string, AnswerButtons> keyDict = new Dictionary<string, AnswerButtons>();

    [Header("Highlight Speed")]
    [SerializeField] public float speed;

    private List<AnswerButtons> keysToHighlight = new List<AnswerButtons>();
    private List<IndividualPitch> keysFromQuestions = new List<IndividualPitch>();

    void Start()
    {
        foreach (GameObject obj in keysObj)
        {
            keyDict.Add(obj.GetComponent<AnswerButtons>().keyValue, obj.GetComponent<AnswerButtons>());
        }
    }

    public void GetTheKeys(QuestionComponent questionComponent)
    {
        QuestionComponent buffer = new QuestionComponent(questionComponent);
        keysFromQuestions = new List<IndividualPitch>(buffer.playerAnswersIndiv);
        List<IndividualPitch> keys = new List<IndividualPitch>(buffer.playerAnswersIndiv);
        foreach (IndividualPitch key in keys)
        {
            string name = key.keyName;
            keysToHighlight.Add(keyDict[name]);
        }
    }
    
    public void HighlightAnsweredKeys()
    {
        StartCoroutine(IEHighlightAnsweredKeys());
    }

    public IEnumerator IEHighlightAnsweredKeys()
    {
        foreach (IndividualPitch key in keysFromQuestions)
        {
            if (!keyDict.ContainsKey(key.keyName))
            {
                Debug.Log("Non-existent key");
                continue;
            }

            if (key.isAnsweredCorrectly)
            {
                keyDict[key.keyName].ShowAsGreen();
            }
            else
            {
                keyDict[key.keyName].ShowAsRed();
            }
            yield return new WaitForSeconds(speed);
        }
        
        foreach (IndividualPitch key in keysFromQuestions)
        {
            if (!keyDict.ContainsKey(key.keyName))
            {
                Debug.Log("Non-existent key");
                continue;
            }

            keyDict[key.keyName].ResetColors();
        }
    }

}
