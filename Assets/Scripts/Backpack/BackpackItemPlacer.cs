using UnityEngine;                 // ��� MonoBehaviour, Vector2Int, GameObject � �.�.
using UnityEngine.UI;              // ��� UI-���������, ���� �����
using System.Collections.Generic; // ��� List, HashSet � ������ ���������
public class BackpackItemPlacer : MonoBehaviour
{
    public BackpackGridLayout gridLayout;
    public Transform itemsParent;

    public bool TryPlaceItem(Vector2Int position, InventoryItemData itemData)
    {
        if (!gridLayout.IsAreaFree(position, itemData.size))
            return false;

        GameObject itemGO = Instantiate(itemData.itemPrefab, itemsParent, false);
        // ���������������� itemGO ������������ gridLayout.GetSlotRect(position)
        gridLayout.MarkAreaUsed(position, itemData.size, true);
        // ������������� InventoryItemView � ������
        return true;
    }
}
