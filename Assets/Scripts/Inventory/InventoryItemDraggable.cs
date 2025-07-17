using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryItemDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float cellSize = 140f; // размер ячейки в пикселях, подгони под свою сетку

    private static bool hasLoggedCanvasWarning = false; // флаг для единовременного лога

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
        Init();
    }

    void Start()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"[InventoryItemDraggable] Canvas not found on '{gameObject.name}'. Drag & Drop не будет работать.");
                enabled = false; // Отключаем компонент, чтобы избежать проблем
            }
            else
            {
                if (!hasLoggedCanvasWarning)
                {
                    Debug.Log($"[InventoryItemDraggable] Canvas найден в Start() на '{gameObject.name}', хотя не был найден в Awake(). Проверь порядок инициализации.");
                    hasLoggedCanvasWarning = true;
                }
            }
        }
    }

    public void Init()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Убираем поиск Canvas из Init(), чтобы не сыпалась ложная ошибка
        // canvas = GetComponentInParent<Canvas>();
        // if (canvas == null)
        // {
        //     Debug.LogError($"[InventoryItemDraggable] Canvas not found for '{gameObject.name}'. Drag & Drop may not work properly.");
        //     enabled = false;
        //     return;
        // }
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

        // Переносим объект под канвас, чтобы он был на переднем плане
        transform.SetParent(canvas.transform, true);

        // Вычисляем смещение между курсором и позицией объекта
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
        {
            Debug.Log("TryMergeWith: other item data mismatch");
            return false;
        }

        var mergeHandler = GetComponent<InventoryItemMergeHandler>();
        if (mergeHandler == null || mergeHandler.mergeRules == null)
        {
            Debug.Log("TryMergeWith: no merge handler or rules");
            return false;
        }

        if (!mergeHandler.mergeRules.TryGetMergeResult(itemData, out var resultItem))
        {
            Debug.Log($"TryMergeWith: no merge result for {itemData.name}");
            return false;
        }

        bool isInBackpack = transform.IsChildOf(backpackGridManager?.slotContainer);
        bool otherIsInBackpack = otherItemGO.transform.IsChildOf(backpackGridManager?.slotContainer);

        GameObject newItem = Instantiate(resultItem.itemPrefab);

        bool placed = false;

        if (isInBackpack || otherIsInBackpack)
        {
            // Мерж происходит в рюкзаке — размещаем в рюкзаке

            // Если у тебя есть метод размещения предмета в рюкзаке с указанием позиции, вызови его.
            // Например, если есть backpackGridManager.PlaceExistingItem:
            // Тут можно попытаться найти свободную позицию для предмета в рюкзаке:

            Vector2Int backpackPos;
            bool foundPos = false;
            if (backpackGridManager.TryFindFreePosition(resultItem.size, out backpackPos))
            {
                placed = backpackGridManager.PlaceExistingItem(backpackPos, resultItem, newItem);
                foundPos = placed;
            }

            if (!foundPos)
            {
                Debug.LogWarning("TryMergeWith: Нет места в рюкзаке для нового предмета мерджа");
                Destroy(newItem);
                return false;
            }

            newItem.transform.SetParent(backpackGridManager.itemsParent, false);
        }
        else
        {
            // Мерж происходит на земле — размещаем на земле

            Vector2Int mergePosition = Vector2Int.zero;
            bool positionFound = false;

            if (groundGridManager != null)
            {
                groundGridManager.UpdateGridUsed(gameObject);
                groundGridManager.UpdateGridUsed(otherItemGO);

                if (groundGridManager.GetGridPositionUnderWorld(transform.position, out Vector2Int posThis))
                {
                    mergePosition = posThis;
                    positionFound = true;
                }
                else if (groundGridManager.GetGridPositionUnderWorld(otherItemGO.transform.position, out Vector2Int posOther))
                {
                    mergePosition = posOther;
                    positionFound = true;
                }

                if (!positionFound || !groundGridManager.CanPlaceAt(mergePosition, resultItem.size))
                {
                    if (!groundGridManager.TryFindFreePosition(resultItem.size, out mergePosition))
                    {
                        Debug.LogWarning("TryMergeWith: Нет свободного места для размещения результата мерджа на земле");
                        Destroy(newItem);
                        return false;
                    }
                }

                placed = groundGridManager.PlaceExistingItem(mergePosition, resultItem, newItem);
                newItem.transform.SetParent(groundGridManager.itemsParent, false);
            }
        }

        if (!placed)
        {
            //Debug.LogWarning("TryMergeWith: Не удалось разместить новый предмет после мерджа");
            Destroy(newItem);
            return false;
        }

        Destroy(otherItemGO);
        Destroy(gameObject);

        RectTransform newRT = newItem.GetComponent<RectTransform>();
        if (newRT != null)
        {
            newRT.pivot = new Vector2(0, 1);
            newRT.anchorMin = new Vector2(0, 1);
            newRT.anchorMax = new Vector2(0, 1);
            newRT.localScale = Vector3.one;
        }

        var drag = newItem.GetComponent<InventoryItemDraggable>();
        if (drag != null)
        {
            drag.itemData = resultItem;
            drag.backpackGridManager = backpackGridManager;
            drag.groundGridManager = groundGridManager;
            drag.Init();

            drag.canvasGroup.alpha = 1f;
            drag.canvasGroup.blocksRaycasts = true;
        }

        Debug.Log($"TryMergeWith: {resultItem.name} успешно создан и размещён");

        return true;
    }









    public void OnEndDrag(PointerEventData eventData)
    {
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

        // Попытка мерджа по UI
        var targetItem = GetItemUnderPointer();
        if (targetItem != null && TryMergeWith(targetItem, mergeParent))
            return;

        // Попытка мерджа по физике (2D)
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);
        var hits = Physics2D.OverlapPointAll(worldPoint2D);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var otherMerge = hit.GetComponent<InventoryItemMergeHandler>();
            var thisMerge = GetComponent<InventoryItemMergeHandler>();

            if (otherMerge != null && thisMerge != null && thisMerge.TryMergeWith(hit.gameObject, out InventoryItemData mergedResult))
            {
                // Освобождаем слоты у обоих предметов
                groundGridManager?.ClearItemFromGrid(gameObject);
                groundGridManager?.ClearItemFromGrid(hit.gameObject);

                Destroy(hit.gameObject);
                Destroy(gameObject);

                Vector2Int mergedSlot = Vector2Int.zero;
                if (groundGridManager.GetGridPositionUnderWorld(hit.transform.position, out mergedSlot))
                {
                    GameObject newItem = Instantiate(mergedResult.itemPrefab);
                    bool placed = groundGridManager.PlaceExistingItem(mergedSlot, mergedResult, newItem);

                    if (!placed)
                    {
                        Debug.LogWarning("OnEndDrag: Не удалось разместить новый предмет после мерджа");
                        Destroy(newItem);
                        return;
                    }

                    var drag = newItem.GetComponent<InventoryItemDraggable>();
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

        // Восстановление прозрачности и блокировки raycast
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Пытаемся поставить предмет в землю
        if (groundGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, Input.mousePosition))
        {
            if (groundGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject))
            {
                transform.SetParent(groundGridManager.itemsParent, false);
                return;
            }
        }

        // Пытаемся поставить предмет в рюкзак
        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, Input.mousePosition))
        {
            if (backpackGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject))
            {
                transform.SetParent(backpackGridManager.itemsParent, false);
                return;
            }
        }

        // Не удалось поставить — вернуть на место
        transform.SetParent(originalParent, false);
        rectTransform.anchoredPosition = originalPosition;
    }


    public void SetData(InventoryItemData newData)
    {
        itemData = newData;

        transform.localScale = Vector3.one;
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null && itemData != null)
        {
            rect.sizeDelta = new Vector2(itemData.size.x * cellSize, itemData.size.y * cellSize);
        }
    }
}
