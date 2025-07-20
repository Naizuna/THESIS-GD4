using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIUtils
{
    public static void ShowUIComponents(params GameObject[] components)
    {
        foreach(var go in components)
        {
            if(go is null) continue;

            go.SetActive(true);
        }
    }

    public static void HideUIComponents(params GameObject[] components)
    {
        foreach(var go in components)
        {
            if(go is null) continue;

            go.SetActive(false);
        }
    }
}
