using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// TEMPORARY (sandbox test page, to be removed).
/// A colored cube that can be dragged with the mouse.
/// A cube in the palette is a "source": dragging it makes a copy and leaves the
/// original in place. A cube in the grid moves itself. Dropping a cube outside
/// the grid removes it.
/// </summary>
public class SandboxCube : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // How far a cube stays from the edges of the cell it sits in.
    private const float InsetInsideCell = 10f;

    // Reused for every drop so no new list is created each time.
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

    private RectTransform _rectTransform;
    private UnityEngine.UI.Image _image;
    private RectTransform _dragLayer;
    private bool _isPaletteSource;
    private SandboxCell _cell;
    private SandboxCube _draggedCube;

    /// <summary>What kind of cube this is: its color and scoring rule.</summary>
    public SandboxCubeKind Kind { get; private set; }

    /// <summary>
    /// Creates a cube. The drag layer is the object cubes are moved under while
    /// being dragged, so they are drawn on top of everything else.
    /// </summary>
    public static SandboxCube Create(Transform parent, SandboxCubeKind kind, RectTransform dragLayer, bool isPaletteSource)
    {
        GameObject cubeObject = new GameObject("Cube", typeof(RectTransform));
        cubeObject.transform.SetParent(parent, false);

        SandboxCube cube = cubeObject.AddComponent<SandboxCube>();
        cube._rectTransform = (RectTransform)cubeObject.transform;
        cube._image = cubeObject.AddComponent<UnityEngine.UI.Image>();
        cube._image.color = kind.Color;
        cube.Kind = kind;
        cube._dragLayer = dragLayer;
        cube._isPaletteSource = isPaletteSource;
        return cube;
    }

    /// <summary>Called by Unity when the player starts dragging this cube.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector2 size = _rectTransform.rect.size;
        _draggedCube = _isPaletteSource ? Create(_dragLayer, Kind, _dragLayer, false) : this;
        _draggedCube.PickUp(size, eventData.position);
    }

    /// <summary>Called by Unity every frame while the drag continues.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedCube != null)
        {
            _draggedCube.transform.position = eventData.position;
        }
    }

    /// <summary>Called by Unity when the player lets go.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_draggedCube == null)
        {
            return;
        }

        SandboxCell cell = FindCellUnderPointer(eventData);
        if (cell != null)
        {
            cell.Place(_draggedCube);
        }
        else
        {
            Destroy(_draggedCube.gameObject);
        }

        _draggedCube = null;
    }

    /// <summary>Moves the cube into a cell and stretches it to fill the cell.</summary>
    public void SnapInto(SandboxCell cell)
    {
        _cell = cell;
        _rectTransform.SetParent(cell.transform, false);
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.offsetMin = new Vector2(InsetInsideCell, InsetInsideCell);
        _rectTransform.offsetMax = new Vector2(-InsetInsideCell, -InsetInsideCell);
        _image.raycastTarget = true;
    }

    /// <summary>Lifts the cube out of its cell so it can follow the mouse.</summary>
    private void PickUp(Vector2 size, Vector2 pointerPosition)
    {
        if (_cell != null)
        {
            _cell.Clear();
            _cell = null;
        }

        Vector2 center = new Vector2(0.5f, 0.5f);
        _rectTransform.SetParent(_dragLayer, false);
        _rectTransform.anchorMin = center;
        _rectTransform.anchorMax = center;
        _rectTransform.sizeDelta = size;
        _rectTransform.position = pointerPosition;

        // While dragged, the cube must not block the mouse, or the cell
        // underneath could not be found when the cube is dropped.
        _image.raycastTarget = false;
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
