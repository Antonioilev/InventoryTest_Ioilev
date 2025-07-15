using UnityEngine;                 // Для MonoBehaviour, Vector2Int, GameObject и т.п.
using UnityEngine.UI;              // Для UI-элементов, если нужны
using System.Collections.Generic; // Для List, HashSet и других коллекций
public class BackpackVisualManager : MonoBehaviour
{
    public Transform backgroundContainer;
    private GameObject currentBackground;

    public void ShowVisual(GameObject prefab)
    {
        if (currentBackground != null) Destroy(currentBackground);
        if (prefab != null)
        {
            currentBackground = Instantiate(prefab, backgroundContainer, false);
        }
    }
}
