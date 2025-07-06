using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PianoHandler : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] TMP_Text keyLabel;
    private string key;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdatePianoKeyPressed(string key)
    {
        this.key = key;
        keyLabel.text = this.key;
        Debug.Log(this.key);
    }

    public string GetPianoKeyPressed()
    {
        return this.key;
    }
}
