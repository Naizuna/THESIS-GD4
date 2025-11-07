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
    [SerializeField] private Sprite highlightSprite;

    [Header("Sound")]
    [SerializeField] private AudioClip noteSound;

    private bool isSelected;
    private bool isHighlighted = false;
    private Button button;
    private Image image;

    void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();

        // Clear previous listeners and add a single click event
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        UpdateVisual();
    }

    private void OnClick()
    {
        if (!button.interactable)
            return;

        if (!isSelected && !controller.CanSelectMoreAnswers())
        {
            // do nothing (no sound, no selection)
            return;
        }

        // Play piano note when active
        if (noteSound != null)
            SoundManager.Instance.PlaySFX(noteSound);

        // Handle selection logic
        ToggleSelection();
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

    public void HighlightKey(bool value)
    {
        isHighlighted = value;
        UpdateVisual();
    }

    public void Deselect()
    {
        isSelected = false;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (image == null) return;

        if (isHighlighted)
            image.sprite = highlightSprite;
        else
            image.sprite = isSelected ? selectedSprite : normalSprite;
    }
}
