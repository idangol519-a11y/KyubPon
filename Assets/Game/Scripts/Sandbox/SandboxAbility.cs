/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// What a sandbox block does when it is activated.
/// </summary>
public enum SandboxAbility
{
    /// <summary>Damages every opposing block directly next to it.</summary>
    Strike,

    /// <summary>Pushes every opposing block next to it one square away.</summary>
    Push,

    /// <summary>Heals every friendly block directly next to it.</summary>
    Heal,

    /// <summary>Damages every opposing block in the same row.</summary>
    RowShot
}
