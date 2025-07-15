using UnityEngine;

public class GroundGridManager : MonoBehaviour
{
    public Transform ItemsParent; // Контейнер для предметов на земле
    public RectTransform groundRectTransform; // UI-элемент земли или аналог

    // Проверяет, находится ли указатель мыши над зоной земли (например, RectTransform с землей)
    public bool IsPointerOverGround(Vector2 screenPosition)
    {
        if (groundRectTransform == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(groundRectTransform, screenPosition, cam);
    }

    // Пытается разместить предмет на земле по позиции мыши (примерная реализация)
    public bool TryPlaceItemAtMousePosition(InventoryItemData itemData, RectTransform draggedRect)
    {
        if (itemData == null || ItemsParent == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(groundRectTransform, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        // Создаем экземпляр предмета на земле
        GameObject itemGO = Instantiate(itemData.itemPrefab, ItemsParent, false);
        itemGO.name = $"GroundItem_{itemData.itemId}";

        RectTransform rt = itemGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = localPoint;

            // Подстройка размера, если нужно (можно доработать под реальные размеры)
            rt.sizeDelta = draggedRect.sizeDelta;
        }

        // Можно тут добавить инициализацию, если нужно (InventoryItemView и др.)

        return true;
    }
}
