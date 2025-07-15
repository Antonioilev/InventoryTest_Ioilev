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

        // Переперентивание на общий Canvas для свободного перемещения
        if (canvas != null)
            SetParentAndKeepWorldPosition(transform, canvas.transform);
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
        else if (backpackGridManager.SlotContainer == null)
        {
            Debug.LogWarning("InventoryItemDraggable: backpackGridManager SlotContainer is not assigned!");
        }
        else if (itemData == null)
        {
            Debug.LogWarning("InventoryItemDraggable: itemData is not assigned!");
        }
        else
        {
            // Проверяем, куда бросают предмет — в рюкзак или на землю
            if (backpackGridManager.IsPointerOverSlotContainer(Input.mousePosition))
            {
                placed = backpackGridManager.TryPlaceItemAtMousePosition(itemData, rectTransform);
                if (placed)
                {
                    // Предмет положен в рюкзак — он уничтожается в TryPlaceItem,
                    // так что тут ничего делать не надо.
                    return;
                }
            }

            // Если есть менеджер земли и указатель над ним — кладём на землю
            if (!placed && groundGridManager != null && groundGridManager.IsPointerOverGround(Input.mousePosition))
            {
                placed = groundGridManager.TryPlaceItemAtMousePosition(itemData, rectTransform);
                if (placed)
                {
                    // При размещении на земле меняем родителя на контейнер земли
                    SetParentAndKeepWorldPosition(transform, groundGridManager.ItemsParent);
                }
            }
        }

        if (!placed)
        {
            // Возврат в исходное положение и родителя
            SetParentAndKeepWorldPosition(transform, originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
        else
        {
            if (placed && backpackGridManager != null && backpackGridManager.IsPointerOverSlotContainer(Input.mousePosition))
            {
                // В случае размещения в рюкзаке объект уничтожается, чтобы не дублироваться.
                Destroy(gameObject);
            }
        }
    }

    // Метод смены родителя без смещения объекта в мире (UI)
    private void SetParentAndKeepWorldPosition(Transform child, Transform newParent)
    {
        Vector3 worldPos = child.position;
        child.SetParent(newParent, false);
        child.position = worldPos;
    }
}
