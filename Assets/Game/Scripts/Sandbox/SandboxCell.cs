using UnityEngine;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// One square of the sandbox grid on screen. It draws whatever block the board
/// says is on it: the block's color, its HP, and an X if it is an enemy block.
/// </summary>
public class SandboxCell : MonoBehaviour
{
    // How far the block picture stays from the edges of the square.
    private const float BlockInset = 8f;
    private const int SmallestTextSize = 8;
    private const int LargestTextSize = 72;

    // How much an enemy block's color is darkened, so the two sides are easy to tell apart.
    private const float EnemyDarkening = 0.35f;

    // The X takes the top part of the block and the HP number the bottom part.
    private const float SplitHeight = 0.42f;

    private static readonly Color EnemyMarkColor = new Color(0f, 0f, 0f, 0.75f);

    private UnityEngine.UI.Image _squareImage;
    private UnityEngine.UI.Image _blockImage;
    private UnityEngine.UI.Text _enemyMark;
    private UnityEngine.UI.Text _hpText;
    private Color _normalColor;

    /// <summary>Where this square is in the grid: x is the column, y is the row.</summary>
    public Vector2Int GridPosition { get; private set; }

    /// <summary>Creates an empty square at the given grid position.</summary>
    public static SandboxCell Create(Transform parent, Vector2Int gridPosition, Color color, Font font)
    {
        GameObject cellObject = new GameObject($"Cell {gridPosition.x},{gridPosition.y}", typeof(RectTransform));
        cellObject.transform.SetParent(parent, false);

        SandboxCell cell = cellObject.AddComponent<SandboxCell>();
        cell.GridPosition = gridPosition;
        cell._normalColor = color;

        // The square's image is also what the mouse "hits" when a block is dropped here.
        cell._squareImage = cellObject.AddComponent<UnityEngine.UI.Image>();
        cell._squareImage.color = color;
        cell.CreateBlockPicture(font);
        cell.Show(null);
        return cell;
    }

    /// <summary>Lights the square up while it is being activated, or returns it to normal.</summary>
    public void SetHighlight(bool isOn, Color highlightColor)
    {
        _squareImage.color = isOn ? highlightColor : _normalColor;
    }

    /// <summary>Draws the given block on this square. Pass null for an empty square.</summary>
    public void Show(SandboxBlock block)
    {
        _blockImage.gameObject.SetActive(block != null);
        if (block == null)
        {
            return;
        }

        bool isEnemy = block.Side == SandboxSide.Enemy;
        _blockImage.color = isEnemy ? Color.Lerp(block.Kind.Color, Color.black, EnemyDarkening) : block.Kind.Color;
        _enemyMark.enabled = isEnemy;
        _hpText.text = block.Hp.ToString();
    }

    private void CreateBlockPicture(Font font)
    {
        RectTransform blockRect = CreateChild(transform, "Block", Vector2.zero, Vector2.one);
        blockRect.offsetMin = new Vector2(BlockInset, BlockInset);
        blockRect.offsetMax = new Vector2(-BlockInset, -BlockInset);
        _blockImage = blockRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        _blockImage.raycastTarget = false;

        _enemyMark = CreateText(blockRect, "EnemyMark", font, new Vector2(0f, SplitHeight), Vector2.one, EnemyMarkColor);
        _enemyMark.text = "X";
        _hpText = CreateText(blockRect, "Hp", font, Vector2.zero, new Vector2(1f, SplitHeight), Color.white);
    }

    /// <summary>Creates text that shrinks or grows to fit, since squares change size with the grid.</summary>
    private static UnityEngine.UI.Text CreateText(Transform parent, string name, Font font,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        UnityEngine.UI.Text text = CreateChild(parent, name, anchorMin, anchorMax).gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = font;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = SmallestTextSize;
        text.resizeTextMaxSize = LargestTextSize;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateChild(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)child.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }
}
