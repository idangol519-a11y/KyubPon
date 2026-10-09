using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One square of the battle grid on screen. It draws whatever block the board
/// says is on it: the block's color, its HP, and an X if it is an enemy block.
/// </summary>
public class BattleCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // How far the block picture stays from the edges of the square.
    private const float BlockInset = 8f;
    private const int SmallestTextSize = 8;
    private const int LargestTextSize = 72;

    // How much an enemy block's color is darkened, so the two sides are easy to tell apart.
    private const float EnemyDarkening = 0.35f;

    // The HP number sits in the bottom part of the block. The enemy X covers the whole block.
    private const float HpHeight = 0.42f;
    private const float HpOutlineWidth = 2f;

    private static readonly Color EnemyMarkColor = new Color(0f, 0f, 0f, 0.6f);

    private UnityEngine.UI.Image _squareImage;
    private UnityEngine.UI.Image _blockImage;
    private UnityEngine.UI.Text _enemyMark;
    private UnityEngine.UI.Text _hpText;
    private UnityEngine.UI.Image _areaTint;
    private Color _normalColor;
    private BattleScreen _screen;

    /// <summary>Where this square is in the grid: x is the column, y is the row.</summary>
    public Vector2Int GridPosition { get; private set; }

    /// <summary>Creates an empty square at the given grid position.</summary>
    public static BattleCell Create(Transform parent, Vector2Int gridPosition, Color color, Font font, BattleScreen screen)
    {
        GameObject cellObject = new GameObject($"Cell {gridPosition.x},{gridPosition.y}", typeof(RectTransform));
        cellObject.transform.SetParent(parent, false);

        BattleCell cell = cellObject.AddComponent<BattleCell>();
        cell.GridPosition = gridPosition;
        cell._normalColor = color;
        cell._screen = screen;

        // The square's image is also what the mouse "hits" when a block is dropped here.
        cell._squareImage = cellObject.AddComponent<UnityEngine.UI.Image>();
        cell._squareImage.color = color;
        // Created before the block picture, so the tint is drawn underneath the block.
        cell.CreateAreaTint();
        cell.CreateBlockPicture(font);
        cell.Show(null);
        return cell;
    }

    /// <summary>Called by Unity when the mouse moves onto this square. Starts the wait for the rules tooltip.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _screen.StartHover(null, this);
    }

    /// <summary>Called by Unity when the mouse leaves this square.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _screen.EndHover(null, this);
    }

    /// <summary>Lights the square up while it is being activated, or returns it to normal.</summary>
    public void SetHighlight(bool isOn, Color highlightColor)
    {
        _squareImage.color = isOn ? highlightColor : _normalColor;
    }

    /// <summary>
    /// Colors the square, used to show a block's area of effect. The tint is drawn
    /// under the block, so on an occupied square it shows as a colored frame around the block.
    /// </summary>
    public void ShowAreaTint(Color color)
    {
        _areaTint.color = color;
        _areaTint.enabled = true;
    }

    /// <summary>Removes the area-of-effect tint.</summary>
    public void HideAreaTint()
    {
        _areaTint.enabled = false;
    }

    /// <summary>Draws the given block on this square. Pass null for an empty square.</summary>
    public void Show(BattleBlock block)
    {
        _blockImage.gameObject.SetActive(block != null);
        if (block == null)
        {
            return;
        }

        bool isEnemy = block.Side == BattleSide.Enemy;
        block.Kind.ApplyLook(_blockImage, isEnemy ? EnemyDarkening : 0f);
        _enemyMark.enabled = isEnemy;
        _hpText.text = block.Hp.ToString();
    }

    private void CreateAreaTint()
    {

        _areaTint = CreateChild(transform, "AreaTint", Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
        _areaTint.raycastTarget = false;
        _areaTint.enabled = false;
    }

    private void CreateBlockPicture(Font font)
    {
        RectTransform blockRect = CreateChild(transform, "Block", Vector2.zero, Vector2.one);
        blockRect.offsetMin = new Vector2(BlockInset, BlockInset);
        blockRect.offsetMax = new Vector2(-BlockInset, -BlockInset);
        _blockImage = blockRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        _blockImage.raycastTarget = false;

        _enemyMark = CreateText(blockRect, "EnemyMark", font, Vector2.zero, Vector2.one, EnemyMarkColor);
        _enemyMark.text = "X";
        _hpText = CreateText(blockRect, "Hp", font, Vector2.zero, new Vector2(1f, HpHeight), Color.white);

        // A dark outline keeps the white number readable on bright skins.
        UnityEngine.UI.Outline outline = _hpText.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(HpOutlineWidth, -HpOutlineWidth);
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
