using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides the order in which the squares of the board are visited during the
/// activation part of a turn. It is shared by the screen (which plays the turn
/// for real) and by the enemy (which plays turns in its head to plan a move),
/// so both always follow exactly the same rule.
/// Squares are numbered in reading order: square 0 is the top-left, and the
/// square at (column, row) has the number row * Columns + column.
/// </summary>
public static class BattleActivation
{
    /// <summary>The number of the square at a board position.</summary>
    public static int IndexOf(BattleBoard board, Vector2Int position)
    {
        return position.y * board.Columns + position.x;
    }

    /// <summary>The board position of a numbered square.</summary>
    public static Vector2Int SquareAt(BattleBoard board, int index)
    {
        return new Vector2Int(index % board.Columns, index / board.Columns);
    }

    /// <summary>
    /// Fills "order" with every square of the board exactly once.
    /// Left to right: start at a random block and go square by square in reading
    /// order, wrapping from the last square back to the first.
    /// Random: all squares, shuffled, so the order is different every turn.
    /// "blockSquares" is a spare list the method uses while it works.
    /// </summary>
    public static void BuildVisitOrder(BattleBoard board, System.Random random, bool isRandomOrder,
        List<int> order, List<Vector2Int> blockSquares)
    {
        int squareCount = board.Columns * board.Rows;
        int startIndex = isRandomOrder ? 0 : PickStartIndex(board, random, blockSquares);

        order.Clear();
        for (int step = 0; step < squareCount; step++)
        {
            order.Add((startIndex + step) % squareCount);
        }

        if (!isRandomOrder)
        {
            return;
        }

        // A standard shuffle: walk back from the end, swapping each entry with a
        // randomly chosen one at or before it. Every order is equally likely.
        for (int last = order.Count - 1; last > 0; last--)
        {
            int other = random.Next(last + 1);
            int swapped = order[last];
            order[last] = order[other];
            order[other] = swapped;
        }
    }

    /// <summary>
    /// Picks the square of a random block to start from.
    /// With no blocks at all, any square can be the start.
    /// </summary>
    private static int PickStartIndex(BattleBoard board, System.Random random, List<Vector2Int> blockSquares)
    {
        board.CollectPositions(blockSquares, false);
        if (blockSquares.Count == 0)
        {
            return random.Next(board.Columns * board.Rows);
        }

        return IndexOf(board, blockSquares[random.Next(blockSquares.Count)]);
    }
}
