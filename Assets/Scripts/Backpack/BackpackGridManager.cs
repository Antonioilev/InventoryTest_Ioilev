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

    void Start()
    {
        GenerateGrid();
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

        if (backgroundContainer != null && preset.backpackVisualPrefab != null)
        {
            if (currentBackgroundInstance != null)
                Destroy(currentBackgroundInstance);

            currentBackgroundInstance = Instantiate(preset.backpackVisualPrefab, backgroundContainer, false);
        }

        Vector2 containerSize = slotContainer.rect.size;
        int width = preset.dimension.x;
        int height = preset.dimension.y;

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

                // Добавляем сюда — ставим пивот и анкоры в левый верхний угол
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

        gridUsed = new bool[width, height];
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
        if (!GetGridPositionUnderMouse(out Vector2Int gridPos))
            return false;

        return TryPlaceItem(gridPos, data);
    }

    public bool PlaceExistingItemAtMousePosition(InventoryItemData data, GameObject itemGO)
    {
        if (!GetGridPositionUnderMouse(out Vector2Int gridPos))
            return false;

        return PlaceExistingItem(gridPos, data, itemGO);
    }

    private bool GetGridPositionUnderMouse(out Vector2Int gridPos)
    {
        gridPos = Vector2Int.zero;
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(slotContainer, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        Vector2 offset = localPoint + slotContainer.rect.size / 2f;
        Vector2 cellSize = GetSlotSize();
        int x = Mathf.FloorToInt(offset.x / cellSize.x);
        int y = Mathf.FloorToInt(offset.y / cellSize.y);
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
}
