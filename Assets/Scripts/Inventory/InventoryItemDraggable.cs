using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InventoryItemDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float cellSize = 140f;

    private static bool hasLoggedCanvasWarning = false;

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

    public void Init()
    {
        rectTransform = GetComponent<RectTransform>();

        // Проверяем CanvasGroup и добавляем, если нет
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"[InventoryItemDraggable] Canvas not found on '{gameObject.name}'. Drag & Drop не будет работать.");
                enabled = false;
            }
            else if (!hasLoggedCanvasWarning)
            {
                Debug.Log($"[InventoryItemDraggable] Canvas найден в Start() на '{gameObject.name}', хотя не был найден в Awake(). Проверь порядок инициализации.");
                hasLoggedCanvasWarning = true;
            }
        }
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
        Vector2 pointerPosition = Vector2.zero;

        // Приоритет: Touch > Mouse
        var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
        if (touchscreen != null && touchscreen.touches.Count > 0 && touchscreen.primaryTouch.press.isPressed)
        {
            pointerPosition = touchscreen.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            Debug.LogWarning("GetItemUnderPointer: Нет доступных устройств ввода");
            return null;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var result in results)
        {
            GameObject go = result.gameObject;
            if (go != gameObject && go.GetComponent<InventoryItemDraggable>() != null)
            {
                return go;
            }
        }

        return null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 mousePos = eventData.position;
        Debug.Log($"OnEndDrag called on: {gameObject.name}");
        Debug.Log($"Mouse Position: {mousePos}");

        Transform mergeParent = null;

        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, mousePos))
        {
            mergeParent = backpackGridManager.slotContainer;
            Debug.Log("Pointer is over BackpackGrid slotContainer");
        }
        else if (groundGridManager != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, mousePos))
        {
            mergeParent = groundGridManager.itemsParent;
            Debug.Log("Pointer is over GroundGrid slotContainer");
        }
        else
        {
            mergeParent = transform.parent;
            Debug.Log("Pointer is outside known containers, keeping original parent");
        }

        var targetItem = GetItemUnderPointer();
        Debug.Log($"Target item under pointer: {(targetItem != null ? targetItem.name : "null")}");

        if (targetItem != null && TryMergeWith(targetItem, mergeParent))
        {
            Debug.Log("Items merged successfully");
            return;
        }

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);
        var hits = Physics2D.OverlapPointAll(worldPoint2D);
        Debug.Log($"Physics2D.OverlapPointAll hits count: {hits.Length}");

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var otherMerge = hit.GetComponent<InventoryItemMergeHandler>();
            var thisMerge = GetComponent<InventoryItemMergeHandler>();

            if (otherMerge != null && thisMerge != null && thisMerge.TryMergeWith(hit.gameObject, out InventoryItemData mergedResult))
            {
                Debug.Log($"Merging with {hit.gameObject.name} successful, creating merged item {mergedResult.name}");

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

        Debug.Log("No merge happened, placing item in container");

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (groundGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(groundGridManager.slotContainer, mousePos))
        {
            if (groundGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject))
            {
                transform.SetParent(groundGridManager.itemsParent, false);
                Debug.Log("Placed item in GroundGrid");
                return;
            }
        }

        if (backpackGridManager != null &&
            RectTransformUtility.RectangleContainsScreenPoint(backpackGridManager.slotContainer, mousePos))
        {
            if (backpackGridManager.PlaceExistingItemAtMousePosition(itemData, gameObject))
            {
                transform.SetParent(backpackGridManager.itemsParent, false);
                Debug.Log("Placed item in BackpackGrid");
                return;
            }
        }

        transform.SetParent(originalParent, false);
        rectTransform.anchoredPosition = originalPosition;
        Debug.Log("Returned item to original position");
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
            Vector2Int backpackPos;
            bool foundPos = backpackGridManager.TryFindFreePosition(resultItem.size, out backpackPos);
            if (foundPos)
            {
                placed = backpackGridManager.PlaceExistingItem(backpackPos, resultItem, newItem);
                newItem.transform.SetParent(backpackGridManager.itemsParent, false);
            }

            if (!placed)
            {
                Debug.LogWarning("TryMergeWith: Нет места в рюкзаке для нового предмета мерджа");
                Destroy(newItem);
                return false;
            }
        }
        else
        {
            Vector2Int mergePosition = Vector2Int.zero;
            bool positionFound = false;

            groundGridManager?.UpdateGridUsed(gameObject);
            groundGridManager?.UpdateGridUsed(otherItemGO);

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

        if (!placed)
        {
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
