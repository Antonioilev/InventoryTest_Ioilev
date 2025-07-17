using UnityEngine;

public class InventoryItemMergeHandler : MonoBehaviour
{
    public InventoryItemData itemData;
    public MergeRules mergeRules;

    public bool TryMergeWith(GameObject otherItemGO, out InventoryItemData result)
    {
        InventoryItemMergeHandler other = otherItemGO.GetComponent<InventoryItemMergeHandler>();
        result = null;

        if (other == null) return false;
        if (other.itemData != this.itemData) return false;

        return mergeRules.TryGetMergeResult(itemData, out result);
    }
}
