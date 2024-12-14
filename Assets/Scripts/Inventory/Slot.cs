using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ItemType 
{ 
    Wand,
    Spell, 
    WandSpellSlot 
};
public class Slot : MonoBehaviour, IDropHandler
{
    private DraggableItem currentItem;
    [SerializeField] private GameObject ItemPrefab;
    [SerializeField] private ItemType SlotType;
    [HideInInspector] public int Index {  get; private set; }
    [SerializeField] private Image background;
    public void SetIndex(int index)
    {
        Index = index;
    }
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();

        if (draggableItem == null) return;

        if (((SlotType == ItemType.WandSpellSlot || SlotType == ItemType.Spell) && (draggableItem.ItemType == ItemType.Spell || draggableItem.ItemType == ItemType.WandSpellSlot)) ||
            (SlotType == ItemType.Wand && draggableItem.ItemType == ItemType.Wand))
        {
            ProceedOnDrop(draggableItem);
        }
    }
    /// <summary>
    /// Manage the on dropped item in inventory
    /// </summary>
    /// <param name="draggableItem"></param>
    private void ProceedOnDrop(DraggableItem draggableItem)
    {

        switch (SlotType)
        {
            case ItemType.WandSpellSlot:
                if (draggableItem.ItemType == ItemType.WandSpellSlot)
                {
                    WandDataContainer wandDataContainer = GetComponentInParent<WandDataContainer>();
                    wandDataContainer.WandSpellToWandSpell(InventoryManager.Instance.Wands[draggableItem.Container.Index], Index, draggableItem.Index);
                }
                else if (draggableItem.ItemType == ItemType.Spell)
                {
                    GetComponentInParent<WandDataContainer>().InventorySpellToWand(draggableItem.Index, Index);
                }
                break;

            case ItemType.Wand:
                if (InventoryManager.Instance.Wands[draggableItem.Index] != null)
                {
                    InventoryManager.Instance.SwapWands(draggableItem.Index, Index);
                    draggableItem.SetIndex(Index);
                }
                break;

            case ItemType.Spell:
                if (draggableItem.ItemType == ItemType.WandSpellSlot)
                {
                    draggableItem.Container.WandSpellToInventory(draggableItem, Index);
                }
                else if (draggableItem.ItemType == ItemType.Spell)
                {
                    if (InventoryManager.Instance.Spells[draggableItem.Index] != null)
                    {
                        InventoryManager.Instance.SwapSpellSlot(draggableItem.Index, Index);
                        draggableItem.SetIndex(Index);
                    }
                }
                break;
        }
        InventoryUI.Instance.UpdateUI();
    }
    /// <summary>
    /// Sets an item to the slot
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    public Transform SetItem(ItemDataBase data, ItemType type, WandDataContainer container)
    {
        ClearItem();  // Ensure old item is removed first

        currentItem = Instantiate(ItemPrefab, transform).GetComponent<DraggableItem>();
        currentItem.SetData(data, Index, type, container);  // Pass the index to keep track of the inventory index

        if (data != null)
        {
            // Handle different item types (optional customization based on type)
            if (data is WandData)
            {
                return currentItem.wandItemSpellSlots;
            }
            else if (data is SpellData)
            {
            }
        }
        return currentItem.transform;
    }
    /// <summary>
    /// Clears an item off the slot
    /// </summary>
    public void ClearItem()
    {
        if (currentItem != null)
        {
            Destroy(currentItem.gameObject);
            currentItem = null;
        }
    }
    /// <summary>
    /// Sets the raycast target of its children and background (For drag and drop)
    /// </summary>
    /// <param name="state"></param>
    public void SetRaycastTarget(bool state)
    {
        background.raycastTarget = !background.raycastTarget;
        if(currentItem != null)
        {
            currentItem.SetRaycastTarget(state);
        }
    }
}
