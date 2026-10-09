using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// The board: a grid of squares, each empty or holding one block.
/// This class only stores where blocks are. The fighting rules are in BattleCombat.
/// </summary>
public class BattleBoard
{
    // Storage is always the largest allowed size, so resizing never has to copy blocks.
    private readonly BattleBlock[,] _blocks =
        new BattleBlock[BattleGridSize.MaximumColumns, BattleGridSize.MaximumRows];

    /// <summary>How many squares wide the board is right now.</summary>
    public int Columns { get; private set; }

    /// <summary>How many squares tall the board is right now.</summary>
    public int Rows { get; private set; }

    /// <summary>Creates an empty board of the given size.</summary>
    public BattleBoard(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }

    /// <summary>True when the position is a square of the board.</summary>
    public bool IsInside(Vector2Int position)
    {
        return position.x >= 0 && position.x < Columns && position.y >= 0 && position.y < Rows;
    }

    /// <summary>The block at a position, or null if the square is empty or off the board.</summary>
    public BattleBlock GetBlock(Vector2Int position)
    {
        return IsInside(position) ? _blocks[position.x, position.y] : null;
    }

    /// <summary>Puts a block on an empty square.</summary>
    public void Place(BattleBlock block, Vector2Int position)
    {
        _blocks[position.x, position.y] = block;
        block.Position = position;
    }

    /// <summary>Takes a block off the board.</summary>
    public void Remove(BattleBlock block)
    {
        _blocks[block.Position.x, block.Position.y] = null;
    }

    /// <summary>Moves a block to another empty square.</summary>
    public void Move(BattleBlock block, Vector2Int position)
    {
        Remove(block);
        Place(block, position);
    }

    /// <summary>
    /// Makes this board an exact copy of another one, for planning moves without
    /// touching the real board. The copied blocks are taken from "spareBlocks"
    /// (one per square of the largest board) instead of being newly created.
    /// </summary>
    public void CopyFrom(BattleBoard source, BattleBlock[] spareBlocks)
    {
        Clear();
        Columns = source.Columns;
        Rows = source.Rows;

        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                BattleBlock original = source._blocks[x, y];
                if (original != null)
                {
                    BattleBlock copy = spareBlocks[y * BattleGridSize.MaximumColumns + x];
                    copy.CopyFrom(original);
                    _blocks[x, y] = copy;
                }
            }
        }
    }

    /// <summary>Removes every block.</summary>
    public void Clear()
    {
        System.Array.Clear(_blocks, 0, _blocks.Length);
    }

    /// <summary>
    /// Changes the board size. Blocks left outside the new size are removed.
    /// They do not count as destroyed in battle.
    /// </summary>
    public void Resize(int columns, int rows)
    {
        for (int x = 0; x < BattleGridSize.MaximumColumns; x++)
        {
            for (int y = 0; y < BattleGridSize.MaximumRows; y++)
            {
                if (x >= columns || y >= rows)
                {
                    _blocks[x, y] = null;
                }
            }
        }

        Columns = columns;
        Rows = rows;
    }

    /// <summary>
    /// Describes the whole board as one line of text: its size and, for every
    /// square, which block is on it and how much HP that block has. Two boards
    /// that look exactly the same give exactly the same text, which is how a
    /// repeated position is recognised.
    /// </summary>
    public string DescribePosition(StringBuilder text)
    {
        text.Clear();
        text.Append(Columns).Append('x').Append(Rows).Append(':');
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                BattleBlock block = _blocks[x, y];
                if (block == null)
                {
                    text.Append('.');
                }
                else
                {
                    text.Append(block.Side == BattleSide.Player ? 'P' : 'E').Append(block.Kind.Name).Append(block.Hp);
                }

                text.Append('|');
            }
        }

        return text.ToString();
    }

    /// <summary>Counts the blocks of one side.</summary>
    public int CountBlocks(BattleSide side)
    {
        int count = 0;
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                if (_blocks[x, y] != null && _blocks[x, y].Side == side)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Fills the list with every empty square, or with every square that holds
    /// a block. The caller passes the list in so no new list is created each turn.
    /// </summary>
    public void CollectPositions(List<Vector2Int> positions, bool wantEmptySquares)
    {
        positions.Clear();
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                bool isEmpty = _blocks[x, y] == null;
                if (isEmpty == wantEmptySquares)
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
        }
    }
}
