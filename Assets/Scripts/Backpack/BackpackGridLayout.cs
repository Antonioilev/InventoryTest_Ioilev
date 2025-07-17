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

        gridUsed = new bool[width, height]; // Инициализация массива занятости

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

        // Создаём слоты
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}";

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
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        spawnedSlots.Clear();
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

    public bool IsAreaFree(Vector2Int position, Vector2Int size)
    {
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        if (position.x < 0 || position.y < 0 || position.x + size.x > width || position.y + size.y > height)
            return false;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (gridUsed[position.x + x, position.y + y])
                    return false;
            }
        }

        return true;
    }

    public void MarkAreaUsed(Vector2Int position, Vector2Int size, bool used)
    {
        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                int posX = position.x + x;
                int posY = position.y + y;

                if (posX >= 0 && posX < width && posY >= 0 && posY < height)
                {
                    gridUsed[posX, posY] = used;
                }
            }
        }
    }
}
