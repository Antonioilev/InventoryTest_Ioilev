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

        // ������������� itemGO �� ������� �����:
        RectTransform slotRect = gridLayout.GetSlotRect(position);
        RectTransform itemRT = itemGO.GetComponent<RectTransform>();
        if (slotRect != null && itemRT != null)
        {
            itemRT.pivot = new Vector2(0f, 1f);
            itemRT.anchorMin = new Vector2(0f, 1f);
            itemRT.anchorMax = new Vector2(0f, 1f);

            itemRT.sizeDelta = slotRect.sizeDelta * (Vector2)itemData.size;
            itemRT.anchoredPosition = slotRect.anchoredPosition;
            itemRT.localScale = Vector3.one;
        }

        gridLayout.MarkAreaUsed(position, itemData.size, true);

        InventoryItemView view = itemGO.GetComponent<InventoryItemView>();
        if (view != null)
            view.Init(itemData, position);

        return true;
    }

}
