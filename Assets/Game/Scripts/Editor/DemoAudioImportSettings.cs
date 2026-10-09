using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets how the soundtrack file is imported. A long music file is "streamed":
/// played straight from disk a little at a time, instead of being unpacked
/// into memory all at once, which would use a lot of memory for one song.
/// </summary>
public class DemoAudioImportSettings : AssetPostprocessor
{
    private const string MusicPath = "Assets/Game/Audio/TemporaryDemo/Resources/DemoAudio/Music.mp3";

    /// <summary>Unity runs this whenever an audio file is imported.</summary>
    private void OnPreprocessAudio()
    {
        if (assetPath == MusicPath)
        {
            UseStreaming((AudioImporter)assetImporter);
        }
    }

    /// <summary>
    /// Runs each time the Editor loads its scripts. It covers the case where the
    /// music file was imported before this script existed.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void FixMusicImportedEarlier()
    {
        AudioImporter importer = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
        if (importer != null && importer.defaultSampleSettings.loadType != AudioClipLoadType.Streaming)
        {
            UseStreaming(importer);
            importer.SaveAndReimport();
        }
    }

    private static void UseStreaming(AudioImporter importer)
    {
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
    }
}
