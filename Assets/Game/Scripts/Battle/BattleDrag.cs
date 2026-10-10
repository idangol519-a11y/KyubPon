using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The two things every drag in the battle needs: the picture that follows the
/// mouse, and finding the square the mouse is over when the player lets go.
/// Shared by the blocks in the hand and by blocks moved on the grid.
/// </summary>
public static class BattleDrag
{
    // Reused for every drop so no new list is created each time.
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

    /// <summary>
    /// Creates the picture of a block that follows the mouse during a drag.
    /// It is put under the drag layer so it is drawn on top of everything else.
    /// </summary>
    public static RectTransform CreateCopy(RectTransform dragLayer, BattleCubeKind kind, Vector2 size, Vector2 screenPosition)
    {
        GameObject copy = new GameObject("Dragged Block", typeof(RectTransform));
        RectTransform rect = (RectTransform)copy.transform;
        rect.SetParent(dragLayer, false);
        rect.sizeDelta = size;
        rect.position = screenPosition;

        // The copy must not block the mouse, or the square underneath
        // could not be found when the copy is dropped.
        UnityEngine.UI.Image image = copy.AddComponent<UnityEngine.UI.Image>();
        kind.ApplyLook(image, 0f);
        image.raycastTarget = false;
        return rect;
    }

    /// <summary>Returns the grid square under the mouse, or null if the mouse is not over the grid.</summary>
    public static BattleCell FindCellUnderPointer(PointerEventData eventData)
    {
        EventSystem.current.RaycastAll(eventData, RaycastResults);
        foreach (RaycastResult result in RaycastResults)
        {
            BattleCell cell = result.gameObject.GetComponentInParent<BattleCell>();
            if (cell != null)
            {
                return cell;
            }
        }

        return null;
    }
}
