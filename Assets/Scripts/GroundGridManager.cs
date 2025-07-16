using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GroundGridManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform slotContainer;  // Контейнер с GridLayoutGroup для земли
    public GameObject slotPrefab;        // Префаб ячейки, без визуала или с прозрачным
    public Transform itemsParent;        // Контейнер для предметов на земле

    public int columns = 10;
    public int rows = 5;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool[,] gridUsed;

    void Start()
    {
        GenerateGrid();
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

        // Создаём слоты (невидимые)
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"GroundSlot_{x}_{y}";
                spawnedSlots.Add(slot);

                Image img = slot.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(1, 1, 1, 0); // прозрачный

                // Устанавливаем пивот и анкоры слота в левый верхний угол
                RectTransform slotRT = slot.GetComponent<RectTransform>();
                if (slotRT != null)
                {
                    slotRT.pivot = new Vector2(0, 1);
                    slotRT.anchorMin = new Vector2(0, 1);
                    slotRT.anchorMax = new Vector2(0, 1);
                }
            }
        }
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

    public bool PlaceExistingItem(Vector2Int slotPosition, InventoryItemData itemData, GameObject itemGO)
    {
        if (itemData == null || itemGO == null || itemsParent == null)
            return false;

        Vector2Int size = itemData.size;
        if (slotPosition.x + size.x > columns || slotPosition.y + size.y > rows)
            return false;

        // Проверяем, свободны ли ячейки
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                if (gridUsed[slotPosition.x + dx, slotPosition.y + dy])
                    return false;
            }
        }

        // Помечаем ячейки как занятые
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
                gridUsed[slotPosition.x + dx, slotPosition.y + dy] = true;

        // Перемещаем предмет в родителя и позиционируем
        itemGO.transform.SetParent(itemsParent, false);

        RectTransform itemRT = itemGO.GetComponent<RectTransform>();
        RectTransform slotRT = GetSlotRect(slotPosition);

        if (itemRT != null && slotRT != null)
        {
            // Пивот и анкоры в левый верхний угол для предмета
            itemRT.pivot = new Vector2(0, 1);
            itemRT.anchorMin = new Vector2(0, 1);
            itemRT.anchorMax = new Vector2(0, 1);

            // Размер предмета с учётом размера слота и занимаемых ячеек
            itemRT.sizeDelta = slotRT.sizeDelta * (Vector2)itemData.size;

            // Позиция предмета совпадает с позицией слота
            itemRT.anchoredPosition = slotRT.anchoredPosition;

            // Сбрасываем локальный масштаб, чтобы избежать искажений при смене родителя
            itemRT.localScale = Vector3.one;
        }

        InventoryItemView view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
        {
            view.Init(itemData, slotPosition);
        }

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
        int row = Mathf.FloorToInt(offset.y / slotSize.y);

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
