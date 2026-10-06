using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A block in the palette under the grid. Dragging it carries a copy with the
/// mouse; dropping the copy on an empty square places a player block there.
/// </summary>
public class SandboxPaletteCube : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Reused for every drop so no new list is created each time.
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

    private SandboxCubeKind _kind;
    private SandboxScreen _screen;
    private RectTransform _dragLayer;
    private RectTransform _draggedCopy;

    /// <summary>
    /// Creates a palette block. The drag layer is the object the dragged copy is
    /// put under, so it is drawn on top of everything else.
    /// </summary>
    public static SandboxPaletteCube Create(Transform parent, SandboxCubeKind kind, SandboxScreen screen, RectTransform dragLayer)
    {
        GameObject cubeObject = new GameObject(kind.Name + " Palette Block", typeof(RectTransform));
        cubeObject.transform.SetParent(parent, false);
        cubeObject.AddComponent<UnityEngine.UI.Image>().color = kind.Color;

        SandboxPaletteCube cube = cubeObject.AddComponent<SandboxPaletteCube>();
        cube._kind = kind;
        cube._screen = screen;
        cube._dragLayer = dragLayer;
        return cube;
    }

    /// <summary>Called by Unity when the player starts dragging this block.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        GameObject copy = new GameObject("Dragged Block", typeof(RectTransform));
        _draggedCopy = (RectTransform)copy.transform;
        _draggedCopy.SetParent(_dragLayer, false);
        _draggedCopy.sizeDelta = ((RectTransform)transform).rect.size;
        _draggedCopy.position = eventData.position;

        // The copy must not block the mouse, or the square underneath
        // could not be found when the copy is dropped.
        UnityEngine.UI.Image image = copy.AddComponent<UnityEngine.UI.Image>();
        image.color = _kind.Color;
        image.raycastTarget = false;
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

        SandboxCell cell = FindCellUnderPointer(eventData);
        if (cell != null)
        {
            _screen.TryPlacePlayerBlock(_kind, cell.GridPosition);
        }
    }

    private static SandboxCell FindCellUnderPointer(PointerEventData eventData)
    {
        EventSystem.current.RaycastAll(eventData, RaycastResults);
        foreach (RaycastResult result in RaycastResults)
        {
            SandboxCell cell = result.gameObject.GetComponentInParent<SandboxCell>();
            if (cell != null)
            {
                return cell;
            }
        }

        return null;
    }
}
