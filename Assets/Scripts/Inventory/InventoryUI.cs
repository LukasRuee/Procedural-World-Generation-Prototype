using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform wandSlotsParent;
    [SerializeField] private Transform spellSlotsParent;
    [SerializeField] private GameObject wandSlotPrefab;
    [SerializeField] private GameObject spellSlotPrefab;

    private WandDataContainer[] containers;
    static public InventoryUI Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        containers = new WandDataContainer[InventoryManager.Instance.MaxWands];
    }
    /// <summary>
    /// Updates the UI
    /// </summary>
    public void UpdateUI()
    {
        ClearUI();

        //Wands
        for (int i = 0; i < InventoryManager.Instance.MaxWands; i++)
        {
            Slot wandSlot = Instantiate(wandSlotPrefab, wandSlotsParent).GetComponent<Slot>();
            wandSlot.SetIndex(i);

            if (InventoryManager.Instance.Wands[i] != null)
            {
                Wand wand = InventoryManager.Instance.Wands[i];
                Transform draggableItem = wandSlot.SetItem(wand.wandData, ItemType.Wand, null);

                if(draggableItem != null)
                {
                    WandDataContainer wandDataContainer = wandSlot.GetComponent<WandDataContainer>();
                    wandDataContainer.Index = i;
                    wandDataContainer.CreateSpellSlots(draggableItem, wand);

                    containers[i] = wandDataContainer;

                    for (int j = 0; j < wand.Spells.Length; j++)
                    {
                        if (wand.Spells[j] != null)
                        {
                            InventoryManager.Instance.Wands[wandDataContainer.Index].AddSpell(wand.Spells[j], j);
                        }
                    }
                }

            }
            else
            {
                wandSlot.ClearItem();
            }
        }

        //Slots
        for (int i = 0; i < InventoryManager.Instance.MaxSpareSpells; i++)
        {
            Slot spellSlot = Instantiate(spellSlotPrefab, spellSlotsParent).GetComponent<Slot>();
            spellSlot.SetIndex(i);

            if (InventoryManager.Instance.Spells[i] != null)
            {
                SpellData spell = InventoryManager.Instance.Spells[i];
                spellSlot.SetItem(spell, ItemType.Spell, null);
            }
            else
            {
                spellSlot.ClearItem();
            }
        }
    }
    /// <summary>
    /// Sets raycast targets for dragging a wand
    /// </summary>
    /// <param name="index"></param>
    public void StartDragWand(int index)
    {
        containers[index].SetRaycastTarget(false);
    }
    /// <summary>
    /// Resets raycast targets for dropping a wand
    /// </summary>
    /// <param name="index"></param>
    public void EndDragWand(int index)
    {
        containers[index].SetRaycastTarget(true);
    }
    /// <summary>
    /// Clears the UI
    /// </summary>
    private void ClearUI()
    {
        foreach (Transform child in wandSlotsParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in spellSlotsParent)
        {
            Destroy(child.gameObject);
        }
    }
}