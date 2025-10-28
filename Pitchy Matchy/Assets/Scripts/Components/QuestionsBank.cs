using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionsBank : MonoBehaviour
{
    [SerializeField] private List<QuestionComponent> availableQuestionsPool;
    [SerializeField] private List<QuestionComponent> easyQuestionsPool;
    [SerializeField] private List<QuestionComponent> mediumQuestionsPool;
    [SerializeField] private List<QuestionComponent> hardQuestionsPool;

    void Awake()
    {
        if (availableQuestionsPool == null) availableQuestionsPool = new List<QuestionComponent>();
        if (easyQuestionsPool      == null) easyQuestionsPool      = new List<QuestionComponent>();
        if (mediumQuestionsPool    == null) mediumQuestionsPool    = new List<QuestionComponent>();
        if (hardQuestionsPool      == null) hardQuestionsPool      = new List<QuestionComponent>();
        SortIntoDifficulties();
    }

    public QuestionComponent GetQuestionFromBank(QuestionComponent.DifficultyClass difficulty)
    {
        QuestionComponent question;
        int n = 0;
        switch (difficulty)
        {
            case QuestionComponent.DifficultyClass.EASY:
                //random question from easy list
                n = easyQuestionsPool.Count;
                question = new QuestionComponent(easyQuestionsPool[Random.Range(0, n)]);
                break;

            case QuestionComponent.DifficultyClass.MEDIUM:
                //random question from medium list
                n = mediumQuestionsPool.Count;
                question = new QuestionComponent(mediumQuestionsPool[Random.Range(0, n)]);
                break;

            case QuestionComponent.DifficultyClass.HARD:
                //random question from hard list
                n = hardQuestionsPool.Count;
                question = new QuestionComponent(hardQuestionsPool[Random.Range(0, n)]);
                break;

            default:
                question = null;
                break;
        }

        return question;
    }

    public QuestionComponent GetQuestionsFromBank (QuestionComponent.DifficultyClass difficulty)
    {
        QuestionComponent TryGet(List<QuestionComponent> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            return new QuestionComponent(pool[Random.Range(0, pool.Count)]);
        }

        QuestionComponent q = difficulty switch
        {
            QuestionComponent.DifficultyClass.EASY =>
                TryGet(easyQuestionsPool) ?? TryGet(mediumQuestionsPool) ?? TryGet(hardQuestionsPool),

            QuestionComponent.DifficultyClass.MEDIUM =>
                TryGet(mediumQuestionsPool) ?? TryGet(easyQuestionsPool) ?? TryGet(hardQuestionsPool),

            QuestionComponent.DifficultyClass.HARD =>
                TryGet(hardQuestionsPool) ?? TryGet(mediumQuestionsPool) ?? TryGet(easyQuestionsPool),

            _ => null
        };

        if (q == null)
            Debug.LogWarning("No questions available in any difficulty!");

        return q;
    }

    private void SortIntoDifficulties()
    {
        foreach (var question in availableQuestionsPool)
        {
            switch (question.questionDifficulty)
            {
                case QuestionComponent.DifficultyClass.EASY:
                    easyQuestionsPool.Add(question);
                    break;

                case QuestionComponent.DifficultyClass.MEDIUM:
                    mediumQuestionsPool.Add(question);
                    break;

                case QuestionComponent.DifficultyClass.HARD:
                    hardQuestionsPool.Add(question);
                    break;

                default:
                    break;
            }
        }
    }
    //add methods for retrieving blocks of questions with difficulty filters
}
