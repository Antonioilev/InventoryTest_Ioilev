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

        // Вычисляем смещение курсора относительно пивота предмета
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);

        pointerOffset = localMousePosition - rectTransform.anchoredPosition;

        // Обновляем занятость и подсветку доступных слотов в рюкзаке
        if (backpackGridManager != null)
        {
            backpackGridManager.UpdateGridUsed();
            backpackGridManager.UpdateSlotHighlightsForItem(itemData);
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

            // Обновляем подсветку доступных слотов в рюкзаке на каждом кадре
            if (backpackGridManager != null)
            {
                backpackGridManager.UpdateSlotHighlightsForItem(itemData);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool placed = false;

        // Попытка поместить предмет в рюкзак, если курсор внутри рюкзака
        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, Input.mousePosition))
        {
            placed = backpackGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject);

            if (placed)
            {
                backpackGridManager.UpdateGridUsed(); // Обновляем массив занятости после размещения
            }
        }
        // Попытка поместить предмет на землю, если курсор внутри groundGrid
        else if (!placed && groundGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, Input.mousePosition))
        {
            placed = groundGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject);
        }

        // Очистка подсветки рюкзака
        if (backpackGridManager != null)
        {
            backpackGridManager.ClearAllSlotHighlights();
        }

        // Если не удалось поместить, возвращаем предмет на исходную позицию и родителя
        if (!placed)
        {
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}
