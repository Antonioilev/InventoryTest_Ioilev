using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BackpackConfig", menuName = "Inventory/BackpackConfig", order = 1)]
public class BackpackConfig : ScriptableObject
{
    [Tooltip("������ ��������� �������� ��������")]
    public List<BackpackPreset> presets = new List<BackpackPreset>();

    /// <summary>
    /// ���������� ������� �������� ������ �������
    /// </summary>
    public BackpackPreset GetCurrentPreset()
    {
        foreach (var preset in presets)
        {
            if (preset.isCurrent)
                return preset;
        }

        Debug.LogWarning("BackpackConfig: �� ���� ������ �� ������� ��� ������� (isCurrent)");
        return null;
    }
}

[System.Serializable]
public class BackpackPreset
{
    public string name = "New Backpack";

    [Header("������ � ���������")]
    public Vector2Int dimension = new Vector2Int(4, 4); // ������ � ������ �����
    public List<Vector2Int> disabledCells = new();      // ����������� ������ (����������)

    [Header("������������")]
    public GameObject backpackVisualPrefab; // ������ ���� ������� (������ � ���������� ��������)

    [Header("������� ������")]
    public bool isCurrent; // ������� "������� ���������"
}
