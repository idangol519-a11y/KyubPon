using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads the pictures of a frame-by-frame effect (like a slash) from PNG files
/// while the game runs. An effect has its own folder with numbered frames:
/// StreamingAssets/Content/Effects/Slash/Slash_0.png, Slash_1.png, and so on.
/// Unity cannot play GIF files, so an animation is stored as one PNG per frame.
/// </summary>
public static class EffectFramesLoader
{
    private const string EffectsFolder = "Content/Effects";

    // Frames are kept after the first load, so each PNG is read from disk only once.
    private static readonly Dictionary<string, Sprite[]> LoadedEffects = new Dictionary<string, Sprite[]>();

    /// <summary>
    /// Returns the frames of an effect, for example "Slash", in play order.
    /// Returns an empty list if there are no frames; the caller should then
    /// draw something simple instead, so the game never crashes over art.
    /// </summary>
    public static Sprite[] Load(string effectName)
    {
        if (LoadedEffects.TryGetValue(effectName, out Sprite[] cachedFrames))
        {
            return cachedFrames;
        }

        // Reading files directly works on PC. On Android, StreamingAssets must be
        // read with UnityWebRequest instead; this method is the one place to change.
        string folder = Path.Combine(Application.streamingAssetsPath, EffectsFolder, effectName);
        List<Sprite> frames = new List<Sprite>();
        while (true)
        {
            string path = Path.Combine(folder, $"{effectName}_{frames.Count}.png");
            Sprite frame = File.Exists(path) ? CreateSprite(File.ReadAllBytes(path)) : null;
            if (frame == null)
            {
                break;
            }

            frames.Add(frame);
        }

        if (frames.Count == 0)
        {
            Debug.LogWarning($"[Warn] No frames found for effect '{effectName}' in {folder}. Using a plain effect instead.");
        }

        Sprite[] result = frames.ToArray();
        LoadedEffects[effectName] = result;
        return result;
    }

    private static Sprite CreateSprite(byte[] pngBytes)
    {
        // The starting size does not matter: LoadImage resizes the texture to fit the PNG.
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(pngBytes))
        {
            return null;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        Rect wholeImage = new Rect(0f, 0f, texture.width, texture.height);
        Vector2 center = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, wholeImage, center, texture.width);
    }
}
