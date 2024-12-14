using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Base class for Inventory Slot
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public int Index { get; private set; }
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text displayName;
    [SerializeField] public ItemType ItemType {  get; private set; }
    public WandDataContainer Container { get; private set; }
    [field: SerializeField] public Transform wandItemSpellSlots {  get; private set; }
    public Transform ParentAfterDrag { get; private set; }
    public void SetIndex(int index)
    {
        Index = index;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if(ItemType == ItemType.Wand)
        {
            InventoryUI.Instance.StartDragWand(Index);
        }
        ParentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        SetRaycastTarget(false);

        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0.6f); 
        transform.localScale = Vector3.one * 1.2f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(ParentAfterDrag);
        SetRaycastTarget(true);

        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1f); 
        transform.localScale = Vector3.one; 
    }
    /// <summary>
    /// Sets the data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <param name="type"></param>
    /// <param name="container"></param>
    public void SetData(ItemDataBase data, int index, ItemType type, WandDataContainer container)
    {
        this.Container = container;
        ItemType = type;
        this.Index = index;
        if (data != null)
        {
            icon.sprite = data.Icon;
            displayName.text = data.name;
        }
        else
        {
            icon.sprite = null;
            displayName.text = "Empty";
        }
    }
    /// <summary>
    /// Sets the raycast target of its icon (For drag and drop)
    /// </summary>
    /// <param name="state"></param>
    public void SetRaycastTarget(bool state)
    {
        icon.raycastTarget = state;
    }
}