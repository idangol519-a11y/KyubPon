using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads block pictures ("skins") from PNG files while the game runs.
/// Each block has its own folder: StreamingAssets/Content/Blocks/RedBlock/RedBlock.png.
/// Because the files are read at run time, a skin can be changed by replacing
/// its PNG, with no code change and no rebuild.
/// </summary>
public static class BlockSkinLoader
{
    private const string BlocksFolder = "Content/Blocks";

    // Skins are kept after the first load, so each PNG is read from disk only once.
    private static readonly Dictionary<string, Sprite> LoadedSkins = new Dictionary<string, Sprite>();

    /// <summary>
    /// Returns the skin for a block, for example "RedBlock".
    /// Returns null if the PNG is missing or broken; the caller should then
    /// draw a plain colored square instead, so the game never crashes over art.
    /// </summary>
    public static Sprite Load(string blockName)
    {
        if (LoadedSkins.TryGetValue(blockName, out Sprite cachedSkin))
        {
            return cachedSkin;
        }

        // Reading files directly works on PC. On Android, StreamingAssets must be
        // read with UnityWebRequest instead; this method is the one place to change.
        string path = Path.Combine(Application.streamingAssetsPath, BlocksFolder, blockName, blockName + ".png");
        Sprite skin = File.Exists(path) ? CreateSprite(File.ReadAllBytes(path)) : null;
        if (skin == null)
        {
            Debug.LogWarning($"[Warn] Block skin not found or unreadable: {path}. Using a plain colored square.");
        }

        LoadedSkins[blockName] = skin;
        return skin;
    }

    private static Sprite CreateSprite(byte[] pngBytes)
    {
        // The starting size does not matter: LoadImage resizes the texture to fit the PNG.
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(pngBytes))
        {
            return null;
        }

        // Point filtering keeps pixel art sharp when it is drawn larger than its real size.
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Rect wholeImage = new Rect(0f, 0f, texture.width, texture.height);
        Vector2 center = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, wholeImage, center, texture.width);
    }
}
