using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The fighting rules of the battle: what each ability does to the board.
/// Plain C# with no screen code, so the rules are in one place and easy to change.
/// Both sides follow the same rules; "opposing" means a block of the other side.
/// </summary>
public static class BattleCombat
{
    /// <summary>HP a Strike removes from each opposing block next to it.</summary>
    public const int StrikeDamage = 2;

    /// <summary>HP a Push removes when the pushed block has another block behind it.</summary>
    public const int BlockedPushDamage = 1;

    /// <summary>HP a Heal gives to each friendly block next to it.</summary>
    public const int HealAmount = 2;

    /// <summary>HP a Row Shot removes from each opposing block in the row.</summary>
    public const int RowShotDamage = 1;

    /// <summary>HP the Legend removes from every opposing block on the board.</summary>
    public const int LegendDamage = 1;

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
    public static BattleOutcome GetOutcome(BattleBoard board, int playerHandCount, int enemyHandCount)
    {
        bool playerIsOut = playerHandCount == 0 && board.CountBlocks(BattleSide.Player) == 0;
        bool enemyIsOut = enemyHandCount == 0 && board.CountBlocks(BattleSide.Enemy) == 0;

        if (playerIsOut && enemyIsOut)
        {
            return BattleOutcome.Draw;
        }

        if (playerIsOut)
        {
            return BattleOutcome.EnemyWins;
        }

        return enemyIsOut ? BattleOutcome.PlayerWins : BattleOutcome.Undecided;
    }

    /// <summary>
    /// Makes one block use its ability. Blocks destroyed by it are taken off
    /// the board and added to the "destroyed" list so the caller can count them.
    /// "attackedSquares" is filled with the square of every block it attacked
    /// (where that block stood when it was hit), so the caller can animate the hits.
    /// Returns true if the ability did anything: hit, pushed, or healed at least one block.
    /// </summary>
    public static bool Activate(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        attackedSquares.Clear();
        return UseAbility(board, block, destroyed, attackedSquares);
    }

    /// <summary>
    /// Runs one block's ability and adds its hits to the list without clearing it,
    /// so the Legend can run its friends' abilities and collect all their hits together.
    /// Returns true if the ability did anything.
    /// </summary>
    private static bool UseAbility(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        int hitsBefore = attackedSquares.Count;
        switch (block.Kind.Ability)
        {
            case BattleAbility.Strike:
                StrikeNeighbours(board, block, destroyed, attackedSquares);
                break;
            case BattleAbility.Push:
                PushNeighbours(board, block, destroyed, attackedSquares);
                break;
            case BattleAbility.Heal:
                return HealNeighbours(board, block);
            case BattleAbility.RowShot:
                ShootRow(board, block, destroyed, attackedSquares);
                break;
            case BattleAbility.Legend:
                bool friendsDidSomething = RetriggerNeighbours(board, block, destroyed, attackedSquares);
                HitEveryOpponent(board, block, destroyed, attackedSquares);
                return friendsDidSomething || attackedSquares.Count > hitsBefore;
        }

        return attackedSquares.Count > hitsBefore;
    }

    /// <summary>
    /// Makes every friendly block next to the Legend use its ability again.
    /// Another Legend is skipped: two of them side by side would otherwise
    /// trigger each other forever.
    /// </summary>
    private static bool RetriggerNeighbours(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        bool anyDidSomething = false;
        foreach (Vector2Int direction in Directions)
        {
            BattleBlock friend = board.GetBlock(block.Position + direction);
            bool canRetrigger = friend != null && friend.Side == block.Side
                && friend.Kind.Ability != BattleAbility.Legend;
            if (canRetrigger)
            {
                anyDidSomething |= UseAbility(board, friend, destroyed, attackedSquares);
            }
        }

        return anyDidSomething;
    }

