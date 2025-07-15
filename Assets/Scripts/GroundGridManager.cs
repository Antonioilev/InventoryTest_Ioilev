using UnityEngine;

public class GroundGridManager : MonoBehaviour
{
    public Transform ItemsParent; // ��������� ��� ��������� �� �����
    public RectTransform groundRectTransform; // UI-������� ����� ��� ������

    // ���������, ��������� �� ��������� ���� ��� ����� ����� (��������, RectTransform � ������)
    public bool IsPointerOverGround(Vector2 screenPosition)
    {
        if (groundRectTransform == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(groundRectTransform, screenPosition, cam);
    }

    // �������� ���������� ������� �� ����� �� ������� ���� (��������� ����������)
    public bool TryPlaceItemAtMousePosition(InventoryItemData itemData, RectTransform draggedRect)
    {
        if (itemData == null || ItemsParent == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(groundRectTransform, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        // ������� ��������� �������� �� �����
        GameObject itemGO = Instantiate(itemData.itemPrefab, ItemsParent, false);
        itemGO.name = $"GroundItem_{itemData.itemId}";

        RectTransform rt = itemGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = localPoint;

            // ���������� �������, ���� ����� (����� ���������� ��� �������� �������)
            rt.sizeDelta = draggedRect.sizeDelta;
        }

        // ����� ��� �������� �������������, ���� ����� (InventoryItemView � ��.)

        return true;
    }
}
