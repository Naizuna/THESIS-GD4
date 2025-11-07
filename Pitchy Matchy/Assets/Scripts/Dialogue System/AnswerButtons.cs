using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnswerButtons : MonoBehaviour
{
    [Header("Key Value")]
    public string keyValue; // Example: "C", "D#", "G"

    [Header("Visual Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    private bool isSelected;
    private Image image;
    private PianoHandler piano;
    private Color defaultColors;

    void Awake()
    {
        image = GetComponent<Image>();
        piano = FindObjectOfType<PianoHandler>(); // Or assign manually
        defaultColors = image.color;

        GetComponent<Button>().onClick.AddListener(ToggleSelection);
        UpdateVisual();
    }

    void ToggleSelection()
    {
        if (!isSelected)
        {
            // trying to select → only if less than 3 selected
            if (piano.GetAllPianoKeysPressed().Count >= 3)
            {
                Debug.Log("Cannot select more than 3.");
                return;
            }

            piano.AddKey(keyValue);
        }
        else
        {
            // deselect → remove from list
            piano.RemoveKey(keyValue);
        }

        isSelected = !isSelected;
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
            image.sprite = isSelected ? selectedSprite : normalSprite;
    }

    public void ShowAsRed()
    {
        image.color = new Color(255, 0, 0);
    }

    public void ShowAsGreen()
    {
        image.color = new Color(0, 255, 0);
    }

    public void ResetColors()
    {
        image.color = defaultColors;
    }
}
