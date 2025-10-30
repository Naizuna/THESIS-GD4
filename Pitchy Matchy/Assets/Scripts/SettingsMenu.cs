using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        bgmSlider.value = PlayerPrefs.GetFloat("BGMVol", 0.5f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);

        masterSlider.onValueChanged.AddListener(SoundManager.Instance.SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
    }
}
