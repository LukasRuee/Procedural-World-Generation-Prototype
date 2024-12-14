using System;
using System.Collections.Generic;
using UnityEngine;

public class WandDataContainer : MonoBehaviour
{
    [SerializeField] private GameObject spellSlotPrefab;
    [HideInInspector] public int Index;
    private Slot[] spellSlots;
    public SpellData[] Spells {  get; private set; }
    /// <summary>
    /// Create spell slots
    /// </summary>
    /// <param name="item"></param>
    /// <param name="wand"></param>
    public void CreateSpellSlots(Transform item, Wand wand)
    {
        spellSlots = new Slot[wand.wandData.MaxSpellSlots];
        Spells = new SpellData[wand.wandData.MaxSpellSlots];

        for(int i  = 0; i < wand.wandData.MaxSpellSlots; i++)
        {
            Spells[i] = wand.Spells[i];
            Slot spellSlot = Instantiate(spellSlotPrefab, item).GetComponent<Slot>();
            spellSlot.SetIndex(i);
            spellSlots[i] = spellSlot.GetComponent<Slot>();

            if (Spells[i] != null)
            {
                SpellData spell = Spells[i];
                spellSlot.SetItem(spell, ItemType.WandSpellSlot, this);
            }
            else
            {
                spellSlot.ClearItem();
            }
        }
    }
    /// <summary>
    /// Sets the raycast target of its children and background (For drag and drop)
    /// </summary>
    /// <param name="state"></param>
    public void SetRaycastTarget(bool state)
    {
        foreach(Slot slot in spellSlots)
        {
            slot.SetRaycastTarget(state);
        }
    }
    /// <summary>
    /// Moves a spell from an wand into the inventory
    /// </summary>
    /// <param name="draggableItem"></param>
    /// <param name="index"></param>
    public void WandSpellToInventory(DraggableItem draggableItem, int index)
    {
        SpellData spell = Spells[draggableItem.Index];
        if (InventoryManager.Instance.AddItem(spell, index))
        {
            InventoryManager.Instance.Wands[draggableItem.Container.Index].RemoveSpell(draggableItem.Index);
        }
    }
    /// <summary>
    /// Moves a spell from the inventory to an wand
    /// </summary>
    /// <param name="inventorySpellIndex"></param>
    /// <param name="targetWandDataSlotIndex"></param>
    public void InventorySpellToWand(int inventorySpellIndex, int targetWandDataSlotIndex)
    {
        SpellData spell = InventoryManager.Instance.Spells[inventorySpellIndex];
        if (InventoryManager.Instance.RemoveItem(spell, inventorySpellIndex))
        {
            InventoryManager.Instance.Wands[Index].Spells[targetWandDataSlotIndex] = spell;
        }
    }
    /// <summary>
    /// Moves a Spell from an wand to another wand
    /// </summary>
    /// <param name="onDraggedWand"></param>
    /// <param name="targetSlotIndex"></param>
    /// <param name="originSlotIndex"></param>
    public void WandSpellToWandSpell(Wand onDraggedWand, int targetSlotIndex, int originSlotIndex)
    {
        if (Spells[targetSlotIndex] == null)
        {
            SpellData draggedSpell = onDraggedWand.Spells[originSlotIndex];
            Spells[targetSlotIndex] = draggedSpell;
            onDraggedWand.Spells[originSlotIndex] = null;
            InventoryManager.Instance.Wands[Index].AddSpell(draggedSpell, targetSlotIndex);
        }
    }
}
