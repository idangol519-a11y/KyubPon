using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// One kind of sandbox block: its name, color, starting HP, and ability.
/// Both sides use the same kinds; the enemy's copies are drawn with an X.
/// To add or change a block, edit the list in <see cref="All"/>.
/// </summary>
public class SandboxCubeKind
{
    /// <summary>Every kind of block, in the order shown in the palette and the rules note.</summary>
    public static readonly SandboxCubeKind[] All =
    {
        new SandboxCubeKind("RED", new Color(0.90f, 0.30f, 0.25f), 6, SandboxAbility.Strike, "STRIKES",
            $"-{SandboxCombat.StrikeDamage} HP TO EACH ENEMY NEXT TO IT"),
        new SandboxCubeKind("BLUE", new Color(0.25f, 0.60f, 0.95f), 8, SandboxAbility.Push, "PUSHES",
            $"PUSHES EACH ENEMY NEXT TO IT 1 SQUARE AWAY. OFF THE EDGE = DESTROYED. BLOCKED = -{SandboxCombat.BlockedPushDamage} HP"),
        new SandboxCubeKind("GREEN", new Color(0.35f, 0.80f, 0.40f), 5, SandboxAbility.Heal, "HEALS",
            $"+{SandboxCombat.HealAmount} HP TO EACH FRIEND NEXT TO IT"),
        new SandboxCubeKind("YELLOW", new Color(1.00f, 0.82f, 0.20f), 4, SandboxAbility.RowShot, "SHOOTS",
            $"-{SandboxCombat.RowShotDamage} HP TO EVERY ENEMY IN ITS ROW")
    };

    /// <summary>The name shown to the player, for example "RED".</summary>
    public string Name { get; }

    /// <summary>The block's color.</summary>
    public Color Color { get; }

    /// <summary>The HP a new block of this kind starts with. Healing cannot go above it.</summary>
    public int MaxHp { get; }

    /// <summary>What this block does when it is activated.</summary>
    public SandboxAbility Ability { get; }

    /// <summary>One word for the status line, for example "STRIKES".</summary>
    public string ActionWord { get; }

    /// <summary>The ability in words, shown in the rules note.</summary>
    public string RuleText { get; }

    private SandboxCubeKind(string name, Color color, int maxHp, SandboxAbility ability, string actionWord, string ruleText)
    {
        Name = name;
        Color = color;
        MaxHp = maxHp;
        Ability = ability;
        ActionWord = actionWord;
        RuleText = ruleText;
    }
}
