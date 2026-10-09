using System.Collections.Generic;

/// <summary>
/// The blocks one side still has to place. Each battle a hand is filled
/// with random blocks, and a block leaves the hand when it is put on the grid.
/// </summary>
public class BattleHand
{
    /// <summary>How many blocks each side gets at the start of a battle.</summary>
    public const int StartingSize = 10;

    private readonly List<BattleCubeKind> _blocks = new List<BattleCubeKind>();

    /// <summary>How many blocks are left in the hand.</summary>
    public int Count => _blocks.Count;

    /// <summary>The block at a position in the hand, counting from 0.</summary>
    public BattleCubeKind GetBlock(int index)
    {
        return _blocks[index];
    }

    /// <summary>Throws the old hand away and deals a new one of random blocks.</summary>
    public void Deal(System.Random random)
    {
        _blocks.Clear();
        for (int count = 0; count < StartingSize; count++)
        {
            _blocks.Add(BattleCubeKind.PickRandom(random));
        }
    }

    /// <summary>Empties the hand.</summary>
    public void Clear()
    {
        _blocks.Clear();
    }

    /// <summary>Puts one block of the given kind into the hand.</summary>
    public void Add(BattleCubeKind kind)
    {
        _blocks.Add(kind);
    }

    /// <summary>Takes one block of the given kind out of the hand. Returns false if there is none.</summary>
    public bool Remove(BattleCubeKind kind)
    {
        return _blocks.Remove(kind);
    }

    /// <summary>Takes a random block out of the hand and returns it. The hand must not be empty.</summary>
    public BattleCubeKind RemoveRandom(System.Random random)
    {
        int index = random.Next(_blocks.Count);
        BattleCubeKind kind = _blocks[index];
        _blocks.RemoveAt(index);
        return kind;
    }
}
