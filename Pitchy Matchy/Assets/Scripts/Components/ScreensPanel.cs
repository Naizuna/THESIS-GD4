using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreensPanel : MonoBehaviour
{
    [SerializeField] GameObject winBanner;
    [SerializeField] GameObject loseBanner;
    [SerializeField] string resultsText;

    void Awake()
    {
        // Use Awake instead of Start for initialization
        // Ensure banners are hidden before any other script tries to show them
        if (winBanner != null)
        {
            winBanner.SetActive(false);
        }
        
        if (loseBanner != null)
        {
            loseBanner.SetActive(false);
        }
    }

    public void SetWinScreen(QuizContext ctx)
    {
        if (winBanner == null)
        {
            Debug.LogError("Win banner is not assigned!");
            return;
        }

        BannerData bannerData = winBanner.GetComponent<BannerData>();
        if (bannerData != null)
        {
            bannerData.SetResultsText(ctx);
        }

        // Play sound before showing UI to avoid potential timing issues
        if (SceneManager.GetActiveScene().name == "Final Quiz")
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayFinalWinMusic();
            }
        }
        else
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWinMusic();
            }
        }
        
        // Show with proper Canvas update
        StartCoroutine(ShowBannerCoroutine(winBanner));
    }

    public void SetLoseScreen(QuizContext ctx)
    {
        if (loseBanner == null)
        {
            Debug.LogError("Lose banner is not assigned!");
            return;
        }

        BannerData bannerData = loseBanner.GetComponent<BannerData>();
        if (bannerData != null)
        {
            bannerData.SetResultsText(ctx);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLoseMusic();
        }

        // Show with proper Canvas update
        StartCoroutine(ShowBannerCoroutine(loseBanner));
    }

    private IEnumerator ShowBannerCoroutine(GameObject banner)
    {
        banner.SetActive(true);
        
        // Wait for end of frame to ensure all layout updates are processed
        yield return new WaitForEndOfFrame();
        
        // Force canvas update
        Canvas.ForceUpdateCanvases();
        
        // Force layout rebuild if the banner has layout components
        RectTransform rectTransform = banner.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}