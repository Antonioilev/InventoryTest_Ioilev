using UnityEngine;

[CreateAssetMenu(fileName = "MergeRules", menuName = "Inventory/Merge Rules", order = 3)]
public class MergeRules : ScriptableObject
{
    [System.Serializable]
    public class MergeEntry
    {
        public InventoryItemData inputItem;      // Что нужно 2 штуки
        public InventoryItemData resultItem;     // Что получится
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
