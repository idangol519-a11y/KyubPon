/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// The different ways a sandbox cube can change the score when it is activated.
/// </summary>
public enum SandboxScoreRule
{
    /// <summary>Adds a fixed number of points.</summary>
    FlatPoints,

    /// <summary>Adds points for each cube directly next to it (up, down, left, right).</summary>
    PointsPerNeighbour,

    /// <summary>Doubles the score collected so far.</summary>
    DoubleScore,

    /// <summary>Adds points for every cube anywhere on the grid.</summary>
    PointsPerCubeOnGrid
}
