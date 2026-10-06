/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// The scoring maths for sandbox cubes. Plain C# with no Unity code,
/// so the rules are easy to read and change in one place.
/// </summary>
public static class SandboxScoring
{
    /// <summary>Points a "flat points" cube adds.</summary>
    public const int FlatPoints = 10;

    /// <summary>Points added for each cube next to a "per neighbour" cube.</summary>
    public const int PointsPerNeighbour = 5;

    /// <summary>What a "double score" cube multiplies the score by.</summary>
    public const int ScoreMultiplier = 2;

    /// <summary>Points added for each cube on the grid by a "per cube on grid" cube.</summary>
    public const int PointsPerCubeOnGrid = 1;

    /// <summary>
    /// Returns the new score after one cube is activated.
    /// The score is a double because doubling it many times makes numbers
    /// far too large for an int.
    /// </summary>
    public static double Apply(SandboxScoreRule rule, double scoreSoFar, int neighbourCubes, int cubesOnGrid)
    {
        switch (rule)
        {
            case SandboxScoreRule.FlatPoints:
                return scoreSoFar + FlatPoints;
            case SandboxScoreRule.PointsPerNeighbour:
                return scoreSoFar + PointsPerNeighbour * neighbourCubes;
            case SandboxScoreRule.DoubleScore:
                return scoreSoFar * ScoreMultiplier;
            case SandboxScoreRule.PointsPerCubeOnGrid:
                return scoreSoFar + PointsPerCubeOnGrid * cubesOnGrid;
            default:
                return scoreSoFar;
        }
    }
}
