using UnityEngine;

[CreateAssetMenu(fileName = "MergeRules", menuName = "Inventory/Merge Rules", order = 3)]
public class MergeRules : ScriptableObject
{
    [System.Serializable]
    public class MergeEntry
    {
        public InventoryItemData inputItem;      // ��� ����� 2 �����
        public InventoryItemData resultItem;     // ��� ���������
    }

    public MergeEntry[] rules;

    public bool TryGetMergeResult(InventoryItemData input, out InventoryItemData result)
    {
        foreach (var entry in rules)
        {
            if (entry.inputItem == input)
            {
                result = entry.resultItem;
                return true;
            }
        }

        result = null;
        return false;
    }
}
