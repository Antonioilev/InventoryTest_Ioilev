using UnityEngine;

public class Generator_Items : MonoBehaviour
{
    public GroundGridManager groundGridManager;
    public BackpackGridManager backpackGridManager;

    [Header("Предметы")]
    public InventoryItemData sword;
    public InventoryItemData knife;
    public InventoryItemData screwdriver;
    public InventoryItemData ball;

    /// <summary>
    /// Спавнит предмет на земле, если есть место и нет коллизий
    /// </summary>
    /// <param name="itemData">ScriptableObject с данными предмета</param>
    /// <returns>Созданный GameObject или null</returns>
    public GameObject TrySpawnItem(InventoryItemData itemData)
    {
        if (itemData == null || itemData.itemPrefab == null)
        {
            Debug.LogWarning("Generator_Items: Не указан itemData или prefab");
            return null;
        }

        if (groundGridManager.TrySpawnItemWithColliderCheck(itemData, out GameObject newItem))
        {
            // 👉 Назначаем itemData и менеджеры вручную
            InventoryItemDraggable draggable = newItem.GetComponent<InventoryItemDraggable>();
            if (draggable != null)
            {
                draggable.itemData = itemData;
                draggable.groundGridManager = groundGridManager;
                draggable.backpackGridManager = backpackGridManager;
            }
            else
            {
                Debug.LogWarning($"Generator_Items: Префаб {itemData.name} не содержит InventoryItemDraggable");
            }

            return newItem;
        }

        Debug.Log($"Generator_Items: Нет свободного места для '{itemData.itemId}' без пересечений");
        return null;
    }

    // ↓ Методы для кнопок ↓

    public void Spawn_Sword() => TrySpawnItem(sword);
    public void Spawn_Knife() => TrySpawnItem(knife);
    public void Spawn_Screwdriver() => TrySpawnItem(screwdriver);
    public void Spawn_Ball() => TrySpawnItem(ball);
}
