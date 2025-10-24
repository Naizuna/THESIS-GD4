using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrentScoreUI : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] QuizController quizController;
    private PlayerMetric playerMetric;
    // Start is called before the first frame update
    void Start()
    {
        playerMetric = quizController.ctx.PlyrMetric;
    }

    // Update is called once per frame
    void Update()
    {
        quizController.ctx.UpdatePlayerMetrics();
        playerMetric.CalculateTotalAccuracy();
        float totalAccuracy = playerMetric.totalAccuracy * 100f;
        text.text =  totalAccuracy.ToString() + "%";
    }
}
