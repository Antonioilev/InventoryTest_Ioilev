using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData", order = 2)]
public class InventoryItemData : ScriptableObject
{
    public string itemId = "sword";
    public Sprite icon;
    public Vector2Int size = new Vector2Int(1, 1); // �������� (1,2) � ������������ ���
    public GameObject itemPrefab;  // ������ �������� � ������������ Drag, Image, Collider � �.�.

    public bool rotatable = true; // ����� �� ������������
}
