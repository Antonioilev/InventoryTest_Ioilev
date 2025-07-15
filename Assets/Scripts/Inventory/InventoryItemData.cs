using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData", order = 2)]
public class InventoryItemData : ScriptableObject
{
    public string itemId = "sword";
    public Sprite icon;
    public Vector2Int size = new Vector2Int(1, 1); // например (1,2) — вертикальный меч
    public GameObject itemPrefab;  // Префаб предмета с компонентами Drag, Image, Collider и т.п.

    public bool rotatable = true; // можно ли поворачивать
}
