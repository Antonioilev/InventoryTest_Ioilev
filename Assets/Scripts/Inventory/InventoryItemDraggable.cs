using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

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
        Init(); // вызываем инициализацию
    }

    void Start()
    {
        // Дополнительная проверка, если объект вставлен в Canvas позже
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                Debug.LogWarning("InventoryItemDraggable: Canvas not found in Start()");
        }
    }

    public void Init()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogWarning("InventoryItemDraggable: Canvas not found in Init()");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null || canvas.transform == null)
        {
            Debug.LogError("InventoryItemDraggable: Canvas or canvas.transform is null in OnBeginDrag");
            return;
        }

        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        transform.SetParent(canvas.transform, true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);

        pointerOffset = localMousePosition - rectTransform.anchoredPosition;

        backpackGridManager?.UpdateGridUsed();

        if (groundGridManager != null)
        {
            groundGridManager.StartDragging(gameObject);
            groundGridManager.UpdateGridUsed(gameObject);
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

        groundGridManager?.UpdateGridUsed(gameObject);
    }

    private GameObject GetItemUnderPointer()
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var result in results)
        {
            var go = result.gameObject;
            if (go != gameObject && go.GetComponent<InventoryItemDraggable>() != null)
                return go;
        }

        return null;
    }

    private bool TryMergeWith(GameObject otherItemGO, Transform parentForNewItem)
    {
        var otherDraggable = otherItemGO.GetComponent<InventoryItemDraggable>();
        if (otherDraggable == null || otherDraggable.itemData != itemData)
            return false;

        var mergeHandler = GetComponent<InventoryItemMergeHandler>();
        if (mergeHandler == null || mergeHandler.mergeRules == null)
            return false;

        if (!mergeHandler.mergeRules.TryGetMergeResult(itemData, out var resultItem))
            return false;

        Destroy(otherItemGO);
        Destroy(gameObject);

        GameObject newItem = Instantiate(resultItem.itemPrefab);
        newItem.transform.SetParent(parentForNewItem, false); // СНАЧАЛА родитель

        var newRT = newItem.GetComponent<RectTransform>();
        if (newRT != null)
        {
            newRT.pivot = new Vector2(0, 1);
            newRT.anchorMin = new Vector2(0, 1);
            newRT.anchorMax = new Vector2(0, 1);
            newRT.localScale = Vector3.one;
            newRT.anchoredPosition = Vector2.zero; // Сброс позиции
        }

        var drag = newItem.GetComponent<InventoryItemDraggable>();
        if (drag != null)
        {
            drag.itemData = resultItem;
            drag.backpackGridManager = backpackGridManager;
            drag.groundGridManager = groundGridManager;
            drag.Init(); // Инициализация

            // Сброс состояния drag (прозрачность и блокировка Raycasts)
            drag.canvasGroup.alpha = 1f;
            drag.canvasGroup.blocksRaycasts = true;
        }

        return true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool placed = false;

        Transform mergeParent = null;
        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, Input.mousePosition))
        {
            mergeParent = backpackGridManager.slotContainer;
        }
        else if (groundGridManager != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, Input.mousePosition))
        {
            mergeParent = groundGridManager.itemsParent;
        }
        else
        {
            mergeParent = transform.parent;
        }

        // Попытка мерджа
        var targetItem = GetItemUnderPointer();
        if (targetItem != null && TryMergeWith(targetItem, mergeParent))
            return;

        // Второй способ мерджа (через физику)
        var hits = Physics2D.OverlapPointAll(Input.mousePosition);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var otherMerge = hit.GetComponent<InventoryItemMergeHandler>();
            var thisMerge = GetComponent<InventoryItemMergeHandler>();

            if (otherMerge != null && thisMerge != null && thisMerge.TryMergeWith(hit.gameObject, out InventoryItemData mergedResult))
            {
                Destroy(hit.gameObject);
                Destroy(gameObject);

                Vector2Int mergedSlot = Vector2Int.zero;
                if (groundGridManager.GetGridPositionUnderWorld(hit.transform.position, out mergedSlot))
                {
                    GameObject newItem = Instantiate(mergedResult.itemPrefab);
                    groundGridManager.PlaceExistingItem(mergedSlot, mergedResult, newItem);

                    InventoryItemDraggable drag = newItem.GetComponent<InventoryItemDraggable>();
                    if (drag != null)
                    {
                        drag.itemData = mergedResult;
                        drag.groundGridManager = groundGridManager;
                        drag.backpackGridManager = backpackGridManager;
                        drag.Init();
                        drag.canvasGroup.alpha = 1f;
                        drag.canvasGroup.blocksRaycasts = true;
                    }
                }
                return;
            }
        }

        // ⬇⬇⬇ Добавь это обязательно ⬇⬇⬇

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Пытаемся поставить в землю
        if (groundGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, Input.mousePosition))
        {
            if (groundGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject))
            {
                transform.SetParent(groundGridManager.itemsParent, false);
                return;
            }
        }

        // Пытаемся поставить в рюкзак
        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, Input.mousePosition))
        {
            if (backpackGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject))
            {
                transform.SetParent(backpackGridManager.itemsParent, false);
                return;
            }
        }

        // Не получилось — вернем обратно
        transform.SetParent(originalParent, false);
        rectTransform.anchoredPosition = originalPosition;
    }

}
