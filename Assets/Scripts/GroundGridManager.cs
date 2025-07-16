using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GroundGridManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public Transform itemsParent;

    public int columns = 10;
    public int rows = 5;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool[,] gridUsed;

    private bool isDragging = false;
    private GameObject draggedItem = null;

    void Start()
    {
        GenerateGrid();
    }

    void Update()
    {
        // При перетаскивании постоянно обновляем массив занятости с исключением перетаскиваемого предмета
        if (isDragging && draggedItem != null)
        {
            UpdateGridUsed(draggedItem);
        }
    }

    // Вызывается при начале перетаскивания предмета
    public void StartDragging(GameObject item)
    {
        isDragging = true;
        draggedItem = item;

        // Можно сразу обновить занятость, чтобы освободить слоты под предметом
        UpdateGridUsed(draggedItem);
    }

    // Вызывается при завершении перетаскивания
    public void StopDragging()
    {
        isDragging = false;
        draggedItem = null;

        // Обновляем занятость без исключений - фиксируем текущие позиции всех предметов
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

        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;
        float cellSize = Mathf.Min(cellWidth, cellHeight);

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
                if (img != null)
                    img.color = new Color(1, 1, 1, 0.2f);

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
            if (slot != null)
                Destroy(slot);
        }
        spawnedSlots.Clear();
    }

    // Ключевая функция — обновляет занятость слотов, исключая перетаскиваемый предмет
    public void UpdateGridUsed(GameObject excludeItem = null)
    {
        if (gridUsed == null)
            return;

        // Сбрасываем все слоты как свободные
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                gridUsed[x, y] = false;

        Vector2 slotSize = GetSlotSize();

        foreach (Transform item in itemsParent)
        {
            if (excludeItem != null && item.gameObject == excludeItem)
                continue; // исключаем перетаскиваемый предмет

            RectTransform rt = item as RectTransform;
            if (rt == null) continue;

            Vector2 localPos = rt.anchoredPosition;

            int startX = Mathf.RoundToInt(localPos.x / slotSize.x);
            int startY = rows - 1 - Mathf.RoundToInt(localPos.y / slotSize.y);

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
                }
            }
        }
    }

    // Проверка, можно ли разместить предмет, при этом слоты занятые исключаемым предметом разрешаем (чтобы он мог возвращаться на свои слоты)
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
                        return false; // слот занят другим предметом
                }
            }
        }
        return true;
    }

    // Проверка, принадлежит ли ячейка (x,y) исключаемому предмету excludeItem
    private bool IsCellOccupiedByItemAtPosition(int x, int y, GameObject excludeItem)
    {
        if (excludeItem == null)
            return false;

        RectTransform rt = excludeItem.GetComponent<RectTransform>();
        if (rt == null)
            return false;

        Vector2 slotSize = GetSlotSize();
        Vector2 localPos = rt.anchoredPosition;

        int startX = Mathf.RoundToInt(localPos.x / slotSize.x);
        int startY = rows - 1 - Mathf.RoundToInt(localPos.y / slotSize.y);

        int sizeX = Mathf.CeilToInt(rt.sizeDelta.x / slotSize.x);
        int sizeY = Mathf.CeilToInt(rt.sizeDelta.y / slotSize.y);

        if (x >= startX && x < startX + sizeX && y >= startY && y < startY + sizeY)
            return true;

        return false;
    }

    private void MarkCells(Vector2Int slotPos, Vector2Int size, bool occupied)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                gridUsed[slotPos.x + dx, slotPos.y + dy] = occupied;
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

    public bool PlaceExistingItemAtMousePosition(InventoryItemData itemData, GameObject itemGO)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(slotContainer, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        Vector2 slotSize = GetSlotSize();
        Vector2 offset = localPoint + slotContainer.rect.size / 2f;

        int column = Mathf.FloorToInt(offset.x / slotSize.x);
        int rowRaw = Mathf.FloorToInt(offset.y / slotSize.y);
        int row = rows - 1 - rowRaw;

        Vector2Int gridPos = new Vector2Int(column, row);
        return PlaceExistingItem(gridPos, itemData, itemGO);
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
}
