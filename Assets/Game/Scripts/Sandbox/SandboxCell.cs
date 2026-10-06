using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// One square of the sandbox grid. It can hold a single cube.
/// </summary>
public class SandboxCell : MonoBehaviour
{
    /// <summary>The cube sitting in this cell, or null when the cell is empty.</summary>
    public SandboxCube Occupant { get; private set; }

    /// <summary>Where this cell is in the grid: x is the column, y is the row.</summary>
    public Vector2Int GridPosition { get; private set; }

    /// <summary>Creates an empty cell at the given grid position.</summary>
    public static SandboxCell Create(Transform parent, Vector2Int gridPosition, Color color)
    {
        GameObject cellObject = new GameObject($"Cell {gridPosition.x},{gridPosition.y}", typeof(RectTransform));
        cellObject.transform.SetParent(parent, false);

        // The image is also what the mouse "hits" when a cube is dropped here.
        cellObject.AddComponent<UnityEngine.UI.Image>().color = color;

        SandboxCell cell = cellObject.AddComponent<SandboxCell>();
        cell.GridPosition = gridPosition;
        return cell;
    }

    /// <summary>Puts a cube in this cell. A cube that was already here is replaced.</summary>
    public void Place(SandboxCube cube)
    {
        if (Occupant != null && Occupant != cube)
        {
            Destroy(Occupant.gameObject);
        }

        Occupant = cube;
        cube.SnapInto(this);
    }

    /// <summary>Marks the cell as empty, for when its cube is picked up.</summary>
    public void Clear()
    {
        Occupant = null;
    }
}
