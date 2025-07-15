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
    public Vector2Int dimension = new Vector2Int(4, 4); // Ширина и высота
    public List<Vector2Int> disabledCells = new(); // Отключённые ячейки
    public Sprite backpackSprite; // Фоновый спрайт рюкзака
    public bool isCurrent; // Отметка активного пресета
}
