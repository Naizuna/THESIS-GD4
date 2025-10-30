using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreensPanel : MonoBehaviour
{

    [SerializeField] GameObject winBanner;
    [SerializeField] GameObject loseBanner;
    [SerializeField] string resultsText;

    void Start()
    {
        UIUtils.HideUIComponents(winBanner);
        UIUtils.HideUIComponents(loseBanner);
    }

    public void SetWinScreen(QuizContext ctx)
    {
        BannerData bannerData = winBanner.GetComponent<BannerData>();
        bannerData.SetResultsText(ctx);
        SoundManager.Instance.PlayWinMusic();
        UIUtils.ShowUIComponents(winBanner);
    }

    public void SetLoseScreen(QuizContext ctx)
    {
        BannerData bannerData = loseBanner.GetComponent<BannerData>();
        bannerData.SetResultsText(ctx);
        SoundManager.Instance.PlayLoseMusic();
        UIUtils.ShowUIComponents(loseBanner);
    }
}
