using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnswerButton : MonoBehaviour
{
    [Header("Set the piano note label here")]
    public string noteValue;

    [HideInInspector] public DialogueController controller;

    [Header("Visuals")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    private bool isSelected;
    private Button button;
    private Image image;

    void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        button.onClick.AddListener(ToggleSelection);
        UpdateVisual();
    }

    void ToggleSelection()
    {
        if (!isSelected)
        {
            if (!controller.CanSelectMoreAnswers())
                return;

            isSelected = true;
            controller.AddSelectedAnswer(noteValue);
        }
        else
        {
            isSelected = false;
            controller.RemoveSelectedAnswer(noteValue);
        }
        UpdateVisual();
    }

    public void Deselect()
    {
        isSelected = false;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (image != null)
        {
            image.sprite = isSelected ? selectedSprite : normalSprite;
        }
    }
}
