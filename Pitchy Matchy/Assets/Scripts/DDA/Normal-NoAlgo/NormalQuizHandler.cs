using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class NormalQuizHandler : MonoBehaviour, IQuizHandler
{
    private readonly QuizContext ctx;
    private Coroutine runningCoroutine;

    public bool IsSessionFinished { get; set; } = false;

    public NormalQuizHandler(QuizContext context)
    {
        ctx = context;
    }

    public enum ResponseTimeCategory
    {
        FAST,      
        AVERAGE,    
        SLOW       
    }

    public void StartQuiz()
    {
        ctx.CurrQuestionIndex = 0;
        ctx.QuestionsToAnswer.Clear();
        // load a fixed number of random questions
        for (int i = 0; i < ctx.NumberOfQuestions; i++)
        {
            var randDiff = (QuestionComponent.DifficultyClass)UnityEngine.Random.Range(
                0, System.Enum.GetValues(typeof(QuestionComponent.DifficultyClass)).Length);
            ctx.AddQuestion(ctx.Bank.GetQuestionFromBank(randDiff));
        }
        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();
    }

    public void Update()
    {
        Debug.Log("Current: " + ctx.QuestionsToAnswer.Count);
    }

    public void ReceivePlayerAnswers(List<string> answers)
    {
        ctx.RecordResponseTime();
        if (IsSessionFinished) return;

        ctx.PlayerAnswers = new List<string>(answers);
        ProcessAnswer();
    }

    public static ResponseTimeCategory DiscretizeResponseTime(float seconds)
    {
        if (seconds <= 5f)
            return ResponseTimeCategory.FAST;
        else if (seconds <= 10f)
            return ResponseTimeCategory.AVERAGE;
        else
            return ResponseTimeCategory.SLOW;
    }

    public void LoadNextQuestion()
    {
        if (IsSessionFinished) return;

        ctx.CurrQuestionIndex++;
        ctx.PlayerAnswers.Clear();

        if (ctx.CurrQuestionIndex >= ctx.NumberOfQuestions)
        {
            IsSessionFinished = true;
            return;
        }

        ctx.UpdateQuestionText();
        ctx.PlayCurrentQuestionPitches();
    }

    private void ProcessAnswer()
    {
        if (runningCoroutine != null)
        {
            ctx.coroutineRunner.StopCoroutine(runningCoroutine);
        }
        runningCoroutine = ctx.coroutineRunner.StartCoroutine(ProcessAnswerCoroutine());
    }

    private IEnumerator ProcessAnswerCoroutine()
    {
        Debug.Log("Coroutine started");
        ctx.enablePlayerInput(false);

        var q = ctx.GetCurrentQuestion();

        if (q == null) yield break;


        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        var responseTime = ctx.ResponseTimes[ctx.ResponseTimes.Count - 1];
        string timeCat = DiscretizeResponseTime(responseTime).ToString();


        q.CheckAnswers();

        ctx.ShowCorrectAnswers();
        ctx.keysHighlighter.GetTheKeys(new QuestionComponent(q));
        ctx.PlayCurrentQuestionPitches();
        ctx.keysHighlighter.HighlightAnsweredKeys();

        Debug.Log("About to wait");
        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);

        if (q.isAnsweredCorrectly)
        {
            ctx.Player.PlayAttack();
            ctx.Enemy.TakeDamage(ctx.Player.GetAttackPower());
            ctx.correctStreak++;
            ctx.ShowResponseTime(timeCat);
        }
        else
        {
            ctx.Enemy.PlayAttack();
            ctx.Player.TakeDamage(ctx.Enemy.GetAttackPower());
            ctx.correctStreak = 0;
            ctx.TogglePlayerImmunity(false);
        }

        ctx.CheckCorrectStreak();

        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        ctx.ShowResponseTime("");
        LoadNextQuestion();
        ctx.enablePlayerInput(true);
    }
    
}
