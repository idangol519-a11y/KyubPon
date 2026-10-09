using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// The computer opponent of the sandbox. It chooses its move the way a simple
/// chess program does:
///   1. Search. List every move it could make: each kind of block in its hand
///      on each empty square.
///   2. Simulate. For each move, play the rest of the turn on a spare copy of
///      the board, without showing anything on screen.
///   3. Evaluate. Give the resulting board a score: its own blocks and their HP
///      count for it, the player's count against it (chess programs call this
///      "material").
///   4. Choose the move with the best score.
/// Chess has no luck, but this game does: the activation order is random. So
/// each move is simulated several times and the scores are averaged, which
/// tells the enemy how good the move is on average.
/// It looks one turn ahead only. It does not try to guess the player's next move.
/// </summary>
public class SandboxEnemy
{
    // How many times each move is played out. More is steadier but slower.
    private const int SimulationsPerMove = 6;

    // What a block is worth just for being alive, on top of its HP. This makes the
    // enemy prefer destroying a block over merely hurting two of them.
    private const float ValueOfABlock = 4f;

    // Extra worth of the legendary block, so the enemy protects its own and hunts the player's.
    private const float ExtraValueOfALegend = 6f;

    // Moves scoring within this much of the best are treated as equally good, and one of
    // them is picked at random. It stops the enemy from playing the same way every game.
    private const float CloseEnoughToBest = 0.25f;

    private readonly System.Random _random;
    private readonly List<Vector2Int> _emptySquares = new List<Vector2Int>();
    private readonly List<Vector2Int> _spareSquares = new List<Vector2Int>();
    private readonly List<SandboxCubeKind> _kindsInHand = new List<SandboxCubeKind>();
    private readonly List<SandboxCubeKind> _bestKinds = new List<SandboxCubeKind>();
    private readonly List<Vector2Int> _bestSquares = new List<Vector2Int>();

    // Everything below is the "spare board" used for thinking. It is created once and
    // reused for every simulation, so planning a move creates no garbage in memory.
    private readonly SandboxBoard _spareBoard = new SandboxBoard(SandboxGridSize.MinimumColumns, SandboxGridSize.MinimumRows);
    private readonly SandboxBlock[] _spareBlocks = new SandboxBlock[SandboxGridSize.MaximumColumns * SandboxGridSize.MaximumRows];
    private readonly SandboxBlock _candidateBlock = new SandboxBlock();
    private readonly List<int> _visitOrder = new List<int>();
    private readonly HashSet<SandboxBlock> _blocksThatActed = new HashSet<SandboxBlock>();
    private readonly List<SandboxBlock> _destroyedBlocks = new List<SandboxBlock>();
    private readonly List<Vector2Int> _hitSquares = new List<Vector2Int>();

    /// <summary>Creates the opponent. It shares the screen's random number generator.</summary>
    public SandboxEnemy(System.Random random)
    {
        _random = random;
        for (int index = 0; index < _spareBlocks.Length; index++)
        {
            _spareBlocks[index] = new SandboxBlock();
        }
    }

    /// <summary>
    /// Chooses the best move it can find and plays it: takes the block out of the
    /// enemy's hand and puts it on the board. Returns the block, or null when the
    /// hand is empty or the board is full. "isRandomOrder" is the activation order
    /// currently selected, so the enemy plans with the same rule the turn will use.
    /// </summary>
    public SandboxBlock PlaceBlock(SandboxBoard board, SandboxHand hand, bool isRandomOrder)
    {
        board.CollectPositions(_emptySquares, true);
        if (_emptySquares.Count == 0 || hand.Count == 0)
        {
            return null;
        }

        FindBestMoves(board, hand, isRandomOrder);
        int choice = _random.Next(_bestKinds.Count);
        SandboxCubeKind kind = _bestKinds[choice];
        hand.Remove(kind);

        SandboxBlock block = new SandboxBlock(kind, SandboxSide.Enemy);
        board.Place(block, _bestSquares[choice]);
        return block;
    }

    /// <summary>Scores every possible move and keeps the ones that share the best score.</summary>
    private void FindBestMoves(SandboxBoard board, SandboxHand hand, bool isRandomOrder)
    {
        ListKindsInHand(hand);
        _bestKinds.Clear();
        _bestSquares.Clear();
        float bestScore = float.NegativeInfinity;

        foreach (SandboxCubeKind kind in _kindsInHand)
        {
            foreach (Vector2Int square in _emptySquares)
            {
                float score = ScoreMove(board, kind, square, isRandomOrder);
                if (score > bestScore + CloseEnoughToBest)
                {
                    // Clearly better than anything so far: forget the earlier moves.
                    bestScore = score;
                    _bestKinds.Clear();
                    _bestSquares.Clear();
                }

                if (score >= bestScore - CloseEnoughToBest)
                {
                    _bestKinds.Add(kind);
                    _bestSquares.Add(square);
                }
            }
        }
    }

    /// <summary>Two red blocks in the hand are the same move, so each kind is listed only once.</summary>
    private void ListKindsInHand(SandboxHand hand)
    {
        _kindsInHand.Clear();
        for (int index = 0; index < hand.Count; index++)
        {
            SandboxCubeKind kind = hand.GetBlock(index);
            if (!_kindsInHand.Contains(kind))
            {
                _kindsInHand.Add(kind);
            }
        }
    }

    /// <summary>
    /// Plays one move out several times on the spare board and returns the average
    /// score of the boards that result.
    /// </summary>
    private float ScoreMove(SandboxBoard board, SandboxCubeKind kind, Vector2Int square, bool isRandomOrder)
    {
        float total = 0f;
        for (int simulation = 0; simulation < SimulationsPerMove; simulation++)
        {
            _spareBoard.CopyFrom(board, _spareBlocks);
            _candidateBlock.Reset(kind, SandboxSide.Enemy);
            _spareBoard.Place(_candidateBlock, square);

            SimulateActivation(isRandomOrder);
            total += ScoreBoard(_spareBoard);
        }

        return total / SimulationsPerMove;
    }

    /// <summary>The same activation the screen runs, without pictures, sounds, or pauses.</summary>
    private void SimulateActivation(bool isRandomOrder)
    {
        SandboxActivation.BuildVisitOrder(_spareBoard, _random, isRandomOrder, _visitOrder, _spareSquares);
        _blocksThatActed.Clear();
        _destroyedBlocks.Clear();

        foreach (int index in _visitOrder)
        {
            SandboxBlock block = _spareBoard.GetBlock(SandboxActivation.SquareAt(_spareBoard, index));
            if (block != null && _blocksThatActed.Add(block))
            {
                SandboxCombat.Activate(_spareBoard, block, _destroyedBlocks, _hitSquares);
            }
        }
    }

    /// <summary>
    /// How good a board is for the enemy: the worth of its own blocks minus the
    /// worth of the player's. Higher is better for the enemy.
    /// </summary>
    private static float ScoreBoard(SandboxBoard board)
    {
        float score = 0f;
        for (int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++)
            {
                SandboxBlock block = board.GetBlock(new Vector2Int(column, row));
                if (block != null)
                {
                    float worth = ValueOfABlock + block.Hp;
                    if (block.Kind.Ability == SandboxAbility.Legend)
                    {
                        worth += ExtraValueOfALegend;
                    }

                    score += block.Side == SandboxSide.Enemy ? worth : -worth;
                }
            }
        }

        return score;
    }
}
