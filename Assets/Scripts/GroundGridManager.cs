using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GroundGridManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform slotContainer;  // ��������� � GridLayoutGroup ��� �����
    public GameObject slotPrefab;        // ������ ������, ��� ������� ��� � ����������
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

        // ������ ����� (���������)
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                slot.name = $"GroundSlot_{x}_{y}";
                spawnedSlots.Add(slot);

                // ������� ���� ��������� (��������, ���������� Image)
                Image img = slot.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(1, 1, 1, 0); // ����������
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

    // ����� �������� ������ TryPlaceItem, RemoveItem � �.�. ���������� �������
}
