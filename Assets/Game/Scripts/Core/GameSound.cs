/// <summary>
/// Every sound effect the game can play. Each name is also the name of its
/// sound file (for example ButtonClick plays "ButtonClick.wav"), so adding a
/// sound means adding a name here and a file with the same name.
/// </summary>
public enum GameSound
{
    /// <summary>The mouse moves onto a button.</summary>
    ButtonHover,

    /// <summary>A button is pressed.</summary>
    ButtonClick,

    /// <summary>The player leaves the title screen.</summary>
    TitleStart,

    /// <summary>The player picks a block up from their hand.</summary>
    BlockPickUp,

    /// <summary>The player puts a block on the grid.</summary>
    BlockPlace,

    /// <summary>The enemy puts a block on the grid.</summary>
    EnemyPlace,

    /// <summary>The player tries something that is not allowed.</summary>
    InvalidMove,

    /// <summary>New hands are dealt at the start of a battle.</summary>
    DealHands,

    /// <summary>A block strikes the blocks next to it.</summary>
    Strike,

    /// <summary>A block pushes the blocks next to it.</summary>
    Push,

    /// <summary>A block shoots along its row.</summary>
    RowShot,

    /// <summary>A block heals its friends.</summary>
    Heal,

    /// <summary>A block is destroyed.</summary>
    BlockDestroyed,

    /// <summary>The player wins a battle.</summary>
    Win,

    /// <summary>The player loses a battle.</summary>
    Lose,

    /// <summary>A battle ends in a draw.</summary>
    Draw
}
