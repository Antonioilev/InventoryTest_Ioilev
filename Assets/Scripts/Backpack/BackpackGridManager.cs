using UnityEngine;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
    public RectTransform SlotContainer => gridLayoutManager.slotContainer;
    private bool[,] gridUsed;
    public Transform itemsParent;

    [Header("Config")]
    public BackpackConfig backpackConfig;

    [Header("References")]
    public BackpackGridLayoutManager gridLayoutManager;   // Новый скрипт для работы со слотами
    public Transform backgroundContainer;

    private GameObject currentBackgroundInstance;

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        ClearGrid();

        if (backpackConfig == null)
        {
            Debug.LogError("BackpackGridManager: Missing backpackConfig");
            return;
        }

        var preset = backpackConfig.GetCurrentPreset();
        if (preset == null)
        {
            Debug.LogWarning("No active backpack preset found.");
            return;
        }

        // Визуал рюкзака
        if (backgroundContainer != null && preset.backpackVisualPrefab != null)
        {
            if (currentBackgroundInstance != null)
            {
                Destroy(currentBackgroundInstance);
                currentBackgroundInstance = null;
            }
            GameObject prefabInstance = Instantiate(preset.backpackVisualPrefab, backgroundContainer, false);
            currentBackgroundInstance = prefabInstance;
        }

        // Создаем слоты через LayoutManager
        gridLayoutManager.GenerateSlots(preset.dimension, preset.disabledCellIndices);

        // Инициализируем массив занятости ячеек
        gridUsed = new bool[preset.dimension.x, preset.dimension.y];
    }

    public void ClearGrid()
    {
        gridLayoutManager.ClearSlots();

        if (currentBackgroundInstance != null)
        {
            Destroy(currentBackgroundInstance);
            currentBackgroundInstance = null;
        }
    }

    public bool TryPlaceItem(Vector2Int slotPosition, InventoryItemData itemData)
    {
        if (itemData == null || itemsParent == null || itemData.itemPrefab == null)
            return false;

        Vector2Int size = itemData.size;
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

        GameObject itemGO = Instantiate(itemData.itemPrefab, itemsParent, false);
        itemGO.name = $"Item_{itemData.itemId}";

        RectTransform rt = itemGO.GetComponent<RectTransform>();
        var slotRect = gridLayoutManager.GetSlotRect(slotPosition);
        if (rt != null && slotRect != null)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            rt.sizeDelta = slotRect.sizeDelta * (Vector2)size;
            rt.anchoredPosition = slotRect.anchoredPosition;
        }

        var view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
        {
            view.Init(itemData, slotPosition);
        }
        else
        {
            Debug.LogWarning("TryPlaceItem: InventoryItemView missing on itemPrefab");
        }

        var drag = itemGO.GetComponent<InventoryItemDraggable>();
        if (drag == null)
        {
            Debug.LogWarning("TryPlaceItem: InventoryItemDraggable missing on itemPrefab");
        }

        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                gridUsed[slotPosition.x + dx, slotPosition.y + dy] = true;

        return true;
    }

    public bool TryPlaceItemAtMousePosition(InventoryItemData itemData, RectTransform draggedRect)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridLayoutManager.slotContainer, Input.mousePosition, cam, out Vector2 localPoint))
            return false;

        Vector2 slotSize = gridLayoutManager.GetCellSize();
        Vector2 offset = localPoint + gridLayoutManager.slotContainer.rect.size / 2f;

        int column = Mathf.FloorToInt(offset.x / slotSize.x);
        int row = Mathf.FloorToInt(offset.y / slotSize.y);

        Vector2Int gridPos = new Vector2Int(column, row);

        return TryPlaceItem(gridPos, itemData);
    }

    public bool IsPointerOverSlotContainer(Vector2 screenPosition)
    {
        if (SlotContainer == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(SlotContainer, screenPosition, cam);
    }
}
