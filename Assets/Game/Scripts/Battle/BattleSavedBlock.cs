using System;

/// <summary>
/// One block on the grid as it is written to the save file.
/// The fields are public and not properties because Unity's JSON writer only saves fields.
/// </summary>
[Serializable]
public class BattleSavedBlock
{
    /// <summary>The skin name of the block's kind, for example "RedBlock".</summary>
    public string Kind;

    /// <summary>True for an enemy block, false for one of the player's.</summary>
    public bool IsEnemy;

    /// <summary>Health the block has left.</summary>
    public int Hp;

    /// <summary>The block's column, counting from 0 at the left.</summary>
    public int Column;

    /// <summary>The block's row, counting from 0 at the top.</summary>
    public int Row;
}
