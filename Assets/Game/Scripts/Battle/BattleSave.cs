using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads and writes the saved battle. There is one save file, kept in the game's
/// own data folder (Application.persistentDataPath), so Continue on the Home
/// screen can pick the battle up where the player left it.
/// A problem with the file is logged and never stops the game.
/// </summary>
public static class BattleSave
{
    private const string FileName = "BattleSave.json";

    // The new save is written beside the old one first, then swapped in. If the
    // game closes halfway through writing, the old save is still complete.
    private const string TemporaryFileName = "BattleSave.json.new";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string TemporaryFilePath => Path.Combine(Application.persistentDataPath, TemporaryFileName);

    /// <summary>True when there is a saved battle to continue.</summary>
    public static bool Exists => File.Exists(FilePath);

    /// <summary>Writes the battle to the save file, replacing the previous save.</summary>
    public static void Save(BattleSaveData data)
    {
        try
        {
            File.WriteAllText(TemporaryFilePath, JsonUtility.ToJson(data, true));
            File.Copy(TemporaryFilePath, FilePath, true);
            File.Delete(TemporaryFilePath);
        }
        catch (Exception problem)
        {
            Debug.LogWarning($"[Warn] Could not save the battle to {FilePath}: {problem.Message}");
        }
    }

    /// <summary>
    /// Reads the saved battle. Returns null when there is no save, or when the file
    /// cannot be read or was written by a different version of the save layout.
    /// </summary>
    public static BattleSaveData Load()
    {
        if (!Exists)
        {
            return null;
        }

        try
        {
            BattleSaveData data = JsonUtility.FromJson<BattleSaveData>(File.ReadAllText(FilePath));
            if (data == null || data.FormatVersion != BattleSaveData.CurrentFormatVersion)
            {
                Debug.LogWarning($"[Warn] The saved battle at {FilePath} is from another version and was ignored.");
                return null;
            }

            return data;
        }
        catch (Exception problem)
        {
            Debug.LogWarning($"[Warn] Could not read the saved battle at {FilePath}: {problem.Message}");
            return null;
        }
    }

    /// <summary>Removes the saved battle, so the next battle starts fresh.</summary>
    public static void Delete()
    {
        try
        {
            File.Delete(FilePath);
        }
        catch (Exception problem)
        {
            Debug.LogWarning($"[Warn] Could not delete the saved battle at {FilePath}: {problem.Message}");
        }
    }
}
