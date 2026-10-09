using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// The fighting rules of the sandbox: what each ability does to the board.
/// Plain C# with no screen code, so the rules are in one place and easy to change.
/// Both sides follow the same rules; "opposing" means a block of the other side.
/// </summary>
public static class SandboxCombat
{
    /// <summary>HP a Strike removes from each opposing block next to it.</summary>
    public const int StrikeDamage = 2;

    /// <summary>HP a Push removes when the pushed block has another block behind it.</summary>
    public const int BlockedPushDamage = 1;

    /// <summary>HP a Heal gives to each friendly block next to it.</summary>
    public const int HealAmount = 2;

    /// <summary>HP a Row Shot removes from each opposing block in the row.</summary>
    public const int RowShotDamage = 1;

    /// <summary>The four squares next to a block: up, down, left, right.</summary>
    public static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// Says how the battle stands. A side is out when it has no blocks on the
    /// board and none left in its hand. One side out means the other wins;
    /// both out at once is a draw.
    /// </summary>
    public static SandboxOutcome GetOutcome(SandboxBoard board, int playerHandCount, int enemyHandCount)
    {
        bool playerIsOut = playerHandCount == 0 && board.CountBlocks(SandboxSide.Player) == 0;
        bool enemyIsOut = enemyHandCount == 0 && board.CountBlocks(SandboxSide.Enemy) == 0;

        if (playerIsOut && enemyIsOut)
        {
            return SandboxOutcome.Draw;
        }

        if (playerIsOut)
        {
            return SandboxOutcome.EnemyWins;
        }

        return enemyIsOut ? SandboxOutcome.PlayerWins : SandboxOutcome.Undecided;
    }

    /// <summary>
    /// Makes one block use its ability. Blocks destroyed by it are taken off
    /// the board and added to the "destroyed" list so the caller can count them.
    /// "attackedSquares" is filled with the square of every block it attacked
    /// (where that block stood when it was hit), so the caller can animate the hits.
    /// Returns true if the ability did anything: hit, pushed, or healed at least one block.
    /// </summary>
    public static bool Activate(SandboxBoard board, SandboxBlock block, List<SandboxBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        attackedSquares.Clear();
        switch (block.Kind.Ability)
        {
            case SandboxAbility.Strike:
                StrikeNeighbours(board, block, destroyed, attackedSquares);
                break;
            case SandboxAbility.Push:
                PushNeighbours(board, block, destroyed, attackedSquares);
                break;
            case SandboxAbility.Heal:
                return HealNeighbours(board, block);
            case SandboxAbility.RowShot:
                ShootRow(board, block, destroyed, attackedSquares);
                break;
        }

        return attackedSquares.Count > 0;
    }

    /// <summary>
    /// Lists the squares a block's ability can reach, whether or not anything is
    /// standing on them. "Attack squares" are where it hurts or pushes opposing
    /// blocks. "Other squares" are everything else it affects: where a heal lands,
    /// or where a pushed block would end up. Squares off the board are left out.
    /// Keep this in step with the ability methods below.
    /// </summary>
    public static void CollectAreaOfEffect(SandboxBoard board, SandboxBlock block,
        List<Vector2Int> attackSquares, List<Vector2Int> otherSquares)
    {
        attackSquares.Clear();
        otherSquares.Clear();

        switch (block.Kind.Ability)
        {
            case SandboxAbility.Strike:
                AddNeighbours(board, block.Position, 1, attackSquares);
                break;
            case SandboxAbility.Push:
                AddNeighbours(board, block.Position, 1, attackSquares);
                AddNeighbours(board, block.Position, 2, otherSquares);
                break;
            case SandboxAbility.Heal:
                AddNeighbours(board, block.Position, 1, otherSquares);
                break;
            case SandboxAbility.RowShot:
                AddRow(board, block.Position, attackSquares);
                break;
        }
    }

    /// <summary>Adds the squares that are a given number of steps away in each of the four directions.</summary>
    private static void AddNeighbours(SandboxBoard board, Vector2Int center, int distance, List<Vector2Int> squares)
    {
        foreach (Vector2Int direction in Directions)
        {
            Vector2Int square = center + direction * distance;
            if (board.IsInside(square))
            {
                squares.Add(square);
            }
        }
    }

    private static void AddRow(SandboxBoard board, Vector2Int center, List<Vector2Int> squares)
    {
        for (int column = 0; column < board.Columns; column++)
        {
            if (column != center.x)
            {
                squares.Add(new Vector2Int(column, center.y));
            }
        }
    }

    private static void StrikeNeighbours(SandboxBoard board, SandboxBlock block, List<SandboxBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        foreach (Vector2Int direction in Directions)
        {
            SandboxBlock target = board.GetBlock(block.Position + direction);
            if (IsOpponent(block, target))
            {
                attackedSquares.Add(target.Position);
                Damage(board, target, StrikeDamage, destroyed);
            }
        }
    }

    /// <summary>
    /// Each opposing neighbour is shoved one square further away. If that square is
    /// off the board the block is destroyed; if another block is in the way,
    /// the pushed block is hurt instead of moving.
    /// </summary>
    private static void PushNeighbours(SandboxBoard board, SandboxBlock block, List<SandboxBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        foreach (Vector2Int direction in Directions)
        {
            SandboxBlock target = board.GetBlock(block.Position + direction);
            if (!IsOpponent(block, target))
            {
                continue;
            }

            attackedSquares.Add(target.Position);
            Vector2Int destination = target.Position + direction;
            if (!board.IsInside(destination))
            {
                target.Destroy();
                RemoveDestroyed(board, target, destroyed);
            }
            else if (board.GetBlock(destination) == null)
            {
                board.Move(target, destination);
            }
            else
            {
                Damage(board, target, BlockedPushDamage, destroyed);
            }
        }
    }

    /// <summary>Heals friendly neighbours. Returns true if any of them actually gained HP.</summary>
    private static bool HealNeighbours(SandboxBoard board, SandboxBlock block)
    {
        bool healedSomeone = false;
        foreach (Vector2Int direction in Directions)
        {
            SandboxBlock target = board.GetBlock(block.Position + direction);
            if (target != null && target.Side == block.Side)
            {
                int hpBefore = target.Hp;
                target.Heal(HealAmount);
                healedSomeone |= target.Hp > hpBefore;
            }
        }

        return healedSomeone;
    }

    private static void ShootRow(SandboxBoard board, SandboxBlock block, List<SandboxBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        for (int column = 0; column < board.Columns; column++)
        {
            SandboxBlock target = board.GetBlock(new Vector2Int(column, block.Position.y));
            if (IsOpponent(block, target))
            {
                attackedSquares.Add(target.Position);
                Damage(board, target, RowShotDamage, destroyed);
            }
        }
    }

    private static bool IsOpponent(SandboxBlock block, SandboxBlock other)
    {
        return other != null && other.Side != block.Side;
    }

    private static void Damage(SandboxBoard board, SandboxBlock target, int amount, List<SandboxBlock> destroyed)
    {
        target.TakeDamage(amount);
        RemoveDestroyed(board, target, destroyed);
    }

    private static void RemoveDestroyed(SandboxBoard board, SandboxBlock target, List<SandboxBlock> destroyed)
    {
        if (!target.IsAlive)
        {
            board.Remove(target);
            destroyed.Add(target);
        }
    }
}
