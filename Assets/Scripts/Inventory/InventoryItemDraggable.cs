using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 originalPosition;
    private Transform originalParent;

    private Vector2 pointerOffset;

    [Header("Drag Targets")]
    public BackpackGridManager backpackGridManager;
    public GroundGridManager groundGridManager;

    public InventoryItemData itemData;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogWarning("InventoryItemDraggable: Canvas not found in parent hierarchy");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        if (canvas != null)
            transform.SetParent(canvas.transform, true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);

        pointerOffset = localMousePosition - rectTransform.anchoredPosition;

        // Обновляем занятость в рюкзаке (если есть)
        if (backpackGridManager != null)
            backpackGridManager.UpdateGridUsed();

        // Начинаем перетаскивание на земле
        if (groundGridManager != null)
        {
            groundGridManager.StartDragging(gameObject); // установим draggedItem
            groundGridManager.UpdateGridUsed(gameObject); // сразу освобождаем слоты под предметом
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition))
        {
            rectTransform.anchoredPosition = localMousePosition - pointerOffset;
        }

        // Во время перетаскивания обновляем занятость слотов земли (исключая текущий предмет)
        if (groundGridManager != null)
            groundGridManager.UpdateGridUsed(gameObject);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool placed = false;

        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, Input.mousePosition))
        {
            placed = backpackGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject);
            if (placed)
                backpackGridManager.UpdateGridUsed();
        }
        else if (!placed && groundGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, Input.mousePosition))
        {
            placed = groundGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject);
            if (placed)
                groundGridManager.UpdateGridUsed();
        }

        if (backpackGridManager != null)
            backpackGridManager.ClearAllSlotHighlights();

        if (groundGridManager != null)
            groundGridManager.StopDragging(); // очистка draggedItem

        if (!placed)
        {
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalPosition;
        }

        // Гарантированно обновляем занятость после попытки размещения
        if (groundGridManager != null)
            groundGridManager.UpdateGridUsed();
    }
}
