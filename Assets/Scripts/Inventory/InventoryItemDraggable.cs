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

    private bool TryMergeWith(GameObject otherItemGO)
    {
        var otherDraggable = otherItemGO.GetComponent<InventoryItemDraggable>();
        if (otherDraggable == null || otherDraggable.itemData != itemData)
            return false;

        var mergeHandler = GetComponent<InventoryItemMergeHandler>();
        if (mergeHandler == null || mergeHandler.mergeRules == null)
            return false;

        if (!mergeHandler.mergeRules.TryGetMergeResult(itemData, out var resultItem))
            return false;

        // Удаляем оба
        Destroy(otherItemGO);
        Destroy(gameObject);

        // Спавним новый
        GameObject newItem = Instantiate(resultItem.itemPrefab, transform.parent);
        var rt = newItem.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0, 1);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = GetComponent<RectTransform>().anchoredPosition;

        // Прокидываем itemData
        var drag = newItem.GetComponent<InventoryItemDraggable>();
        if (drag != null)
            drag.itemData = resultItem;

        return true;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. Проверка на мердж
        var targetItem = GetItemUnderPointer();
        if (targetItem != null && TryMergeWith(targetItem))
        {
            return; // успешно замержили — больше ничего не делаем
        }

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

        // Попытка слияния (мерджа)
        if (!placed)
        {
            // Проверяем попадание в другой предмет
            var hits = Physics2D.OverlapPointAll(Input.mousePosition);
            foreach (var hit in hits)
            {
                if (hit.gameObject == this.gameObject) continue;

                var otherMerge = hit.GetComponent<InventoryItemMergeHandler>();
                var thisMerge = GetComponent<InventoryItemMergeHandler>();

                if (otherMerge != null && thisMerge != null && thisMerge.TryMergeWith(hit.gameObject, out InventoryItemData mergedResult))
                {
                    // Удаляем оба старых
                    Destroy(hit.gameObject);
                    Destroy(this.gameObject);

                    // Спавним новый на позиции старого
                    Vector2Int mergedSlot = Vector2Int.zero;
                    if (groundGridManager.GetGridPositionUnderWorld(hit.transform.position, out mergedSlot)) // нужен такой метод
                    {
                        GameObject newItem = Instantiate(mergedResult.itemPrefab, groundGridManager.itemsParent);
                        groundGridManager.PlaceExistingItem(mergedSlot, mergedResult, newItem);

                        // Назначаем ссылки
                        InventoryItemDraggable drag = newItem.GetComponent<InventoryItemDraggable>();
                        if (drag != null)
                        {
                            drag.itemData = mergedResult;
                            drag.groundGridManager = groundGridManager;
                            drag.backpackGridManager = backpackGridManager;
                        }
                    }

                    return; // завершили OnEndDrag
                }
            }
        }


        // Гарантированно обновляем занятость после попытки размещения
        if (groundGridManager != null)
            groundGridManager.UpdateGridUsed();
    }
}
