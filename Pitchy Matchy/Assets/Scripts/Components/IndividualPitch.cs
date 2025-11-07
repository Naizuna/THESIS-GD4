using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class IndividualPitch
{
    public string keyName { get; set; }
    public bool isAnsweredCorrectly { get; set; }

    public IndividualPitch(string key, bool correct)
    {
        keyName = key;
        isAnsweredCorrectly = correct;
    }
}
