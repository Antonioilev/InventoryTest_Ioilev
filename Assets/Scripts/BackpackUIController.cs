using UnityEngine;

public class BackpackUIController : MonoBehaviour
{
    [Header("References")]
    public BackpackGridManager gridManager;        // ������ �� �������� �����
    public BackpackConfig backpackConfig;          // ��� �� ScriptableObject, ������� �������� � GridManager

    public void SwitchToPreset(int index)
    {
        if (backpackConfig == null || gridManager == null)
        {
            Debug.LogError("BackpackUIController: Missing references");
            return;
        }

        // ������������� ����� ������ �� �������
        backpackConfig.SetPresetByIndex(index);

        // ������������� �����
        gridManager.GenerateGrid();
    }
}
