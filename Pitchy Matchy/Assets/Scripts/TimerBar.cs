using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.U2D;

public class TimerBar : MonoBehaviour
{
    [SerializeField] QuizController quizController;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] Sprite[] timerBarSprite;

    private bool isFrozen = false;
    private float frozenResponseTime = 0f; // Store the time when frozen

    void Update()
    {
        if (!isFrozen)
        {
            UpdateTimerBar();
        }
        else
        {
            int spriteIndex = GetSpriteIndex(frozenResponseTime);
            sr.sprite = timerBarSprite[Mathf.Clamp(spriteIndex, 0, timerBarSprite.Length - 1)];
        }
    }

    void UpdateTimerBar()
    {
        //start from question start time (green)
        float startTime = quizController.ctx.questionStartTime; //e.g. 15.239 
        
        //get response time
        float responseTime = Time.time - startTime;

        // // Clamp to valid array indices (0-9)
        // int spriteIndex = (int)Mathf.Clamp(responseTime, 0, timerBarSprite.Length - 1);

        // sr.sprite = timerBarSprite[spriteIndex];
        int spriteIndex = GetSpriteIndex(responseTime);

        // Apply sprite safely
        sr.sprite = timerBarSprite[Mathf.Clamp(spriteIndex, 0, timerBarSprite.Length - 1)];
    }

    private int GetSpriteIndex(float t)
    {
        if (t < 5f)//FAST
        {
            // 0–5 sec → sprites 0–3 (green)
            return Mathf.FloorToInt(Mathf.Lerp(0, 3, t / 5f));
        }
        else if (t < 10f)//AVERAGE
        {
            // 5–10 sec → sprites 4–6 (yellow)
            return Mathf.FloorToInt(Mathf.Lerp(4, 6, (t - 5f) / 5f));
        }
        else//SLOW
        {
            // 10+ sec → sprites 7–9+ (red)
            return Mathf.Min(7 + Mathf.FloorToInt((t - 10f)), timerBarSprite.Length -1);
        }
    }

    public void FreezeTimer()
    {
        if (!isFrozen)
        {
            float startTime = quizController.ctx.questionStartTime;
            frozenResponseTime = Time.time - startTime;
            isFrozen = true;
            Debug.Log($"[TimerBar] Frozen at {frozenResponseTime:F2} seconds");
        }
    }

    public void UnfreezeTimer()
    {
        isFrozen = false;
        frozenResponseTime = 0f;
        Debug.Log("[TimerBar] Unfrozen - ready for next question");
    }

    public void ResetTimer()
    {
        UnfreezeTimer();
        sr.sprite = timerBarSprite[0];
        Debug.Log("[TimerBar] Reset to start");
    }
}