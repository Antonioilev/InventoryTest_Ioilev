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
        else
            Debug.LogWarning("InventoryItemDraggable: Canvas is null in OnBeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool placed = false;

        if (backpackGridManager == null)
        {
            Debug.LogWarning("InventoryItemDraggable: backpackGridManager is not assigned!");
        }
        else if (backpackGridManager.slotContainer == null)
        {
            Debug.LogWarning("InventoryItemDraggable: backpackGridManager.slotContainer is not assigned!");
        }
        else if (itemData == null)
        {
            Debug.LogWarning("InventoryItemDraggable: itemData is not assigned!");
        }
        else
        {
            // Проверяем, находится ли мышь над слотом рюкзака
            if (RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, Input.mousePosition))
            {
                placed = backpackGridManager.TryPlaceItemAtMousePosition(itemData, rectTransform);
            }
        }

        if (!placed)
        {
            // Если не удалось положить в рюкзак, возвращаем на прежнее место
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalPosition;
        }
        else
        {
            // Уничтожаем перетаскиваемый объект, т.к. предмет теперь в рюкзаке
            Destroy(gameObject);
        }
    }
}
