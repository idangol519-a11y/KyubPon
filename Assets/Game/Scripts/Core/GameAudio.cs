using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The game's sound player: one place that plays the soundtrack and every
/// sound effect, so any script can make a sound with one line:
/// GameAudio.Play(GameSound.ButtonClick).
/// It creates itself when the game starts and stays alive across scenes,
/// which is why the music keeps playing when the screen changes.
/// This is one of the few global services in the game (see docs/GameState.md).
/// Sound files are loaded from a "Resources/DemoAudio" folder. A missing file
/// is skipped with a warning; it never stops the game.
/// </summary>
public class GameAudio : MonoBehaviour
{
    private const string AudioFolder = "DemoAudio/";
    private const string MusicName = "Music";
    private const string HitChainName = "HitChain";

    /// <summary>How many "hit chain" sounds exist (HitChain1 to HitChain10), each higher than the last.</summary>
    public const int HitChainSteps = 10;

    private static GameAudio _instance;

    private readonly Dictionary<string, AudioClip> _loadedClips = new Dictionary<string, AudioClip>();
    private AudioSource _musicSource;
    private AudioSource _soundSource;

    /// <summary>
    /// Unity calls this once, before the first scene loads, whichever scene that is.
    /// It creates the sound player, applies the saved volumes, and starts the music.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateWhenGameStarts()
    {
        GameObject audioObject = new GameObject("GameAudio");
        DontDestroyOnLoad(audioObject);
        _instance = audioObject.AddComponent<GameAudio>();

        // Two speakers: one loops the soundtrack, the other plays short effects on top of it.
        _instance._musicSource = audioObject.AddComponent<AudioSource>();
        _instance._musicSource.loop = true;
        _instance._soundSource = audioObject.AddComponent<AudioSource>();

        GameSettings.Load().ApplyAudio();
        _instance.StartMusic();
    }

    /// <summary>Plays a sound effect once.</summary>
    public static void Play(GameSound sound)
    {
        if (_instance != null)
        {
            _instance.PlayClip(sound.ToString());
        }
    }

    /// <summary>
    /// Plays one of the rising "hit chain" sounds. Step 1 is the lowest; each hit in a
    /// row uses the next step, so a long chain of hits climbs in pitch. Steps past the
    /// last one keep playing the highest sound.
    /// </summary>
    public static void PlayHitChain(int step)
    {
        if (_instance != null)
        {
            _instance.PlayClip(HitChainName + Mathf.Clamp(step, 1, HitChainSteps));
        }
    }

    /// <summary>Sets how loud the soundtrack and the sound effects are, each from 0 (silent) to 1 (full).</summary>
    public static void SetVolumes(float musicVolume, float soundVolume)
    {
        if (_instance != null)
        {
            _instance._musicSource.volume = musicVolume;
            _instance._soundSource.volume = soundVolume;
        }
    }

    private void StartMusic()
    {
        AudioClip music = LoadClip(MusicName);
        if (music != null)
        {
            _musicSource.clip = music;
            _musicSource.Play();
        }
    }

    private void PlayClip(string clipName)
    {
        AudioClip clip = LoadClip(clipName);
        if (clip != null)
        {
            // PlayOneShot lets several effects overlap instead of cutting each other off.
            _soundSource.PlayOneShot(clip);
        }
    }

    /// <summary>Loads a sound file the first time it is needed and remembers it, found or not.</summary>
    private AudioClip LoadClip(string clipName)
    {
        if (_loadedClips.TryGetValue(clipName, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(AudioFolder + clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[Warn] Sound file not found: Resources/{AudioFolder}{clipName}. It will be silent.");
        }

        _loadedClips[clipName] = clip;
        return clip;
    }
}
