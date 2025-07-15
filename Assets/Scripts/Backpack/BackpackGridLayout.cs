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
        // Здесь реализация генерации слотов (не показана)
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
