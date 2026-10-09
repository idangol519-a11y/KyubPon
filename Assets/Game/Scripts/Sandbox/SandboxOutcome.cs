/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// How a sandbox battle stands: still going, won by one side, or drawn.
/// </summary>
public enum SandboxOutcome
{
    /// <summary>The battle is still going.</summary>
    Undecided,

    /// <summary>The enemy has no blocks left on the grid or in hand.</summary>
    PlayerWins,

    /// <summary>The player has no blocks left on the grid or in hand.</summary>
    EnemyWins,

    /// <summary>Neither side won: both ran out together, or the board stopped changing.</summary>
    Draw
}
