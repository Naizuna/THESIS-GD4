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

    void Awake()
    {
        image = GetComponent<Image>();
        piano = FindObjectOfType<PianoHandler>(); // Or assign manually

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
}
