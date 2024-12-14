using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public List<SpellData> LoadedSpells { get; private set; }
    public List<WandData> LoadedWands { get; private set; }
    public SpellData[] Spells { get; private set; }
    public Wand[] Wands { get; private set; }
    public Wand SelectedWand { get; private set; }
    [SerializeField] public int MaxSpareSpells { get; private set; } = 10;
    [SerializeField] public int MaxWands { get; private set; } = 4;
    [SerializeField] private Transform wandPos;
    [SerializeField] private bool loadAllItemsInInventory;

    [Header("Resources")]
    [SerializeField] private string wandResourcesPath = "Wands";
    [SerializeField] private string spellResourcesPath = "Spells";

    static public InventoryManager Instance { get; private set; }
    
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        Wands = new Wand[MaxWands];
        Spells = new SpellData[MaxSpareSpells];

        LoadedWands = new List<WandData>(Resources.LoadAll<WandData>(wandResourcesPath));
        LoadedSpells = new List<SpellData>(Resources.LoadAll<SpellData>(spellResourcesPath));
    }
    private void Start()
    {
        if(loadAllItemsInInventory)
        {
            int counter = 0;
            foreach (WandData wand in LoadedWands)
            {
                AddItem(wand);
                counter++;
                if (counter > MaxWands) break;
            }
            counter = 0;
            foreach (SpellData spell in LoadedSpells)
            {
                AddItem(spell);
                counter++;
                if (counter > MaxSpareSpells) break;
            }
        }
        SetSelectedWand(Wands[0]);
    }
    #region ItemManagment dynamic
    /// <summary>
    /// Adds a Item
    /// </summary>
    /// <param name="data"></param>
    /// <returns>Returns true if successfull</returns>
    public bool AddItem(ItemDataBase data)
    {
        if (data is WandData)
        {
            return AddWand((WandData)data);
        }
        else if (data is SpellData)
        {
            return AddSpell((SpellData)data);
        }
        return false;
    }
    /// <summary>
    /// Adds a wand
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool AddWand(WandData data)
    {
        for (int i = 0; i < MaxWands; i++)
        {
            if (Wands[i] == null)
            {
                Wands[i] = Instantiate(data.Prefab, transform).GetComponent<Wand>();//data.Prefab.GetComponent<Wand>();
                Wands[i].transform.SetParent(wandPos);
                Wands[i].transform.position = wandPos.position;
                Wands[i].gameObject.SetActive(false);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Adds a Spell
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool AddSpell(SpellData data)
    {
        for (int i = 0; i < MaxSpareSpells; i++)
        {
            if (Spells[i] == null)
            {
                Spells[i] = data;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Removes a Item
    /// </summary>
    /// <param name="data"></param>
    public void RemoveItem(ItemDataBase data)
    {
        if (data is WandData)
        {
            RemoveWand((WandData)data);
        }
        else if (data is SpellData)
        {
            RemoveSpell((SpellData)data);
        }
    }
    /// <summary>
    /// Removes a wand
    /// </summary>
    /// <param name="data"></param>
    private void RemoveWand(WandData data)
    {
        for (int i = 0; i < MaxWands; i++)
        {
            if (Wands[i] == data)
            {
                Destroy(Wands[i]);// = null;
                Wands[i] = null;
                return;
            }
        }
    }
    /// <summary>
    /// Removes a spell
    /// </summary>
    /// <param name="data"></param>
    private void RemoveSpell(SpellData data)
    {
        for (int i = 0; i < MaxSpareSpells; i++)
        {
            if (Spells[i] == data)
            {
                Spells[i] = null;
                return;
            }
        }
    }
    #endregion
    #region ItemManagment per index
    /// <summary>
    /// Adds a item
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <returns>Returns true if successfull</returns>
    public bool AddItem(ItemDataBase data, int index)
    {
        if (data is WandData)
        {
            return AddWand((WandData)data, index);
        }
        else if (data is SpellData)
        {
            return AddSpareSpell((SpellData)data, index);
        }
        return false;
    }
    /// <summary>
    /// Adds a wand
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool AddWand(WandData data, int index)
    {
        if (index < MaxWands)
        {
            if (Wands[index] == null)
            {
                Wands[index] = Instantiate(data.Prefab, transform).GetComponent<Wand>();//data.Prefab.GetComponent<Wand>();
                Wands[index].transform.SetParent(wandPos);
                Wands[index].transform.position = wandPos.position;
                Wands[index].gameObject.SetActive(false);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Adds a spell
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool AddSpareSpell(SpellData data, int index)
    {
        if(index < MaxSpareSpells)
        {
            if (Spells[index] == null)
            {
                Spells[index] = data;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Removes a item
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool RemoveItem(ItemDataBase data, int index)
    {
        if (data is WandData)
        {
            return RemoveWand((WandData)data, index);
        }
        else if (data is SpellData)
        {
            return RemoveSpell((SpellData)data, index);
        }
        return false;
    }
    /// <summary>
    /// Removes a wand
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool RemoveWand(WandData data, int index)
    {
        if (Wands[index] == data)
        {
            Destroy(Wands[index]);// = null;
            Wands[index] = null;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Removes a spell
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool RemoveSpell(SpellData data, int index)
    {
        if (Spells[index] == data)
        {
            Spells[index] = null;
            return true;
        }
        return false;
    }
    #endregion
    /// <summary>
    /// Uses the selecteds wand CastSpell method
    /// </summary>
    public void UseItem()
    {
        if (SelectedWand != null)
        {
            SelectedWand.CastSpell();
        }
    }
    /// <summary>
    /// Swaps a wands position
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    public void SwapWands(int index1, int index2)
    {
        Wand temp = Wands[index1];
        Wands[index1] = Wands[index2];
        Wands[index2] = temp;
        if (index1 == 0 || index2 == 0)
        {
            SetSelectedWand(Wands[0]);
        }
    }
    /// <summary>
    /// Swaps a spell slots position
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    public void SwapSpellSlot(int index1, int index2)
    {
        SpellData temp = Spells[index1];
        Spells[index1] = Spells[index2];
        Spells[index2] = temp;
    }
    /// <summary>
    /// Sets the selected wand
    /// </summary>
    /// <param name="wand"></param>
    private void SetSelectedWand(Wand wand)
    {
        if (SelectedWand != null)
        {
            SelectedWand.gameObject.SetActive(false);
        }

        SelectedWand = wand;

        if (SelectedWand != null)
        {
            SelectedWand.gameObject.SetActive(true);
        }
    }

}
