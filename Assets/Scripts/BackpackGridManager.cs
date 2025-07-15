using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
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

        // Копируем disabled индексы в HashSet для быстрого поиска
        HashSet<int> disabledIndicesSet = new HashSet<int>();
        if (preset.disabledCellIndices != null)
        {
            disabledIndicesSet = new HashSet<int>(preset.disabledCellIndices);
        }

        // Создаем все ячейки
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cellIndex = y * width + x;

                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}_idx{cellIndex}";
                spawnedSlots.Add(slot);

                bool isDisabled = disabledIndicesSet.Contains(cellIndex);

                // Отладка
                Debug.Log($"Cell {cellIndex} disabled: {isDisabled}");

                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null)
                {
                    Color c = slotImage.color;
                    c.a = isDisabled ? 0f : 1f;  // прозрачность для выключенных
                    slotImage.color = c;
                }

                Transform lockIcon = slot.transform.Find("LockIcon");
                if (lockIcon != null)
                    lockIcon.gameObject.SetActive(isDisabled);

                CanvasGroup cg = slot.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = slot.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = !isDisabled; // блокируем взаимодействие для выключенных
            }
        }

        // После создания ячеек отключаем GridLayoutGroup, чтобы зафиксировать их позицию
        //grid.enabled = false;

        // ВАЖНО: НЕ выключаем объекты SetActive(false), иначе сетка сожмётся!
        // Если хочется визуально "выключить", используем прозрачность и блокировку.
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
}
