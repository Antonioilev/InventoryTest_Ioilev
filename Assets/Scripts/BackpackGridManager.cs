using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
    [Header("Config")]
    public BackpackConfig backpackConfig;

    [Header("UI References")]
    public RectTransform slotContainer; // ������ � GridLayoutGroup
    public GameObject slotPrefab;       // ������ ����� ������
    public Transform backgroundContainer; // ��������� ��� ������ ������� (� Canvas ��� ��������)

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private GameObject currentBackgroundInstance;

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

        // ������� ��� �� �������
        if (backgroundContainer != null)
        {
            // ������� ����������
            if (currentBackgroundInstance != null)
            {
                Destroy(currentBackgroundInstance);
                currentBackgroundInstance = null;
            }

            // ������� �����
            if (preset.backpackVisualPrefab != null)
            {
                currentBackgroundInstance = Instantiate(preset.backpackVisualPrefab, backgroundContainer);
                currentBackgroundInstance.name = $"BackpackVisual_{preset.name}";
            }
        }

        // ��������� �����
        Vector2 containerSize = slotContainer.rect.size;

        int width = preset.dimension.x;
        int height = preset.dimension.y;

        float cellWidth = containerSize.x / width;
        float cellHeight = containerSize.y / height;
        float cellSize = Mathf.Min(cellWidth, cellHeight);

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = width;
            grid.cellSize = new Vector2(cellSize, cellSize);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                if (preset.disabledCells.Contains(cellPos))
                    continue;

                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"Slot_{x}_{y}";
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

        // ������ ���
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
