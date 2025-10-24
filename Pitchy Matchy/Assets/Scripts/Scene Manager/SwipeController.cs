using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [SerializeField] int maxPage;
    int currentPage;
    Vector3 targetPos;
    [SerializeField] Vector3 pageStep;
    [SerializeField] RectTransform levelPagesRect;
    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;
    float dragThreshold;

    // NEW: Arrays to allow different buttons per page
    [SerializeField] private Button[] previousButtons;
    [SerializeField] private Button[] nextButtons;

    [SerializeField] TMP_Text[] pageTexts;
    [SerializeField] string[] pageDescriptions;

    void Awake()
    {
        currentPage = 1;
        targetPos = levelPagesRect.localPosition;
        dragThreshold = Screen.width / 15;
        UpdateButtonStates();
        UpdatePageText();
    }

    private void UpdatePageText()
    {
        // Hide all text objects first
        foreach (TMP_Text text in pageTexts)
            text.gameObject.SetActive(false);

        // Show only the text for current page
        if (currentPage - 1 < pageTexts.Length)
        {
            pageTexts[currentPage - 1].gameObject.SetActive(true);

            // Update the text content if descriptions exist
            if (currentPage - 1 < pageDescriptions.Length && !string.IsNullOrEmpty(pageDescriptions[currentPage - 1]))
            {
                pageTexts[currentPage - 1].text = pageDescriptions[currentPage - 1];
            }
        }
    }

    public void Next()
    {
        if (currentPage < maxPage)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
        }
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
    }

    void MovePage()
    {
        levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        UpdateButtonStates();
        UpdatePageText();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }
        else MovePage();
    }

    // NEW: Controls which buttons show per page
    public void UpdateButtonStates()
    {
        // Hide ALL previous buttons
        foreach (Button btn in previousButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        // Hide ALL next buttons
        foreach (Button btn in nextButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        int index = currentPage - 1;

        // Show the correct previous button
        if (index < previousButtons.Length && previousButtons[index] != null)
        {
            previousButtons[index].gameObject.SetActive(true);
            previousButtons[index].interactable = currentPage > 1;
        }

        // Show the correct next button
        if (index < nextButtons.Length && nextButtons[index] != null)
        {
            nextButtons[index].gameObject.SetActive(true);
            nextButtons[index].interactable = currentPage < maxPage;
        }
    }
}
