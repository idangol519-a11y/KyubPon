using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One block of the player's hand, shown under the grid. Dragging it carries a copy
/// with the mouse; dropping the copy on an empty square places the block there.
/// </summary>
public class BattlePaletteCube : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    private BattleCubeKind _kind;
    private BattleScreen _screen;
    private RectTransform _dragLayer;
    private RectTransform _draggedCopy;

    /// <summary>
    /// Creates a palette block. The drag layer is the object the dragged copy is
    /// put under, so it is drawn on top of everything else.
    /// </summary>
    public static BattlePaletteCube Create(Transform parent, BattleCubeKind kind, BattleScreen screen, RectTransform dragLayer)
    {
        GameObject cubeObject = new GameObject(kind.Name + " Palette Block", typeof(RectTransform));
        cubeObject.transform.SetParent(parent, false);
        kind.ApplyLook(cubeObject.AddComponent<UnityEngine.UI.Image>(), 0f);

        BattlePaletteCube cube = cubeObject.AddComponent<BattlePaletteCube>();
        cube._kind = kind;
        cube._screen = screen;
        cube._dragLayer = dragLayer;
        return cube;
    }

    /// <summary>Called by Unity when the mouse moves onto this block. Starts the wait for the rules tooltip.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _screen.StartHover(_kind, null);
    }

    /// <summary>Called by Unity when the mouse leaves this block.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _screen.EndHover(_kind, null);
    }

    /// <summary>Called by Unity when the player starts dragging this block.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _screen.SetDraggingBlock(true);
        GameAudio.Play(GameSound.BlockPickUp);
        _draggedCopy = BattleDrag.CreateCopy(_dragLayer, _kind, ((RectTransform)transform).rect.size, eventData.position);
    }

    /// <summary>Called by Unity every frame while the drag continues.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedCopy != null)
        {
            _draggedCopy.position = eventData.position;
        }
    }

    /// <summary>Called by Unity when the player lets go.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_draggedCopy == null)
        {
            return;
        }

        Destroy(_draggedCopy.gameObject);
        _draggedCopy = null;
        _screen.SetDraggingBlock(false);

        BattleCell cell = BattleDrag.FindCellUnderPointer(eventData);
        if (cell != null)
        {
            // The mouse is already on this square, so Unity will not report "entering" it.
            // Start the hover by hand so the tooltip still appears if the mouse stays put.
            // The screen redraws the hand after a block is placed, which removes this block from it.
            _screen.TryPlacePlayerBlock(_kind, cell.GridPosition);
            _screen.StartHover(null, cell);
        }
    }
}
