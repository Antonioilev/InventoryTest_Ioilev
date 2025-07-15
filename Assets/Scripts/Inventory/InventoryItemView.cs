using UnityEngine;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour
{
    public InventoryItemData itemData;
    public Image iconImage;

    [HideInInspector] public Vector2Int slotPosition;
    [HideInInspector] public Vector2Int occupiedSize;

    void Awake()
    {
        // ���� iconImage �� �������� � ����������, �������� �������� ��������� �������������
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }

    public void Init(InventoryItemData data, Vector2Int position)
    {
        itemData = data;
        slotPosition = position;
        occupiedSize = itemData.size;

        if (iconImage != null && itemData != null)
        {
            iconImage.sprite = itemData.icon;
        }
        else
        {
            Debug.LogWarning("InventoryItemView: iconImage or itemData is null");
        }

        // ����������� ������ ��� ������ � ������������� ����� ��������
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
        }
    }
}
