using System;
using System.Collections.Generic;

/// <summary>
/// Everything needed to put a battle back exactly as it was: the grid, the blocks
/// on it, both hands, and the counters. This is what gets written to the save file.
/// The fields are public and not properties because Unity's JSON writer only saves fields.
/// </summary>
[Serializable]
public class BattleSaveData
{
    /// <summary>
    /// The layout of the save file. Raise it when a field is removed or changes
    /// meaning, so an old file is ignored instead of being read wrongly. A new field
    /// that is right when left at its default (like the potion fields, where an old
    /// save simply has an unused potion) does not need a new number.
    /// </summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>The save file layout this data was written with.</summary>
    public int FormatVersion = CurrentFormatVersion;

    /// <summary>How many squares wide the grid is.</summary>
    public int Columns;

    /// <summary>How many squares tall the grid is.</summary>
    public int Rows;

    /// <summary>The turn about to be played, counting from 1.</summary>
    public int TurnNumber;

    /// <summary>How many enemy blocks have been destroyed so far.</summary>
    public int EnemyBlocksDestroyed;

    /// <summary>How many of the player's blocks have been destroyed so far.</summary>
    public int PlayerBlocksDestroyed;

    /// <summary>True when squares are activated in a shuffled order instead of left to right.</summary>
    public bool IsRandomOrder;

    /// <summary>True when the battle already has a winner or ended in a draw.</summary>
    public bool IsBattleOver;

    /// <summary>The result line shown when the battle is over.</summary>
    public string ResultText;

    /// <summary>True when the potion has already been used in this battle.</summary>
    public bool IsPotionUsed;

    /// <summary>True when the potion was used on the turn about to be played, so it still counts.</summary>
    public bool IsPotionActive;

    /// <summary>True when the player has deleted the gem in this battle.</summary>
    public bool IsGemDeleted;

    /// <summary>Every block on the grid.</summary>
    public List<BattleSavedBlock> Blocks = new List<BattleSavedBlock>();

    /// <summary>The skin names of the blocks left in the player's hand, for example "RedBlock".</summary>
    public List<string> PlayerHand = new List<string>();

    /// <summary>The skin names of the blocks left in the enemy's hand.</summary>
    public List<string> EnemyHand = new List<string>();

    /// <summary>
    /// The board positions seen so far, used to spot a draw by repetition.
    /// Each entry here pairs with the entry at the same place in <see cref="SeenPositionCounts"/>.
    /// They are two lists because Unity's JSON writer cannot save a dictionary.
    /// </summary>
    public List<string> SeenPositions = new List<string>();

    /// <summary>How many times each position in <see cref="SeenPositions"/> has been seen.</summary>
    public List<int> SeenPositionCounts = new List<int>();
}
