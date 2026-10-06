using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The Settings page opened from the Home screen.
/// Every change is applied straight away, so there is no "Apply" button.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    private const string FullscreenOnText = "FULLSCREEN: ON";
    private const string FullscreenOffText = "FULLSCREEN: OFF";
    private const float PercentScale = 100f;

    [SerializeField] private UnityEngine.UI.Slider _volumeSlider;
    [SerializeField] private UnityEngine.UI.Text _volumeValueLabel;
    [SerializeField] private UnityEngine.UI.Button _fullscreenButton;
    [SerializeField] private UnityEngine.UI.Text _fullscreenLabel;
    [SerializeField] private UnityEngine.UI.Button _previousResolutionButton;
    [SerializeField] private UnityEngine.UI.Button _nextResolutionButton;
    [SerializeField] private UnityEngine.UI.Text _resolutionLabel;
    [SerializeField] private UnityEngine.UI.Button _backButton;

    private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
    private GameSettings _settings;
    private int _resolutionIndex;

    /// <summary>Raised when the player presses Back.</summary>
    public event Action Closed;

    private void Awake()
    {
        _volumeSlider.onValueChanged.AddListener(ChangeVolume);
        _fullscreenButton.onClick.AddListener(ToggleFullscreen);
        _previousResolutionButton.onClick.AddListener(SelectPreviousResolution);
        _nextResolutionButton.onClick.AddListener(SelectNextResolution);
        _backButton.onClick.AddListener(Close);
    }

    /// <summary>Shows the panel and fills it with the given settings.</summary>
    public void Open(GameSettings settings)
    {
        _settings = settings;
        gameObject.SetActive(true);

        BuildResolutionList();
        ShowCurrentValues();
        EventSystem.current.SetSelectedGameObject(_volumeSlider.gameObject);
    }

    private void Close()
    {
        _settings.Save();
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    /// <summary>
    /// Collects the screen sizes this monitor supports. Unity lists each size once
    /// per refresh rate, so duplicates are skipped. The size currently in use is
    /// always included, even if the monitor did not report it.
    /// </summary>
    private void BuildResolutionList()
    {
        _resolutions.Clear();
        foreach (Resolution resolution in Screen.resolutions)
        {
            AddResolutionIfNew(new Vector2Int(resolution.width, resolution.height));
        }

        Vector2Int current = new Vector2Int(_settings.ResolutionWidth, _settings.ResolutionHeight);
        AddResolutionIfNew(current);
        _resolutionIndex = _resolutions.IndexOf(current);
    }

    private void AddResolutionIfNew(Vector2Int resolution)
    {
        if (!_resolutions.Contains(resolution))
        {
            _resolutions.Add(resolution);
        }
    }

    private void ShowCurrentValues()
    {
        // SetValueWithoutNotify moves the slider without triggering ChangeVolume.
        _volumeSlider.SetValueWithoutNotify(_settings.MasterVolume);
        ShowVolume();
        ShowFullscreen();
        ShowResolution();
    }

    private void ChangeVolume(float volume)
    {
        _settings.MasterVolume = volume;
        _settings.ApplyAudio();
        ShowVolume();
        // Not saved here: this runs many times a second while the slider is dragged.
        // The volume is written to disk once, when the panel closes.
    }

    private void ToggleFullscreen()
    {
        _settings.Fullscreen = !_settings.Fullscreen;
        ApplyAndSaveDisplay();
        ShowFullscreen();
    }

    private void SelectPreviousResolution()
    {
        SelectResolution(_resolutionIndex - 1);
    }

    private void SelectNextResolution()
    {
        SelectResolution(_resolutionIndex + 1);
    }

    /// <summary>Picks a resolution from the list, wrapping around at both ends.</summary>
    private void SelectResolution(int index)
    {
        _resolutionIndex = (index + _resolutions.Count) % _resolutions.Count;
        _settings.ResolutionWidth = _resolutions[_resolutionIndex].x;
        _settings.ResolutionHeight = _resolutions[_resolutionIndex].y;
        ApplyAndSaveDisplay();
        ShowResolution();
    }

    private void ApplyAndSaveDisplay()
    {
        _settings.ApplyDisplay();
        _settings.Save();
    }

    private void ShowVolume()
    {
        _volumeValueLabel.text = $"{Mathf.RoundToInt(_settings.MasterVolume * PercentScale)}%";
    }

    private void ShowFullscreen()
    {
        _fullscreenLabel.text = _settings.Fullscreen ? FullscreenOnText : FullscreenOffText;
    }

    private void ShowResolution()
    {
        _resolutionLabel.text = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
    }
}
