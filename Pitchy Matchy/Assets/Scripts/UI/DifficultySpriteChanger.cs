using UnityEngine;
using UnityEngine.UI;

public class DifficultySpriteChanger : MonoBehaviour
{
    [Header("Target UI element to change")]
    [SerializeField] private Image targetImage;

    [Header("Difficulty Sprites")]
    [SerializeField] private Sprite easySprite;
    [SerializeField] private Sprite mediumSprite;
    [SerializeField] private Sprite hardSprite;

    public void ApplyDifficulty(QuestionComponent.DifficultyClass difficulty)
    {
        switch (difficulty)
        {
            case QuestionComponent.DifficultyClass.EASY:
                targetImage.sprite = easySprite;
                break;
            case QuestionComponent.DifficultyClass.MEDIUM:
                targetImage.sprite = mediumSprite;
                break;
            case QuestionComponent.DifficultyClass.HARD:
                targetImage.sprite = hardSprite;
                break;
        }
    }
}
