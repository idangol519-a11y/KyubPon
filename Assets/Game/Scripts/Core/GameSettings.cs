using UnityEngine;

/// <summary>
/// The player's settings (volume, fullscreen, resolution).
/// Values are stored with PlayerPrefs, which Unity saves on the player's computer,
/// so they are remembered the next time the game starts.
/// </summary>
public class GameSettings
{
    private const string MasterVolumeKey = "settings.masterVolume";
    private const string FullscreenKey = "settings.fullscreen";
    private const string ResolutionWidthKey = "settings.resolutionWidth";
    private const string ResolutionHeightKey = "settings.resolutionHeight";
    private const float DefaultMasterVolume = 1f;

    /// <summary>Overall game volume, from 0 (silent) to 1 (full).</summary>
    public float MasterVolume { get; set; }

    /// <summary>True when the game fills the whole screen, false when it runs in a window.</summary>
    public bool Fullscreen { get; set; }

    /// <summary>Screen width in pixels.</summary>
    public int ResolutionWidth { get; set; }

    /// <summary>Screen height in pixels.</summary>
    public int ResolutionHeight { get; set; }

    /// <summary>
    /// Reads the saved settings. Anything that was never saved
    /// falls back to what the game is using right now.
    /// </summary>
    public static GameSettings Load()
    {
        return new GameSettings
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)),
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1,
            ResolutionWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width),
            ResolutionHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height)
        };
    }

    /// <summary>Writes the settings to disk so they survive closing the game.</summary>
    public void Save()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionWidthKey, ResolutionWidth);
        PlayerPrefs.SetInt(ResolutionHeightKey, ResolutionHeight);
        PlayerPrefs.Save();
    }

    /// <summary>Makes the game use these settings now.</summary>
    public void Apply()
    {
        ApplyAudio();
        ApplyDisplay();
    }

    /// <summary>Sets the volume of everything the player hears.</summary>
    public void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
    }

    /// <summary>
    /// Changes the window size and fullscreen mode.
    /// This has no visible effect inside the Unity Editor; it works in a built game.
    /// </summary>
    public void ApplyDisplay()
    {
        bool alreadyApplied = Screen.width == ResolutionWidth
            && Screen.height == ResolutionHeight
            && Screen.fullScreen == Fullscreen;
        if (alreadyApplied)
        {
            return;
        }

        // FullScreenWindow is "borderless fullscreen": it switches quickly and
        // behaves well when the player alt-tabs.
        FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, mode);
    }
}
