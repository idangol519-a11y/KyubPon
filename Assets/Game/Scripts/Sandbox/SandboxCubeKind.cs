using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// One kind of sandbox block: its name, color, starting HP, and ability.
/// Both sides use the same kinds; the enemy's copies are drawn with an X.
/// To add or change a block, edit the list in <see cref="All"/>.
/// </summary>
public class SandboxCubeKind
{
    // How likely a kind is to be dealt, compared with the others. A common block is
    // four times as likely as the legendary one, so most hands hold no orange block
    // and a hand with two is rare.
    private const int CommonDealWeight = 4;
    private const int LegendaryDealWeight = 1;

    /// <summary>Every kind of block, in the order shown in the palette and the rules note.</summary>
    public static readonly SandboxCubeKind[] All =
    {
        new SandboxCubeKind("RED", "RedBlock", new Color(0.90f, 0.30f, 0.25f), 6, CommonDealWeight,
            SandboxAbility.Strike, "STRIKES", GameSound.Strike,
            $"-{SandboxCombat.StrikeDamage} HP TO EACH ENEMY NEXT TO IT"),
        new SandboxCubeKind("BLUE", "BlueBlock", new Color(0.25f, 0.60f, 0.95f), 8, CommonDealWeight,
            SandboxAbility.Push, "PUSHES", GameSound.Push,
            $"PUSHES EACH ENEMY NEXT TO IT 1 SQUARE AWAY. OFF THE EDGE = DESTROYED. BLOCKED = -{SandboxCombat.BlockedPushDamage} HP"),
        new SandboxCubeKind("GREEN", "GreenBlock", new Color(0.35f, 0.80f, 0.40f), 5, CommonDealWeight,
            SandboxAbility.Heal, "HEALS", GameSound.Heal,
            $"+{SandboxCombat.HealAmount} HP TO EACH FRIEND NEXT TO IT"),
        new SandboxCubeKind("YELLOW", "YellowBlock", new Color(1.00f, 0.82f, 0.20f), 4, CommonDealWeight,
            SandboxAbility.RowShot, "SHOOTS", GameSound.RowShot,
            $"-{SandboxCombat.RowShotDamage} HP TO EVERY ENEMY IN ITS ROW"),
        new SandboxCubeKind("ORANGE", "OrangeBlock", new Color(1.00f, 0.55f, 0.10f), 7, LegendaryDealWeight,
            SandboxAbility.Legend, "GOES WILD", GameSound.Legendary,
            $"LEGENDARY. EVERY FRIEND NEXT TO IT USES ITS ABILITY AGAIN. THEN -{SandboxCombat.LegendDamage} HP TO EVERY ENEMY ON THE GRID")
    };

    /// <summary>The name shown to the player, for example "RED".</summary>
    public string Name { get; }

    /// <summary>
    /// The name of the block's skin folder and PNG, for example "RedBlock"
    /// (StreamingAssets/Content/Blocks/RedBlock/RedBlock.png).
    /// </summary>
    public string SkinName { get; }

    /// <summary>
    /// The block's color. Used for its name in the rules note, and as a plain
    /// colored square if the skin PNG is missing.
    /// </summary>
    public Color Color { get; }

    /// <summary>The HP a new block of this kind starts with. Healing cannot go above it.</summary>
    public int MaxHp { get; }

    /// <summary>How likely this kind is to be dealt, compared with the other kinds. Higher is more common.</summary>
    public int DealWeight { get; }

    /// <summary>What this block does when it is activated.</summary>
    public SandboxAbility Ability { get; }

    /// <summary>A word or two for the status line, for example "STRIKES".</summary>
    public string ActionWord { get; }

    /// <summary>The sound played when the ability does something.</summary>
    public GameSound Sound { get; }

    /// <summary>The ability in words, shown in the rules note.</summary>
    public string RuleText { get; }

    /// <summary>
    /// Picks a random kind for a hand. Kinds with a higher deal weight come up more often.
    /// It works like a raffle: each kind holds as many tickets as its weight, and one ticket is drawn.
    /// </summary>
    public static SandboxCubeKind PickRandom(System.Random random)
    {
        int totalTickets = 0;
        foreach (SandboxCubeKind kind in All)
        {
            totalTickets += kind.DealWeight;
        }

        int ticket = random.Next(totalTickets);
        foreach (SandboxCubeKind kind in All)
        {
            if (ticket < kind.DealWeight)
            {
                return kind;
            }

            ticket -= kind.DealWeight;
        }

        return All[0];
    }

    /// <summary>
    /// Makes an image look like this block: its skin if the PNG exists, otherwise
    /// a square in the block's color. "Darkening" (0 to 1) dims it, which is how
    /// enemy blocks are told apart.
    /// </summary>
    public void ApplyLook(UnityEngine.UI.Image image, float darkening)
    {
        Sprite skin = BlockSkinLoader.Load(SkinName);
        image.sprite = skin;

        // An image's color is multiplied with its picture, so white shows the skin unchanged.
        Color baseColor = skin != null ? Color.white : Color;
        image.color = Color.Lerp(baseColor, Color.black, darkening);
    }

    private SandboxCubeKind(string name, string skinName, Color color, int maxHp, int dealWeight,
        SandboxAbility ability, string actionWord, GameSound sound, string ruleText)
    {
        Name = name;
        SkinName = skinName;
        Color = color;
        MaxHp = maxHp;
        DealWeight = dealWeight;
        Ability = ability;
        ActionWord = actionWord;
        Sound = sound;
        RuleText = ruleText;
    }
}
