using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackpackGridLayoutManager : MonoBehaviour
{
    public RectTransform slotContainer;  // Контейнер для слотов (с GridLayoutGroup)
    public GameObject slotPrefab;        // Префаб слота ячейки

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool[,] gridUsed;

    /// <summary>
    /// Создает сетку слотов по заданным параметрам
    /// </summary>
    /// <param name="dimension">Размер сетки (ширина x высота)</param>
    /// <param name="disabledIndices">Индексы отключенных ячеек (можно null)</param>
    /// <param name="maxCellSize">Максимальный размер ячейки</param>
    public void GenerateSlots(Vector2Int dimension, List<int> disabledIndices, int maxCellSize = 150)
    {
        ClearSlots();

        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("BackpackGridLayoutManager: Missing slotContainer or slotPrefab");
            return;
        }

        int width = dimension.x;
        int height = dimension.y;

        // Инициализируем массив занятости ячеек
        gridUsed = new bool[width, height];

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            Debug.LogError("BackpackGridLayoutManager: GridLayoutGroup missing on slotContainer");
            return;
        }

        Vector2 containerSize = slotContainer.rect.size;
        float availableWidth = containerSize.x - grid.padding.left - grid.padding.right - grid.spacing.x * (width - 1);
        float availableHeight = containerSize.y - grid.padding.top - grid.padding.bottom - grid.spacing.y * (height - 1);

        float cellWidth = availableWidth / width;
        float cellHeight = availableHeight / height;
        float cellSize = Mathf.Min(cellWidth, cellHeight);
        cellSize = Mathf.Min(cellSize, maxCellSize);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = width;
        grid.cellSize = new Vector2(cellSize, cellSize);

        HashSet<int> disabledSet = disabledIndices != null ? new HashSet<int>(disabledIndices) : new HashSet<int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cellIndex = y * width + x;
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}_idx{cellIndex}";
                spawnedSlots.Add(slot);

                bool isDisabled = disabledSet.Contains(cellIndex);

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

                // Если ячейка отключена, считаем её занятой
                gridUsed[x, y] = isDisabled;
            }
        }
    }

    /// <summary>
    /// Очищает ранее созданные слоты
    /// </summary>
    public void ClearSlots()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        spawnedSlots.Clear();

        gridUsed = null;
    }

    /// <summary>
    /// Возвращает RectTransform слота по позиции в сетке
    /// </summary>
    public RectTransform GetSlotRect(Vector2Int gridPos)
    {
        int width = slotContainer.GetComponent<GridLayoutGroup>().constraintCount;
        int index = gridPos.y * width + gridPos.x;

        if (index >= 0 && index < spawnedSlots.Count)
            return spawnedSlots[index].GetComponent<RectTransform>();

        return null;
    }

    /// <summary>
    /// Размер одной ячейки
    /// </summary>
    public Vector2 GetCellSize()
    {
        var grid = slotContainer.GetComponent<GridLayoutGroup>();
        return grid.cellSize;
    }

    /// <summary>
    /// Проверяет, свободна ли область в сетке заданного размера
    /// </summary>
    public bool IsAreaFree(Vector2Int position, Vector2Int size)
    {
        if (gridUsed == null)
            return false;

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

    /// <summary>
    /// Помечает область в сетке как занятую или свободную
    /// </summary>
    public void MarkAreaUsed(Vector2Int position, Vector2Int size, bool used)
    {
        if (gridUsed == null)
            return;

        int width = gridUsed.GetLength(0);
        int height = gridUsed.GetLength(1);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                int posX = position.x + x;
                int posY = position.y + y;

                if (posX >= 0 && posX < width && posY >= 0 && posY < height)
                    gridUsed[posX, posY] = used;
            }
        }
    }
}
