using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
    private bool[,] gridUsed;
    public Transform itemsParent; // Контейнер для инстанцированных предметов

    [Header("Config")]
    public BackpackConfig backpackConfig;

    [Header("UI References")]
    public RectTransform slotContainer; // Панель с GridLayoutGroup
    public GameObject slotPrefab;       // Префаб одной ячейки
    public Transform backgroundContainer; // Контейнер под визуал рюкзака (в Canvas под ячейками)

    private List<GameObject> spawnedSlots = new List<GameObject>();
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

        // Создаем фон рюкзака
        if (backgroundContainer != null && preset.backpackVisualPrefab != null)
        {
            if (currentBackgroundInstance != null)
            {
                Destroy(currentBackgroundInstance);
                currentBackgroundInstance = null;
            }
            GameObject prefabInstance = Instantiate(preset.backpackVisualPrefab);
            prefabInstance.transform.SetParent(backgroundContainer, false);
            currentBackgroundInstance = prefabInstance;
        }

        Vector2 containerSize = slotContainer.rect.size;
        int width = preset.dimension.x;
        int height = preset.dimension.y;

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            Debug.LogError("GridLayoutGroup component missing on slotContainer");
            return;
        }

        float availableWidth = containerSize.x - grid.padding.left - grid.padding.right - grid.spacing.x * (width - 1);
        float availableHeight = containerSize.y - grid.padding.top - grid.padding.bottom - grid.spacing.y * (height - 1);

        float cellWidth = availableWidth / width;
        float cellHeight = availableHeight / height;
        float cellSize = Mathf.Min(cellWidth, cellHeight);
        cellSize = Mathf.Min(cellSize, maxCellSize);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = width;
        grid.cellSize = new Vector2(cellSize, cellSize);

        HashSet<int> disabledIndicesSet = new HashSet<int>(preset.disabledCellIndices ?? new List<int>());

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cellIndex = y * width + x;
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}_idx{cellIndex}";
                spawnedSlots.Add(slot);

                bool isDisabled = disabledIndicesSet.Contains(cellIndex);

                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null)
                {
                    Color c = slotImage.color;
                    c.a = isDisabled ? 0f : 1f;
                    slotImage.color = c;
                }

                Transform lockIcon = slot.transform.Find("LockIcon");
                if (lockIcon != null)
                    lockIcon.gameObject.SetActive(isDisabled);

                CanvasGroup cg = slot.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = slot.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = !isDisabled;
            }
        }

        gridUsed = new bool[width, height];
    }

    public void ClearGrid()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
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

    public bool TryPlaceItem(Vector2Int slotPosition, InventoryItemData itemData)
    {
        if (itemData == null || slotPrefab == null || itemsParent == null || itemData.itemPrefab == null)
            return false;

        var size = itemData.size;
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        if (slotPosition.x + size.x > width || slotPosition.y + size.y > height)
            return false;

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                if (gridUsed[slotPosition.x + dx, slotPosition.y + dy])
                    return false;
            }
        }

        // Инстанцируем префаб предмета
        GameObject itemGO = Instantiate(itemData.itemPrefab, itemsParent, false);
        itemGO.name = $"Item_{itemData.itemId}";

        // Получаем RectTransform и выравниваем позицию
        RectTransform rt = itemGO.GetComponent<RectTransform>();
        var slot = GetSlotRect(slotPosition);
        if (rt != null && slot != null)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            rt.sizeDelta = slot.sizeDelta * (Vector2)itemData.size;
            rt.anchoredPosition = slot.anchoredPosition;
        }

        // Убеждаемся, что на предмете есть InventoryItemView и инициализируем
        InventoryItemView view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
        {
            view.Init(itemData, slotPosition);
        }
        else
        {
            Debug.LogWarning("TryPlaceItem: InventoryItemView missing on itemPrefab");
        }

        // Проверяем наличие компонента для Drag (InventoryItemDraggable), чтобы предмет был интерактивен
        InventoryItemDraggable drag = itemGO.GetComponent<InventoryItemDraggable>();
        if (drag == null)
        {
            Debug.LogWarning("TryPlaceItem: InventoryItemDraggable missing on itemPrefab");
        }

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                gridUsed[slotPosition.x + dx, slotPosition.y + dy] = true;
            }
        }

        return true;
    }



    public RectTransform GetSlotRect(Vector2Int gridPos)
    {
        int width = gridUsed.GetLength(0);
        int index = gridPos.y * width + gridPos.x;

        if (index >= 0 && index < spawnedSlots.Count)
        {
            return spawnedSlots[index].GetComponent<RectTransform>();
        }

        return null;
    }

    public bool TryPlaceItemAtMousePosition(InventoryItemData itemData, RectTransform draggedRect)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(slotContainer, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        Vector2 slotSize = GetSlotSize();
        Vector2 offset = localPoint + slotContainer.rect.size / 2f;

        int column = Mathf.FloorToInt(offset.x / slotSize.x);
        int row = Mathf.FloorToInt(offset.y / slotSize.y);

        Vector2Int gridPos = new Vector2Int(column, row);

        return TryPlaceItem(gridPos, itemData);
    }

    public Vector2 GetSlotSize()
    {
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        return grid.cellSize;
    }
}
