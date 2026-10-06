using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// One kind of sandbox cube: its name, color, and scoring rule.
/// To add or change a cube, edit the list in <see cref="All"/>.
/// </summary>
public class SandboxCubeKind
{
    /// <summary>Every kind of cube, in the order shown in the palette and the rules note.</summary>
    public static readonly SandboxCubeKind[] All =
    {
        new SandboxCubeKind("RED", new Color(0.90f, 0.30f, 0.25f), SandboxScoreRule.FlatPoints,
            $"+{SandboxScoring.FlatPoints} POINTS"),
        new SandboxCubeKind("BLUE", new Color(0.25f, 0.60f, 0.95f), SandboxScoreRule.PointsPerNeighbour,
            $"+{SandboxScoring.PointsPerNeighbour} PER CUBE NEXT TO IT"),
        new SandboxCubeKind("GREEN", new Color(0.35f, 0.80f, 0.40f), SandboxScoreRule.DoubleScore,
            $"SCORE SO FAR X{SandboxScoring.ScoreMultiplier}"),
        new SandboxCubeKind("YELLOW", new Color(1.00f, 0.82f, 0.20f), SandboxScoreRule.PointsPerCubeOnGrid,
            $"+{SandboxScoring.PointsPerCubeOnGrid} PER CUBE ON THE GRID")
    };

    /// <summary>The name shown to the player, for example "RED".</summary>
    public string Name { get; }

    /// <summary>The cube's color.</summary>
    public Color Color { get; }

    /// <summary>How this cube changes the score.</summary>
    public SandboxScoreRule Rule { get; }

    /// <summary>The rule in words, shown in the rules note.</summary>
    public string RuleText { get; }

    private SandboxCubeKind(string name, Color color, SandboxScoreRule rule, string ruleText)
    {
        Name = name;
        Color = color;
        Rule = rule;
        RuleText = ruleText;
    }
}
