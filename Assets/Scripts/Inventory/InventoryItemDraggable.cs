using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 originalPosition;
    private Transform originalParent;

    [Header("Drag Targets")]
    public BackpackGridManager backpackGridManager;
    public GroundGridManager groundGridManager;

    public InventoryItemData itemData;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogWarning("InventoryItemDraggable: Canvas not found in parent hierarchy");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        // ���������������� �� ����� Canvas ��� ���������� �����������
        if (canvas != null)
            SetParentAndKeepWorldPosition(transform, canvas.transform);
        else
            Debug.LogWarning("InventoryItemDraggable: Canvas is null in OnBeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool placed = false;

        if (backpackGridManager == null)
        {
            Debug.LogWarning("InventoryItemDraggable: backpackGridManager is not assigned!");
        }
        else if (backpackGridManager.SlotContainer == null)
        {
            Debug.LogWarning("InventoryItemDraggable: backpackGridManager SlotContainer is not assigned!");
        }
        else if (itemData == null)
        {
            Debug.LogWarning("InventoryItemDraggable: itemData is not assigned!");
        }
        else
        {
            // ���������, ���� ������� ������� � � ������ ��� �� �����
            if (backpackGridManager.IsPointerOverSlotContainer(Input.mousePosition))
            {
                placed = backpackGridManager.TryPlaceItemAtMousePosition(itemData, rectTransform);
                if (placed)
                {
                    // ������� ������� � ������ � �� ������������ � TryPlaceItem,
                    // ��� ��� ��� ������ ������ �� ����.
                    return;
                }
            }

            // ���� ���� �������� ����� � ��������� ��� ��� � ����� �� �����
            if (!placed && groundGridManager != null && groundGridManager.IsPointerOverGround(Input.mousePosition))
            {
                placed = groundGridManager.TryPlaceItemAtMousePosition(itemData, rectTransform);
                if (placed)
                {
                    // ��� ���������� �� ����� ������ �������� �� ��������� �����
                    SetParentAndKeepWorldPosition(transform, groundGridManager.ItemsParent);
                }
            }
        }

        if (!placed)
        {
            // ������� � �������� ��������� � ��������
            SetParentAndKeepWorldPosition(transform, originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
        else
        {
            if (placed && backpackGridManager != null && backpackGridManager.IsPointerOverSlotContainer(Input.mousePosition))
            {
                // � ������ ���������� � ������� ������ ������������, ����� �� �������������.
                Destroy(gameObject);
            }
        }
    }

    // ����� ����� �������� ��� �������� ������� � ���� (UI)
    private void SetParentAndKeepWorldPosition(Transform child, Transform newParent)
    {
        Vector3 worldPos = child.position;
        child.SetParent(newParent, false);
        child.position = worldPos;
    }
}
