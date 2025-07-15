using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BackpackConfig", menuName = "Inventory/BackpackConfig", order = 1)]
public class BackpackConfig : ScriptableObject
{
    [Tooltip("Список доступных пресетов рюкзаков")]
    public List<BackpackPreset> presets = new List<BackpackPreset>();

    /// <summary>
    /// Возвращает текущий активный пресет рюкзака
    /// </summary>
    public BackpackPreset GetCurrentPreset()
    {
        foreach (var preset in presets)
        {
            if (preset.isCurrent)
                return preset;
        }

        Debug.LogWarning("BackpackConfig: Ни один пресет не помечен как текущий (isCurrent)");
        return null;
    }
}

[System.Serializable]
public class BackpackPreset
{
    public string name = "New Backpack";

    [Header("Размер и структура")]
    public Vector2Int dimension = new Vector2Int(4, 4); // Ширина и высота сетки
    public List<Vector2Int> disabledCells = new();      // Отключённые ячейки (координаты)

    [Header("Визуализация")]
    public GameObject backpackVisualPrefab; // Префаб фона рюкзака (спрайт и визуальные элементы)

    [Header("Текущий пресет")]
    public bool isCurrent; // Галочка "текущий выбранный"
}
