using UnityEngine;

public class BackpackUIController : MonoBehaviour
{
    [Header("References")]
    public BackpackGridManager gridManager;        // Ссылка на менеджер сетки
    public BackpackConfig backpackConfig;          // Тот же ScriptableObject, который назначен в GridManager

    public void SwitchToPreset(int index)
    {
        if (backpackConfig == null || gridManager == null)
        {
            Debug.LogError("BackpackUIController: Missing references");
            return;
        }

        // Устанавливаем новый пресет по индексу
        backpackConfig.SetPresetByIndex(index);

        // Перестраиваем сетку
        gridManager.GenerateGrid();
    }
}
