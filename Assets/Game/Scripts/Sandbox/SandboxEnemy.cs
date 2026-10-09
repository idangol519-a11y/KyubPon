using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// The computer opponent of the sandbox. It is deliberately simple: it picks a
/// random block from its hand and puts it next to one of the player's blocks when it
/// can, so its blocks actually get into the fight.
/// </summary>
public class SandboxEnemy
{
    private readonly System.Random _random;
    private readonly List<Vector2Int> _emptySquares = new List<Vector2Int>();
    private readonly List<Vector2Int> _squaresNextToPlayer = new List<Vector2Int>();

    /// <summary>Creates the opponent. It shares the screen's random number generator.</summary>
    public SandboxEnemy(System.Random random)
    {
        _random = random;
    }

    /// <summary>
    /// Places one enemy block taken from the enemy's hand. Returns the block,
    /// or null when the hand is empty or the board is full.
    /// </summary>
    public SandboxBlock PlaceBlock(SandboxBoard board, SandboxHand hand)
    {
        board.CollectPositions(_emptySquares, true);
        if (_emptySquares.Count == 0 || hand.Count == 0)
        {
            return null;
        }

        _squaresNextToPlayer.Clear();
        foreach (Vector2Int square in _emptySquares)
        {
            if (IsNextToPlayerBlock(board, square))
            {
                _squaresNextToPlayer.Add(square);
            }
        }

        List<Vector2Int> choices = _squaresNextToPlayer.Count > 0 ? _squaresNextToPlayer : _emptySquares;
        Vector2Int position = choices[_random.Next(choices.Count)];
        SandboxCubeKind kind = hand.RemoveRandom(_random);

        SandboxBlock block = new SandboxBlock(kind, SandboxSide.Enemy);
        board.Place(block, position);
        return block;
    }

    private static bool IsNextToPlayerBlock(SandboxBoard board, Vector2Int square)
    {
        foreach (Vector2Int direction in SandboxCombat.Directions)
        {
            SandboxBlock neighbour = board.GetBlock(square + direction);
            if (neighbour != null && neighbour.Side == SandboxSide.Player)
            {
                return true;
            }
        }

        return false;
    }
}
