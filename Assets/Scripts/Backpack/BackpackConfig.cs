using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BackpackConfig", menuName = "Inventory/BackpackConfig", order = 1)]
public class BackpackConfig : ScriptableObject
{
    [Tooltip("Список доступных пресетов рюкзаков")]
    public List<BackpackPreset> presets = new List<BackpackPreset>();

    /// <summary>
    /// Возвращает текущий активный пресет рюкзака (isCurrent = true)
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

    /// <summary>
    /// Помечает только один пресет как активный, по индексу
    /// </summary>
    public void SetPresetByIndex(int index)
    {
        if (index < 0 || index >= presets.Count)
        {
            Debug.LogWarning($"BackpackConfig: Попытка установить пресет по невалидному индексу {index}");
            return;
        }

        for (int i = 0; i < presets.Count; i++)
        {
            presets[i].isCurrent = (i == index);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // сохранение изменений в редакторе
#endif
    }

    /// <summary>
    /// Обеспечивает, что только один пресет помечен как текущий (isCurrent).
    /// Если несколько, сбрасывает все, кроме первого найденного.
    /// </summary>
    public void EnsureOnlyOneCurrent()
    {
        bool foundCurrent = false;
        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i].isCurrent)
            {
                if (foundCurrent)
                {
                    presets[i].isCurrent = false;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }
                else
                {
                    foundCurrent = true;
                }
            }
        }
    }
}

[System.Serializable]
public class BackpackPreset
{
    public string name = "New Backpack";

    [Header("Размер и структура")]
    public Vector2Int dimension = new Vector2Int(4, 4);

    [Tooltip("Индексы ячеек, которые нужно отключить (от 0 до width*height - 1)")]
    public List<int> disabledCellIndices = new List<int>();

    [Header("Визуализация")]
    public GameObject backpackVisualPrefab;

    [Header("Текущий пресет")]
    public bool isCurrent;
}