    private static void HitEveryOpponent(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        for (int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++)
            {
                BattleBlock target = board.GetBlock(new Vector2Int(column, row));
                if (IsOpponent(block, target))
                {
                    attackedSquares.Add(target.Position);
                    Damage(board, target, LegendDamage, destroyed);
                }
            }
        }
    }

    /// <summary>
    /// Lists the squares a block's ability can reach, whether or not anything is
    /// standing on them. "Attack squares" are where it hurts or pushes opposing
    /// blocks. "Other squares" are everything else it affects: where a heal lands,
    /// or where a pushed block would end up. Squares off the board are left out.
    /// Keep this in step with the ability methods below.
    /// </summary>
    public static void CollectAreaOfEffect(BattleBoard board, BattleBlock block,
        List<Vector2Int> attackSquares, List<Vector2Int> otherSquares)
    {
        attackSquares.Clear();
        otherSquares.Clear();

        switch (block.Kind.Ability)
        {
            case BattleAbility.Strike:
                AddNeighbours(board, block.Position, 1, attackSquares);
                break;
            case BattleAbility.Push:
                AddNeighbours(board, block.Position, 1, attackSquares);
                AddNeighbours(board, block.Position, 2, otherSquares);
                break;
            case BattleAbility.Heal:
                AddNeighbours(board, block.Position, 1, otherSquares);
                break;
            case BattleAbility.RowShot:
                AddRow(board, block.Position, attackSquares);
                break;
            case BattleAbility.Legend:
                AddNeighbours(board, block.Position, 1, otherSquares);
                AddEverySquareExcept(board, block.Position, otherSquares, attackSquares);
                break;
        }
    }

    /// <summary>Adds the whole board, leaving out the block's own square and the squares already listed.</summary>
    private static void AddEverySquareExcept(BattleBoard board, Vector2Int own, List<Vector2Int> alreadyListed,
        List<Vector2Int> squares)
    {
        for (int row = 0; row < board.Rows; row++)
        {
            for (int column = 0; column < board.Columns; column++)
            {
                Vector2Int square = new Vector2Int(column, row);
                if (square != own && !alreadyListed.Contains(square))
                {
                    squares.Add(square);
                }
            }
        }
    }

    /// <summary>Adds the squares that are a given number of steps away in each of the four directions.</summary>
    private static void AddNeighbours(BattleBoard board, Vector2Int center, int distance, List<Vector2Int> squares)
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

    private static void AddRow(BattleBoard board, Vector2Int center, List<Vector2Int> squares)
    {
        for (int column = 0; column < board.Columns; column++)
        {
            if (column != center.x)
            {
                squares.Add(new Vector2Int(column, center.y));
            }
        }
    }

    private static void StrikeNeighbours(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        foreach (Vector2Int direction in Directions)
        {
            BattleBlock target = board.GetBlock(block.Position + direction);
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
    private static void PushNeighbours(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        foreach (Vector2Int direction in Directions)
        {
            BattleBlock target = board.GetBlock(block.Position + direction);
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
    private static bool HealNeighbours(BattleBoard board, BattleBlock block)
    {
        bool healedSomeone = false;
        foreach (Vector2Int direction in Directions)
        {
            BattleBlock target = board.GetBlock(block.Position + direction);
            if (target != null && target.Side == block.Side)
            {
                int hpBefore = target.Hp;
                target.Heal(HealAmount);
                healedSomeone |= target.Hp > hpBefore;
            }
        }

        return healedSomeone;
    }

    private static void ShootRow(BattleBoard board, BattleBlock block, List<BattleBlock> destroyed,
        List<Vector2Int> attackedSquares)
    {
        for (int column = 0; column < board.Columns; column++)
        {
            BattleBlock target = board.GetBlock(new Vector2Int(column, block.Position.y));
            if (IsOpponent(block, target))
            {
                attackedSquares.Add(target.Position);
                Damage(board, target, RowShotDamage, destroyed);
            }
        }
    }

    private static bool IsOpponent(BattleBlock block, BattleBlock other)
    {
        return other != null && other.Side != block.Side;
    }

    private static void Damage(BattleBoard board, BattleBlock target, int amount, List<BattleBlock> destroyed)
    {
        target.TakeDamage(amount);
        RemoveDestroyed(board, target, destroyed);
    }

    private static void RemoveDestroyed(BattleBoard board, BattleBlock target, List<BattleBlock> destroyed)
    {
        if (!target.IsAlive)
        {
            board.Remove(target);
            destroyed.Add(target);
        }
    }
}
