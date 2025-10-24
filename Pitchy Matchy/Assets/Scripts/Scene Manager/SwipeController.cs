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
    [SerializeField] Button previousButton, nextButton;
    [SerializeField] TMP_Text[] pageTexts;
    [SerializeField] string[] pageDescriptions;


    void Awake()
    {
        currentPage = 1;
        targetPos = levelPagesRect.localPosition;
        dragThreshold = Screen.width / 15;
        UpdateArrowButton();
        UpdatePageText();
    }

    private void UpdatePageText()
    {
        // Hide all text objects first
        foreach (TMP_Text text in pageTexts)
        {
            text.gameObject.SetActive(false);
        }
        
        // Show only the text for current page
        if (currentPage - 1 < pageTexts.Length)
        {
            pageTexts[currentPage - 1].gameObject.SetActive(true);
            
            // Update the text content if you have descriptions
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
    //Adjusts pages
    void MovePage()
    {
        levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        UpdateArrowButton();
        UpdatePageText(); 
    }

    //For Dragging the mouse on screen
    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }
        else MovePage();
    }

    public void UpdateArrowButton()
    {
        nextButton.interactable = true;
        previousButton.interactable = true;
        if (currentPage == 1) previousButton.interactable = false;
        else if(currentPage == maxPage) nextButton.interactable = false;
    }    
}
