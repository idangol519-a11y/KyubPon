/// <summary>
/// The player's items in a battle, shown in the item library on the left.
/// There are two:
///   The potion is used once per battle. For the turn it is used on, the
///   player's red blocks hit harder.
///   The gem is a trinket: it is never used up. For the whole battle it lets the
///   player drag their blue blocks to an empty square before placing a block.
/// Plain C# with no screen code. The enemy has no items.
/// </summary>
public class BattleItems
{
    /// <summary>Extra HP the player's red blocks remove with each hit while the potion is active.</summary>
    public const int PotionStrikeBonus = 1;

    /// <summary>The ability the potion makes stronger. Red blocks have it.</summary>
    public const BattleAbility PotionBoostedAbility = BattleAbility.Strike;

    /// <summary>The ability of the blocks the gem lets the player move. Blue blocks have it.</summary>
    public const BattleAbility GemMovableAbility = BattleAbility.Push;

    /// <summary>True once the potion has been used in this battle. It cannot be used again.</summary>
    public bool IsPotionUsed { get; private set; }

    /// <summary>True from the moment the potion is used until that turn has finished.</summary>
    public bool IsPotionActive { get; private set; }

    /// <summary>The extra damage the player's red blocks do right now: the bonus while the potion is active, otherwise 0.</summary>
    public int PlayerStrikeBonus => IsPotionActive ? PotionStrikeBonus : 0;

    /// <summary>True if the gem lets the player move this block: it must be one of the player's own blue blocks.</summary>
    public static bool CanBeMoved(BattleBlock block)
    {
        return block != null && block.Side == BattleSide.Player && block.Kind.Ability == GemMovableAbility;
    }

    /// <summary>True if the potion makes this block stronger right now.</summary>
    public bool IsBoosted(BattleBlock block)
    {
        return IsPotionActive && block != null && block.Side == BattleSide.Player
            && block.Kind.Ability == PotionBoostedAbility;
    }

    /// <summary>Gives the potion back, for the start of a new battle.</summary>
    public void Reset()
    {
        IsPotionUsed = false;
        IsPotionActive = false;
    }

    /// <summary>Uses the potion for the current turn. Returns false if it was already used.</summary>
    public bool UsePotion()
    {
        if (IsPotionUsed)
        {
            return false;
        }

        IsPotionUsed = true;
        IsPotionActive = true;
        return true;
    }

    /// <summary>Called when a turn has finished. The potion's effect ends here.</summary>
    public void EndTurn()
    {
        IsPotionActive = false;
    }

    /// <summary>Puts the items back as they were in a saved battle.</summary>
    public void Restore(bool isPotionUsed, bool isPotionActive)
    {
        IsPotionUsed = isPotionUsed;

        // A potion that was never used cannot be active, whatever the save file says.
        IsPotionActive = isPotionUsed && isPotionActive;
    }
}
