using UnityEngine;                 // Для MonoBehaviour, Vector2Int, GameObject и т.п.
using UnityEngine.UI;              // Для UI-элементов, если нужны
using System.Collections.Generic; // Для List, HashSet и других коллекций
public class BackpackItemPlacer : MonoBehaviour
{
    public BackpackGridLayout gridLayout;
    public Transform itemsParent;

    public bool TryPlaceItem(Vector2Int position, InventoryItemData itemData)
    {
        if (!gridLayout.IsAreaFree(position, itemData.size))
            return false;

        GameObject itemGO = Instantiate(itemData.itemPrefab, itemsParent, false);
        // позиционирование itemGO относительно gridLayout.GetSlotRect(position)
        gridLayout.MarkAreaUsed(position, itemData.size, true);
        // инициализация InventoryItemView и прочее
        return true;
    }
}
