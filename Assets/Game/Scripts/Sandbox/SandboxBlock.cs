using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// One block on the sandbox board: what kind it is, whose side it is on,
/// how much HP it has left, and where it stands.
/// </summary>
public class SandboxBlock
{
    /// <summary>The block's kind: its color, starting HP, and ability.</summary>
    public SandboxCubeKind Kind { get; }

    /// <summary>The side this block fights for.</summary>
    public SandboxSide Side { get; }

    /// <summary>Health left. The block is destroyed when this reaches 0.</summary>
    public int Hp { get; private set; }

    /// <summary>Where the block is on the board: x is the column, y is the row. Set by the board.</summary>
    public Vector2Int Position { get; set; }

    /// <summary>True while the block still has HP.</summary>
    public bool IsAlive => Hp > 0;

    /// <summary>Creates a block at full HP.</summary>
    public SandboxBlock(SandboxCubeKind kind, SandboxSide side)
    {
        Kind = kind;
        Side = side;
        Hp = kind.MaxHp;
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
