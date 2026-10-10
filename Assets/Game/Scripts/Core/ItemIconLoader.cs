using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads item pictures from PNG files while the game runs.
/// Each item has its own folder: StreamingAssets/Content/Items/Potion/Potion.png.
/// Because the files are read at run time, a picture can be changed by replacing
/// its PNG, with no code change and no rebuild.
/// </summary>
public static class ItemIconLoader
{
    private const string ItemsFolder = "Content/Items";

    // Icons are kept after the first load, so each PNG is read from disk only once.
    private static readonly Dictionary<string, Sprite> LoadedIcons = new Dictionary<string, Sprite>();

    /// <summary>
    /// Returns the picture for an item, for example "Potion".
    /// Returns null if the PNG is missing or broken; the caller should then
    /// keep its plain colored square, so the game never crashes over art.
    /// </summary>
    public static Sprite Load(string itemName)
    {
        if (LoadedIcons.TryGetValue(itemName, out Sprite cachedIcon))
        {
            return cachedIcon;
        }

        // Reading files directly works on PC. On Android, StreamingAssets must be
        // read with UnityWebRequest instead; this method is the one place to change.
        string path = Path.Combine(Application.streamingAssetsPath, ItemsFolder, itemName, itemName + ".png");
        Sprite icon = File.Exists(path) ? CreateSprite(File.ReadAllBytes(path)) : null;
        if (icon == null)
        {
            Debug.LogWarning($"[Warn] Item picture not found or unreadable: {path}. Using a plain colored square.");
        }

        LoadedIcons[itemName] = icon;
        return icon;
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
