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

    public List<QuestionComponent> GetQuestionsFromBank(QuestionComponent.DifficultyClass difficulty, int count)
    {
        List<QuestionComponent> questions = difficulty switch
        {
            QuestionComponent.DifficultyClass.EASY => new List<QuestionComponent>(easyQuestionsPool),
            QuestionComponent.DifficultyClass.MEDIUM => new List<QuestionComponent>(mediumQuestionsPool),
            QuestionComponent.DifficultyClass.HARD => new List<QuestionComponent>(hardQuestionsPool),
            _ => null
        };

        if (questions == null || questions.Count == 0) return new List<QuestionComponent>();

        count = Mathf.Min(count, questions.Count);

        List<QuestionComponent> result = new List<QuestionComponent>(count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, questions.Count);
            result.Add(new QuestionComponent(questions[index]));
        }

        return result;
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
