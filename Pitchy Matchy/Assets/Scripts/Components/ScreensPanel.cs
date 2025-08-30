using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreensPanel : MonoBehaviour
{
    [SerializeField] GameObject parentPanel;
    [SerializeField] TMP_Text textObj;
    [SerializeField] string winText;
    [SerializeField] string loseText;

    public void HideParentPanel()
    {
        UIUtils.HideUIComponents(parentPanel);
    }

    public void ShowParentPanel()
    {
        UIUtils.ShowUIComponents(parentPanel);
    }

    public void SetWinScreen()
    {
        textObj.text = winText;
    }

    public void SetLoseScreen()
    {
        textObj.text = loseText;
    }
}
