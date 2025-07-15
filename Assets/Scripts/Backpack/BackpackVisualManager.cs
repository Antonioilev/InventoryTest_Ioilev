using UnityEngine;                 // ��� MonoBehaviour, Vector2Int, GameObject � �.�.
using UnityEngine.UI;              // ��� UI-���������, ���� �����
using System.Collections.Generic; // ��� List, HashSet � ������ ���������
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
