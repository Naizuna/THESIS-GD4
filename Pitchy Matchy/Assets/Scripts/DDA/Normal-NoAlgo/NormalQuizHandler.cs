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

    public bool IsSessionFinished { get; set; } = false;

    public NormalQuizHandler(QuizContext context)
    {
        ctx = context;
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
        if (IsSessionFinished) return;

        ctx.PlayerAnswers = new List<string>(answers);
        ProcessAnswer();
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
        ctx.coroutineRunner.StartCoroutine(ProcessAnswerCoroutine());
    }

    private IEnumerator ProcessAnswerCoroutine()
    {
        Debug.Log("Coroutine started");
        
        var q = ctx.GetCurrentQuestion();
        Debug.Log($"Question null? {q == null}");
        
        if (q == null) yield break;
        
        Debug.Log("About to set player answers");
        q.playerAnswers = new List<string>(ctx.PlayerAnswers);
        
        Debug.Log("About to check answers");
        q.CheckAnswers();
        
        Debug.Log("About to get keys");
        ctx.keysHighlighter.GetTheKeys(new QuestionComponent(q));
        
        Debug.Log("pas1");
        ctx.keysHighlighter.HighlightAnsweredKeys();
        
        Debug.Log("About to wait");
        yield return new WaitForSeconds(ctx.keysHighlighter.speed * 2f);
        
        Debug.Log("pas2");
        
        if (q.isAnsweredCorrectly)
        {
            ctx.Player.PlayAttack();
            ctx.Enemy.TakeDamage(ctx.Player.GetAttackPower());
        }
        else
        {
            ctx.Enemy.PlayAttack();
            ctx.Player.TakeDamage(ctx.Enemy.GetAttackPower());
        }
        
        LoadNextQuestion();
    }
    
}
