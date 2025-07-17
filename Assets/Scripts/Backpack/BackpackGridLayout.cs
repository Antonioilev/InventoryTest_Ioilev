using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackpackGridLayout : MonoBehaviour
{
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool[,] gridUsed;

    public void GenerateGrid(BackpackPreset preset)
    {
        ClearGrid();

        if (preset == null || slotPrefab == null || slotContainer == null)
        {
            Debug.LogError("Missing references for grid generation.");
            return;
        }

        int width = preset.dimension.x;
        int height = preset.dimension.y;

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            Debug.LogError("Missing GridLayoutGroup on slotContainer.");
            return;
        }

        Vector2 containerSize = slotContainer.rect.size;

        float totalPaddingX = grid.padding.left + grid.padding.right;
        float totalPaddingY = grid.padding.top + grid.padding.bottom;

        float totalSpacingX = grid.spacing.x * (width - 1);
        float totalSpacingY = grid.spacing.y * (height - 1);

        // Вычисляем размер ячейки с учётом паддинга и spacing
        float cellWidth = (containerSize.x - totalPaddingX - totalSpacingX) / width;
        float cellHeight = (containerSize.y - totalPaddingY - totalSpacingY) / height;

        float cellSize = Mathf.Min(cellWidth, cellHeight);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = width;
        grid.cellSize = new Vector2(cellSize, cellSize);

        // Затем инстанциируем слоты, позиционирование будет уже корректным с учетом настроек GridLayoutGroup
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}";

                // Здесь можно дополнительно выставить pivot/anchor, если надо
                RectTransform slotRT = slot.GetComponent<RectTransform>();
                if (slotRT != null)
                {
                    slotRT.pivot = new Vector2(0f, 1f);
                    slotRT.anchorMin = new Vector2(0f, 1f);
                    slotRT.anchorMax = new Vector2(0f, 1f);
                }

                spawnedSlots.Add(slot);
            }
        }
    }

    public void ClearGrid()
    {
        // Очистка слотов (не показана)
    }

    public RectTransform GetSlotRect(Vector2Int gridPos)
    {
        int width = gridUsed.GetLength(0);
        int index = gridPos.y * width + gridPos.x;

        if (index >= 0 && index < spawnedSlots.Count)
        {
            return spawnedSlots[index].GetComponent<RectTransform>();
        }

        // Возвращаем null, если не нашли
        return null;
    }

    public bool IsAreaFree(Vector2Int position, Vector2Int size)
    {
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        // Проверяем выход за границы
        if (position.x < 0 || position.y < 0 || position.x + size.x > width || position.y + size.y > height)
            return false;

        // Проверяем все клетки на занятость
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (gridUsed[position.x + x, position.y + y])
                    return false;
            }
        }

        return true; // Если все свободны
    }

    public void MarkAreaUsed(Vector2Int position, Vector2Int size, bool used)
    {
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (position.x + x < width && position.y + y < height)
                    gridUsed[position.x + x, position.y + y] = used;
            }
        }
    }
}
