using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullScreenToggle;

    private Resolution[] _resolutions;

    private void Start()
    {
        // 1. Configura Volume
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.value = savedVolume;
            _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        AudioListener.volume = savedVolume;

        // 2. Configura Resoluções
        _resolutions = Screen.resolutions;
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = _resolutions[i].width + " x " + _resolutions[i].height + " @ " + _resolutions[i].refreshRateRatio.numerator + "Hz";
                options.Add(option);

                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }

            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.value = currentResIndex;
            _resolutionDropdown.RefreshShownValue();
            _resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        // 3. Fullscreen
        if (_fullScreenToggle != null)
        {
            _fullScreenToggle.isOn = Screen.fullScreen;
            _fullScreenToggle.onValueChanged.AddListener(SetFullScreen);
        }
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetResolution(int index)
    {
        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    public void BackToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
