using System.Collections.Generic;

/// <summary>
/// The player's money and the shop that opens after each battle.
/// Every enemy block the player destroys pays 1 dollar. The shop offers a few
/// random blocks at random prices. A bought block is kept aside and joins the
/// player's hand at the start of the next battle.
/// Plain C# with no screen code, so the prices and rules are in one place.
/// </summary>
public class BattleShop
{
    /// <summary>Dollars paid for each enemy block the player destroys.</summary>
    public const int DollarsPerKill = 1;

    /// <summary>How many blocks the shop puts on sale each time it opens.</summary>
    public const int OfferCount = 4;

    // A block's price is picked at random between these two numbers, both included.
    // The legendary orange block is rare in the shop too (see BattleCubeKind.PickRandom) and costs more.
    private const int CheapestCommonPrice = 2;
    private const int DearestCommonPrice = 4;
    private const int CheapestLegendaryPrice = 6;
    private const int DearestLegendaryPrice = 8;

    private readonly BattleCubeKind[] _offerKinds = new BattleCubeKind[OfferCount];
    private readonly int[] _offerPrices = new int[OfferCount];
    private readonly bool[] _offerSold = new bool[OfferCount];
    private readonly List<BattleCubeKind> _boughtBlocks = new List<BattleCubeKind>();

    /// <summary>The dollars the player has now.</summary>
    public int Money { get; private set; }

    /// <summary>How many bought blocks are waiting to join the hand in the next battle.</summary>
    public int BoughtCount => _boughtBlocks.Count;

    /// <summary>The kind of block on sale in a slot, counting from 0. Null if the slot is empty.</summary>
    public BattleCubeKind GetOfferKind(int index)
    {
        return _offerKinds[index];
    }

    /// <summary>The price of the block in a slot.</summary>
    public int GetOfferPrice(int index)
    {
        return _offerPrices[index];
    }

    /// <summary>True once the block in a slot has been bought.</summary>
    public bool IsOfferSold(int index)
    {
        return _offerSold[index];
    }

    /// <summary>A bought block that is waiting for the next battle, counting from 0.</summary>
    public BattleCubeKind GetBoughtBlock(int index)
    {
        return _boughtBlocks[index];
    }

    /// <summary>Pays the player for destroying one enemy block.</summary>
    public void EarnForKill()
    {
        Money += DollarsPerKill;
    }

    /// <summary>Fills every slot with a new random block at a new random price.</summary>
    public void Restock(System.Random random)
    {
        for (int index = 0; index < OfferCount; index++)
        {
            BattleCubeKind kind = BattleCubeKind.PickRandom(random);
            bool isLegendary = kind.Ability == BattleAbility.Legend;
            int cheapest = isLegendary ? CheapestLegendaryPrice : CheapestCommonPrice;
            int dearest = isLegendary ? DearestLegendaryPrice : DearestCommonPrice;

            _offerKinds[index] = kind;

            // Next's upper limit is left out, so 1 is added to include the dearest price.
            _offerPrices[index] = random.Next(cheapest, dearest + 1);
            _offerSold[index] = false;
        }
    }

    /// <summary>True if the block in a slot is still for sale and the player has enough money for it.</summary>
    public bool CanBuy(int index)
    {
        return _offerKinds[index] != null && !_offerSold[index] && Money >= _offerPrices[index];
    }

    /// <summary>
    /// Buys the block in a slot: takes the money and keeps the block for the next battle.
    /// Returns false, and changes nothing, if it cannot be bought.
    /// </summary>
    public bool Buy(int index)
    {
        if (!CanBuy(index))
        {
            return false;
        }

        Money -= _offerPrices[index];
        _offerSold[index] = true;
        _boughtBlocks.Add(_offerKinds[index]);
        return true;
    }

    /// <summary>Puts every bought block into the hand. Called when a new battle is dealt.</summary>
    public void MoveBoughtBlocksTo(BattleHand hand)
    {
        foreach (BattleCubeKind kind in _boughtBlocks)
        {
            hand.Add(kind);
        }

        _boughtBlocks.Clear();
    }

    /// <summary>Sets the money directly, for a saved battle. Never below 0.</summary>
    public void RestoreMoney(int money)
    {
        Money = money < 0 ? 0 : money;
    }

    /// <summary>Puts one slot back as it was in a saved battle.</summary>
    public void RestoreOffer(int index, BattleCubeKind kind, int price, bool isSold)
    {
        _offerKinds[index] = kind;
        _offerPrices[index] = price;
        _offerSold[index] = isSold;
    }

    /// <summary>Forgets the bought blocks, before they are read back from a saved battle.</summary>
    public void ClearBoughtBlocks()
    {
        _boughtBlocks.Clear();
    }

    /// <summary>Adds one bought block read back from a saved battle.</summary>
    public void RestoreBoughtBlock(BattleCubeKind kind)
    {
        _boughtBlocks.Add(kind);
    }
}
