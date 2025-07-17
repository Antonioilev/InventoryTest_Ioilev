using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
    private bool[,] gridUsed;
    public Transform itemsParent;

    [Header("Config")]
    public BackpackConfig backpackConfig;

    [Header("UI References")]
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public Transform backgroundContainer;

    private List<GameObject> spawnedSlots = new();
    private GameObject currentBackgroundInstance;

    public int maxCellSize = 150;

    public int columns; // количество столбцов в рюкзаке
    public int rows;    // количество строк в рюкзаке

    void Start()
    {
        GenerateGrid();
    }

    public void UpdateGridUsed()
    {
        if (gridUsed == null)
            return;

        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        // Сбросим занятость
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                gridUsed[x, y] = false;

        Vector2 slotSize = GetSlotSize();

        foreach (Transform item in itemsParent)
        {
            RectTransform rt = item as RectTransform;
            if (rt == null)
                continue;

            Vector2 localPos = rt.anchoredPosition;

            int startX = Mathf.RoundToInt(localPos.x / slotSize.x);
            int startY = Mathf.RoundToInt(-localPos.y / slotSize.y);

            int sizeX = Mathf.CeilToInt(rt.sizeDelta.x / slotSize.x);
            int sizeY = Mathf.CeilToInt(rt.sizeDelta.y / slotSize.y);

            Debug.Log($"Item '{item.name}' pos({localPos.x:F2},{localPos.y:F2}), start cell ({startX},{startY}), size ({sizeX},{sizeY})");

            for (int dx = 0; dx < sizeX; dx++)
                for (int dy = 0; dy < sizeY; dy++)
                {
                    int x = startX + dx;
                    int y = startY + dy;

                    if (x >= 0 && x < width && y >= 0 && y < height)
                        gridUsed[x, y] = true;
                    else
                        Debug.LogWarning($"Item '{item.name}' cell out of bounds ({x},{y})");
                }
        }
    }

    public void GenerateGrid()
    {
        ClearGrid();

        if (backpackConfig == null || slotPrefab == null || slotContainer == null)
        {
            Debug.LogError("BackpackGridManager: Missing references");
            return;
        }

        var preset = backpackConfig.GetCurrentPreset();
        if (preset == null)
        {
            Debug.LogWarning("No active backpack preset found.");
            return;
        }

        // Инициализация количества столбцов и строк
        columns = preset.dimension.x;
        rows = preset.dimension.y;

        if (backgroundContainer != null && preset.backpackVisualPrefab != null)
        {
            if (currentBackgroundInstance != null)
                Destroy(currentBackgroundInstance);

            currentBackgroundInstance = Instantiate(preset.backpackVisualPrefab, backgroundContainer, false);
        }

        Vector2 containerSize = slotContainer.rect.size;
        int width = columns;  // Используем columns вместо preset.dimension.x
        int height = rows;    // Используем rows вместо preset.dimension.y

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            Debug.LogError("Missing GridLayoutGroup on slotContainer.");
            return;
        }

        float availableWidth = containerSize.x - grid.padding.left - grid.padding.right - grid.spacing.x * (width - 1);
        float availableHeight = containerSize.y - grid.padding.top - grid.padding.bottom - grid.spacing.y * (height - 1);
        float cellSize = Mathf.Min(availableWidth / width, availableHeight / height, maxCellSize);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = width;
        grid.cellSize = new Vector2(cellSize, cellSize);

        HashSet<int> disabledIndices = new(preset.disabledCellIndices ?? new List<int>());

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}_idx{idx}";
                spawnedSlots.Add(slot);

                RectTransform slotRT = slot.GetComponent<RectTransform>();
                if (slotRT != null)
                {
                    slotRT.pivot = new Vector2(0f, 1f);
                    slotRT.anchorMin = new Vector2(0f, 1f);
                    slotRT.anchorMax = new Vector2(0f, 1f);
                }

                bool isDisabled = disabledIndices.Contains(idx);

                if (slot.TryGetComponent(out Image img))
                {
                    Color c = img.color;
                    c.a = isDisabled ? 0f : 1f;
                    img.color = c;
                }

                Transform lockIcon = slot.transform.Find("LockIcon");
                if (lockIcon != null) lockIcon.gameObject.SetActive(isDisabled);

                if (!slot.TryGetComponent(out CanvasGroup cg))
                    cg = slot.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = !isDisabled;
            }
        }

        gridUsed = new bool[columns, rows];
    }



    public void ClearGrid()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot);
        spawnedSlots.Clear();

        if (currentBackgroundInstance != null)
        {
            Destroy(currentBackgroundInstance);
            currentBackgroundInstance = null;
        }
    }

    public void SetBackpackConfig(BackpackConfig config)
    {
        backpackConfig = config;
        GenerateGrid();
    }

    public bool TryPlaceItem(Vector2Int slotPos, InventoryItemData data)
    {
        if (data == null || slotPrefab == null || itemsParent == null || data.itemPrefab == null)
            return false;

        if (!CanPlaceAt(slotPos, data.size))
            return false;

        GameObject itemGO = Instantiate(data.itemPrefab, itemsParent, false);
        itemGO.name = $"Item_{data.itemId}";

        PlaceRectTransform(itemGO.GetComponent<RectTransform>(), slotPos, data.size);

        InventoryItemView view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
            view.Init(data, slotPos);

        MarkCells(slotPos, data.size, true);
        return true;
    }

    public bool PlaceExistingItem(Vector2Int slotPos, InventoryItemData data, GameObject itemGO)
    {
        if (data == null || itemGO == null || itemsParent == null)
            return false;

        if (!CanPlaceAt(slotPos, data.size))
            return false;

        itemGO.transform.SetParent(itemsParent, false);
        PlaceRectTransform(itemGO.GetComponent<RectTransform>(), slotPos, data.size);

        InventoryItemView view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
            view.Init(data, slotPos);

        MarkCells(slotPos, data.size, true);
        return true;
    }

    public bool TryPlaceItemAtMousePosition(InventoryItemData data)
    {
        if (!GetGridPositionUnderMouse(out Vector2Int gridPos, data.size))
            return false;

        return TryPlaceItem(gridPos, data);
    }

    public bool PlaceExistingItemAtMousePosition(InventoryItemData data, GameObject itemGO)
    {
        if (!GetGridPositionUnderMouse(out Vector2Int gridPos, data.size))
            return false;

        return PlaceExistingItem(gridPos, data, itemGO);
    }

    // Ключевой метод: максимально простой и точный выбор слота под курсором без лишних округлений
    private bool GetGridPositionUnderMouse(out Vector2Int gridPos, Vector2Int itemSize = default)
    {
        gridPos = Vector2Int.zero;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(slotContainer, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        // Смещение локальной точки к левому нижнему углу slotContainer (pivot слотов (0,1) — левый верхний)
        Vector2 offset = localPoint + slotContainer.rect.size * 0.5f;

        Vector2 cellSize = GetSlotSize();

        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        int x = Mathf.Clamp(Mathf.FloorToInt(offset.x / cellSize.x), 0, width - 1);

        // Инвертируем Y, чтобы верхний слот был y = 0, а не снизу
        int y = height - 1 - Mathf.Clamp(Mathf.FloorToInt(offset.y / cellSize.y), 0, height - 1);

        // Корректировка по вертикали для предметов выше 1 ячейки (чтобы верхний левый слот был на высоте курсора)
        if (itemSize != default && itemSize.y > 1)
            y = Mathf.Clamp(y, 0, height - 1);

        gridPos = new Vector2Int(x, y);
        return true;
    }

    private void PlaceRectTransform(RectTransform rt, Vector2Int slotPos, Vector2Int itemSize)
    {
        if (rt == null) return;

        rt.pivot = new Vector2(0f, 1f);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);

        Vector2 slotSize = GetSlotSize();
        rt.sizeDelta = new Vector2(slotSize.x * itemSize.x, slotSize.y * itemSize.y);

        RectTransform slotRect = GetSlotRect(slotPos);
        if (slotRect != null)
            rt.anchoredPosition = slotRect.anchoredPosition;
    }

    private bool CanPlaceAt(Vector2Int slotPos, Vector2Int size)
    {
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        if (slotPos.x + size.x > width || slotPos.y + size.y > height)
            return false;

        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                if (gridUsed[slotPos.x + dx, slotPos.y + dy])
                    return false;

        return true;
    }

    private void MarkCells(Vector2Int slotPos, Vector2Int size, bool occupied)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                gridUsed[slotPos.x + dx, slotPos.y + dy] = occupied;
    }

    public RectTransform GetSlotRect(Vector2Int pos)
    {
        int width = gridUsed.GetLength(0);
        int index = pos.y * width + pos.x;

        if (index >= 0 && index < spawnedSlots.Count)
            return spawnedSlots[index].GetComponent<RectTransform>();

        return null;
    }

    public Vector2 GetSlotSize()
    {
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        return grid != null ? grid.cellSize : Vector2.one * 100f;
    }

    public void UpdateSlotHighlightsForItem(InventoryItemData itemData)
    {
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);
        ClearAllSlotHighlights();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int pos = new(x, y);
                if (CanPlaceAt(pos, itemData.size))
                {
                    RectTransform slot = GetSlotRect(pos);
                    if (slot != null && slot.TryGetComponent(out Image img))
                    {
                        img.color = new Color(0.4f, 1f, 0.4f, 1f);
                    }
                }
            }
        }
    }

    public void ClearAllSlotHighlights()
    {
        foreach (var slotGO in spawnedSlots)
        {
            if (slotGO.TryGetComponent(out Image img))
            {
                Color c = img.color;
                c.r = 1f;
                c.g = 1f;
                c.b = 1f;
                c.a = img.raycastTarget ? 1f : 0f;
                img.color = c;
            }
        }
    }
    public bool TryFindFreePosition(Vector2Int size, out Vector2Int foundPos)
    {
        // Предполагаем, что в BackpackGridManager есть поля:
        // int columns, rows;
        // bool[,] gridUsed; // занятость ячеек

        for (int y = 0; y <= rows - size.y; y++)
        {
            for (int x = 0; x <= columns - size.x; x++)
            {
                bool canPlace = true;

                for (int dx = 0; dx < size.x; dx++)
                {
                    for (int dy = 0; dy < size.y; dy++)
                    {
                        if (gridUsed[x + dx, y + dy])
                        {
                            canPlace = false;
                            break;
                        }
                    }
                    if (!canPlace)
                        break;
                }

                if (canPlace)
                {
                    foundPos = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        foundPos = Vector2Int.zero;
        return false;
    }


}
