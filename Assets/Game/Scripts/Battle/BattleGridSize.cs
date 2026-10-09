using System;

/// <summary>
/// The size of the battle grid, kept inside its allowed limits.
/// Change the constants below to change how small or large the grid may be.
/// </summary>
public class BattleGridSize
{
    /// <summary>The grid can never be narrower than this.</summary>
    public const int MinimumColumns = 5;

    /// <summary>The grid can never be shorter than this.</summary>
    public const int MinimumRows = 4;

    /// <summary>The widest grid that still fits comfortably on screen.</summary>
    public const int MaximumColumns = 12;

    /// <summary>The tallest grid that still fits comfortably on screen.</summary>
    public const int MaximumRows = 8;

    /// <summary>How many cells wide the grid is.</summary>
    public int Columns { get; private set; }

    /// <summary>How many cells tall the grid is.</summary>
    public int Rows { get; private set; }

    /// <summary>Creates a grid size. Values outside the limits are pulled back inside them.</summary>
    public BattleGridSize(int columns, int rows)
    {
        Columns = Math.Clamp(columns, MinimumColumns, MaximumColumns);
        Rows = Math.Clamp(rows, MinimumRows, MaximumRows);
    }

    /// <summary>Adds (or removes, if negative) columns, staying inside the limits.</summary>
    public void ChangeColumns(int amount)
    {
        Columns = Math.Clamp(Columns + amount, MinimumColumns, MaximumColumns);
    }

    /// <summary>Adds (or removes, if negative) rows, staying inside the limits.</summary>
    public void ChangeRows(int amount)
    {
        Rows = Math.Clamp(Rows + amount, MinimumRows, MaximumRows);
    }
}
