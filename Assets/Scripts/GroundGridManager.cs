using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GroundGridManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public Transform itemsParent;

    public Transform canvasRoot;
    public Transform groundItemsParent;

    public int columns = 10;
    public int rows = 5;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool[,] gridUsed;

    private bool isDragging = false;
    private GameObject draggedItem = null;
    public GameObject itemPrefab;

    void Start()
    {
        GenerateGrid();
    }

    void Update()
    {
        if (isDragging && draggedItem != null)
        {
            UpdateGridUsed(draggedItem);
        }
    }

    public void StartDragging(GameObject item)
    {
        isDragging = true;
        draggedItem = item;
        UpdateGridUsed(draggedItem);
    }

    public void StopDragging()
    {
        isDragging = false;
        draggedItem = null;
        UpdateGridUsed();
    }

    public void GenerateGrid()
    {
        ClearGrid();
        gridUsed = new bool[columns, rows];

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            Debug.LogError("GridLayoutGroup component missing on slotContainer");
            return;
        }

        Vector2 containerSize = slotContainer.rect.size;
        float availableWidth = containerSize.x - grid.padding.left - grid.padding.right - grid.spacing.x * (columns - 1);
        float availableHeight = containerSize.y - grid.padding.top - grid.padding.bottom - grid.spacing.y * (rows - 1);
        float cellSize = Mathf.Min(availableWidth / columns, availableHeight / rows);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellSize, cellSize);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"GroundSlot_{x}_{y}";
                spawnedSlots.Add(slot);

                Image img = slot.GetComponent<Image>();
                if (img != null) img.color = new Color(1, 1, 1, 0.2f);

                RectTransform slotRT = slot.GetComponent<RectTransform>();
                if (slotRT != null)
                {
                    slotRT.pivot = new Vector2(0, 1);
                    slotRT.anchorMin = new Vector2(0, 1);
                    slotRT.anchorMax = new Vector2(0, 1);
                }
            }
        }

        UpdateGridUsed();
    }

    public void ClearGrid()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null) Destroy(slot);
        }
        spawnedSlots.Clear();
    }

    public void UpdateGridUsed(GameObject excludeItem = null)
    {
        if (gridUsed == null) return;

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                gridUsed[x, y] = false;

        Vector2 slotSize = GetSlotSize();

        foreach (Transform item in itemsParent)
        {
            if (excludeItem != null && item.gameObject == excludeItem)
                continue;

            RectTransform rt = item as RectTransform;
            if (rt == null) continue;

            Vector2 localPos = rt.anchoredPosition;

            int startX = Mathf.RoundToInt(localPos.x / slotSize.x);
            int startY = rows - 1 - Mathf.RoundToInt(-localPos.y / slotSize.y);

            int sizeX = Mathf.CeilToInt(rt.sizeDelta.x / slotSize.x);
            int sizeY = Mathf.CeilToInt(rt.sizeDelta.y / slotSize.y);

            for (int dx = 0; dx < sizeX; dx++)
            {
                for (int dy = 0; dy < sizeY; dy++)
                {
                    int x = startX + dx;
                    int y = startY + dy;

                    if (x >= 0 && x < columns && y >= 0 && y < rows)
                        gridUsed[x, y] = true;
                    else
                    {
                        //Debug.LogWarning($"Item '{item.name}' occupies out-of-bounds cell ({x},{y})");
                    }
                }
            }
        }
    }

    public bool CanPlaceAt(Vector2Int slotPos, Vector2Int size, GameObject excludeItem = null)
    {
        if (slotPos.x < 0 || slotPos.y < 0 || slotPos.x + size.x > columns || slotPos.y + size.y > rows)
            return false;

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                int x = slotPos.x + dx;
                int y = slotPos.y + dy;

                if (gridUsed[x, y])
                {
                    if (!IsCellOccupiedByItemAtPosition(x, y, excludeItem))
                        return false;
                }
            }
        }
        return true;
    }

    private bool IsCellOccupiedByItemAtPosition(int x, int y, GameObject excludeItem)
    {
        if (excludeItem == null) return false;

        RectTransform rt = excludeItem.GetComponent<RectTransform>();
        if (rt == null) return false;

        Vector2 slotSize = GetSlotSize();
        Vector2 localPos = rt.anchoredPosition;

        int startX = Mathf.RoundToInt(localPos.x / slotSize.x);
        int startY = rows - 1 - Mathf.RoundToInt(localPos.y / slotSize.y);
        int sizeX = Mathf.CeilToInt(rt.sizeDelta.x / slotSize.x);
        int sizeY = Mathf.CeilToInt(rt.sizeDelta.y / slotSize.y);

        return (x >= startX && x < startX + sizeX && y >= startY && y < startY + sizeY);
    }

    private void MarkCells(Vector2Int slotPos, Vector2Int size, bool occupied)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                gridUsed[slotPos.x + dx, slotPos.y + dy] = occupied;
    }

    public bool PlaceExistingItemAtMousePosition(InventoryItemData data, GameObject itemGO)
    {
        if (!GetGridPositionUnderMouse(out Vector2Int gridPos, data.size))
            return false;

        return PlaceExistingItem(gridPos, data, itemGO);
    }

    public bool PlaceExistingItem(Vector2Int slotPosition, InventoryItemData itemData, GameObject itemGO)
    {
        if (itemData == null || itemGO == null || itemsParent == null)
            return false;

        UpdateGridUsed(itemGO);

        if (!CanPlaceAt(slotPosition, itemData.size, itemGO))
            return false;

        MarkCells(slotPosition, itemData.size, true);

        itemGO.transform.SetParent(itemsParent, false);

        RectTransform itemRT = itemGO.GetComponent<RectTransform>();
        RectTransform slotRT = GetSlotRect(slotPosition);

        if (itemRT != null && slotRT != null)
        {
            itemRT.pivot = new Vector2(0, 1);
            itemRT.anchorMin = new Vector2(0, 1);
            itemRT.anchorMax = new Vector2(0, 1);

            itemRT.sizeDelta = slotRT.sizeDelta * (Vector2)itemData.size;

            itemRT.anchoredPosition = slotRT.anchoredPosition;

            itemRT.localScale = Vector3.one;
        }

        InventoryItemView view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
            view.Init(itemData, slotPosition);

        return true;
    }

    private bool IsOverlappingWithOtherItems(GameObject itemGO)
    {
        Collider2D itemCollider = itemGO.GetComponent<Collider2D>();
        if (itemCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Items"));
        filter.useTriggers = false;

        Collider2D[] results = new Collider2D[10];
        int count = itemCollider.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            if (results[i] != null && results[i].gameObject != itemGO)
                return true;
        }

        return false;
    }

    private bool GetGridPositionUnderMouse(out Vector2Int gridPos, Vector2Int itemSize = default)
    {
        gridPos = Vector2Int.zero;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(slotContainer, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        Vector2 offset = localPoint + slotContainer.rect.size * 0.5f;
        Vector2 cellSize = GetSlotSize();

        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        int x = Mathf.Clamp(Mathf.FloorToInt(offset.x / cellSize.x), 0, width - (itemSize.x > 0 ? itemSize.x : 1));
        int y = Mathf.Clamp(height - 1 - Mathf.FloorToInt(offset.y / cellSize.y), 0, height - (itemSize.y > 0 ? itemSize.y : 1));

        gridPos = new Vector2Int(x, y);
        return true;
    }

    private RectTransform GetSlotRect(Vector2Int gridPos)
    {
        int index = gridPos.y * columns + gridPos.x;
        if (index >= 0 && index < spawnedSlots.Count)
            return spawnedSlots[index].GetComponent<RectTransform>();
        return null;
    }

    public Vector2 GetSlotSize()
    {
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        return grid.cellSize;
    }

    public bool TryFindFreePosition(Vector2Int size, out Vector2Int position)
    {
        for (int y = 0; y <= rows - size.y; y++)
        {
            for (int x = 0; x <= columns - size.x; x++)
            {
                Vector2Int candidatePos = new Vector2Int(x, y);
                if (CanPlaceAt(candidatePos, size))
                {
                    position = candidatePos;
                    return true;
                }
            }
        }
        position = Vector2Int.zero;
        return false;
    }

    public bool GetGridPositionUnderWorld(Vector3 worldPosition, out Vector2Int gridPos)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        return GetGridPositionUnderMouse(out gridPos);
    }

    public bool MergeItems(GameObject firstItemGO, GameObject secondItemGO, InventoryItemData mergedData)
    {
        if (firstItemGO == null || secondItemGO == null || mergedData == null)
            return false;

        Destroy(firstItemGO);
        Destroy(secondItemGO);

        GameObject mergedItemGO = Instantiate(itemPrefab);
        mergedItemGO.transform.SetParent(itemsParent, false);

        var view = mergedItemGO.GetComponent<InventoryItemView>();
        if (view != null)
            view.Init(mergedData, Vector2Int.zero);

        if (IsColliding(mergedItemGO))
        {
            Debug.LogWarning("Collision detected when merging");
            Destroy(mergedItemGO);
            return false;
        }

        mergedItemGO.transform.SetParent(itemsParent, false);
        UpdateGridUsed();
        return true;
    }

    bool IsColliding(GameObject item)
    {
        Collider2D thisCol = item.GetComponent<Collider2D>();
        if (thisCol == null) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(item.layer));
        filter.useTriggers = false;

        Collider2D[] results = new Collider2D[10];
        int count = thisCol.Overlap(filter, results);

        foreach (var col in results)
        {
            if (col != null && col.gameObject != item)
            {
                return true;
            }
        }
        return false;
    }

    public bool TrySpawnItemWithColliderCheck(InventoryItemData itemData, out GameObject spawnedItem)
    {
        spawnedItem = null;

        if (itemData == null || itemData.itemPrefab == null)
            return false;

        for (int y = 0; y <= rows - itemData.size.y; y++)
        {
            for (int x = 0; x <= columns - itemData.size.x; x++)
            {
                Vector2Int candidatePos = new Vector2Int(x, y);

                // Проверяем можно ли поставить по логике сетки (занятость слотов)
                if (!CanPlaceAt(candidatePos, itemData.size))
                    continue;

                // Проверяем пересечения с другими предметами через коллайдеры
                if (CheckOverlapAtSlot(candidatePos, itemData, itemData.itemPrefab))
                    continue;

                // Всё ок — создаём объект
                GameObject newItem = Instantiate(itemData.itemPrefab, itemsParent);
                RectTransform slotRT = GetSlotRect(candidatePos);
                RectTransform newItemRT = newItem.GetComponent<RectTransform>();

                if (newItemRT != null && slotRT != null)
                {
                    newItemRT.pivot = new Vector2(0, 1);
                    newItemRT.anchorMin = new Vector2(0, 1);
                    newItemRT.anchorMax = new Vector2(0, 1);
                    newItemRT.sizeDelta = slotRT.sizeDelta * (Vector2)itemData.size;
                    newItemRT.anchoredPosition = slotRT.anchoredPosition;
                    newItemRT.localScale = Vector3.one;
                }

                InventoryItemView view = newItem.GetComponent<InventoryItemView>();
                if (view != null)
                    view.Init(itemData, candidatePos);

                MarkCells(candidatePos, itemData.size, true);

                spawnedItem = newItem;
                return true;
            }
        }

        return false;
    }

    private bool CheckOverlapAtSlot(Vector2Int slotPos, InventoryItemData itemData, GameObject prefab)
    {
        // Создаём временный объект для проверки коллизий
        GameObject tempObj = Instantiate(prefab, itemsParent);
        tempObj.SetActive(false);

        RectTransform slotRT = GetSlotRect(slotPos);
        RectTransform tempRT = tempObj.GetComponent<RectTransform>();

        if (tempRT != null && slotRT != null)
        {
            tempRT.pivot = new Vector2(0, 1);
            tempRT.anchorMin = new Vector2(0, 1);
            tempRT.anchorMax = new Vector2(0, 1);
            tempRT.sizeDelta = slotRT.sizeDelta * (Vector2)itemData.size;
            tempRT.anchoredPosition = slotRT.anchoredPosition;
            tempRT.localScale = Vector3.one;
        }

        tempObj.SetActive(true);

        bool hasOverlap = IsOverlappingWithOtherItems(tempObj);

        Destroy(tempObj);

        return hasOverlap;
    }
    public void ClearItemFromGrid(GameObject itemGO)
    {
        if (gridUsed == null || itemGO == null)
            return;

        RectTransform rt = itemGO.GetComponent<RectTransform>();
        if (rt == null)
            return;

        Vector2 slotSize = GetSlotSize();
        Vector2 localPos = rt.anchoredPosition;

        // Используем Floor для корректного определения начальной ячейки
        int startX = Mathf.FloorToInt(localPos.x / slotSize.x);
        int startY = rows - 1 - Mathf.FloorToInt(-localPos.y / slotSize.y);

        int sizeX = Mathf.CeilToInt(rt.sizeDelta.x / slotSize.x);
        int sizeY = Mathf.CeilToInt(rt.sizeDelta.y / slotSize.y);

        for (int dx = 0; dx < sizeX; dx++)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                int x = startX + dx;
                int y = startY + dy;

                if (x >= 0 && x < columns && y >= 0 && y < rows)
                {
                    gridUsed[x, y] = false;
                }
            }
        }
    }
    public Vector3 GetWorldPositionFromGrid(int x, int y)
    {
        Vector2 slotSize = GetSlotSize(); // метод должен уже быть в классе
        float posX = x * slotSize.x;
        float posY = -(rows - 1 - y) * slotSize.y;

        Vector2 anchoredPos = new Vector2(posX, posY);
        Vector3 worldPos = slotContainer.TransformPoint(anchoredPos);

        return worldPos;
    }


}
