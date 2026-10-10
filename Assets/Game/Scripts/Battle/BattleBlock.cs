using UnityEngine;

/// <summary>
/// One block on the board: what kind it is, whose side it is on,
/// how much HP it has left, and where it stands.
/// </summary>
public class BattleBlock
{
    /// <summary>The block's kind: its color, starting HP, and ability.</summary>
    public BattleCubeKind Kind { get; private set; }

    /// <summary>The side this block fights for.</summary>
    public BattleSide Side { get; private set; }

    /// <summary>Health left. The block is destroyed when this reaches 0.</summary>
    public int Hp { get; private set; }

    /// <summary>Where the block is on the board: x is the column, y is the row. Set by the board.</summary>
    public Vector2Int Position { get; set; }

    /// <summary>
    /// The game time, in seconds, at which the block was put on the board. The screen
    /// sets it. It only times the block's idle bobbing; the rules never look at it.
    /// </summary>
    public float PlacedAtSeconds { get; set; }

    /// <summary>True while the block still has HP.</summary>
    public bool IsAlive => Hp > 0;

    /// <summary>Creates a block at full HP.</summary>
    public BattleBlock(BattleCubeKind kind, BattleSide side)
    {
        Reset(kind, side);
    }

    /// <summary>
    /// Creates a blank block to be filled in later with Reset or CopyFrom.
    /// The enemy keeps a set of these to plan with, instead of creating new blocks all the time.
    /// </summary>
    public BattleBlock()
    {
    }

    /// <summary>Turns this block into a fresh one of the given kind and side, at full HP.</summary>
    public void Reset(BattleCubeKind kind, BattleSide side)
    {
        Kind = kind;
        Side = side;
        Hp = kind.MaxHp;
    }

    /// <summary>
    /// Sets the HP directly, for a block read back from a saved battle.
    /// The value is kept between 0 and the kind's starting HP.
    /// </summary>
    public void RestoreHp(int hp)
    {
        Hp = Mathf.Clamp(hp, 0, Kind.MaxHp);
    }

    /// <summary>Makes this block an exact copy of another one.</summary>
    public void CopyFrom(BattleBlock other)
    {
        Kind = other.Kind;
        Side = other.Side;
        Hp = other.Hp;
        Position = other.Position;
    }

    /// <summary>Removes HP. It never goes below 0.</summary>
    public void TakeDamage(int amount)
    {
        Hp = Mathf.Max(0, Hp - amount);
    }

    /// <summary>Adds HP. It never goes above the kind's starting HP.</summary>
    public void Heal(int amount)
    {
        Hp = Mathf.Min(Kind.MaxHp, Hp + amount);
    }

    /// <summary>Sets HP to 0, for a block that is pushed off the board.</summary>
    public void Destroy()
    {
        Hp = 0;
    }
}
