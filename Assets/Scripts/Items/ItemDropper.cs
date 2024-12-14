using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    static public ItemDropper Instance;
    [SerializeField] private GameObject pickUpPrefab;
    [Range(0f, 1f)][SerializeField] private float spellDropRate = 0.7f; // 70% chance to drop a spell
    [Range(0f, 1f)][SerializeField] private float wandDropRate = 0.3f;  // 30% chance to drop a wand

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }
    /// <summary>
    /// Drops a random item
    /// </summary>
    /// <param name="dropPos"></param>
    public void DropRandomItem(Vector3 dropPos)
    {
        float dropChance = UnityEngine.Random.value;

        // Normalize the drop chances
        float totalDropRate = spellDropRate + wandDropRate;

        // Make sure probabilities sum up to 1
        float normalizedWandChance = wandDropRate / totalDropRate;
        float normalizedSpellChance = spellDropRate / totalDropRate;

        if (dropChance <= normalizedSpellChance)
        {
            SpellData randomSpell = GetRandomSpell();
            if (randomSpell != null)
            {
                PickUp pickUp = Instantiate(pickUpPrefab, dropPos, Quaternion.identity).GetComponent<PickUp>();
                pickUp.SetData(randomSpell);
            }
        }
        else if(dropChance <= normalizedWandChance)
        {
            WandData randomWand = GetRandomWand();
            if (randomWand != null)
            {
                PickUp pickUp = Instantiate(pickUpPrefab, dropPos, Quaternion.identity).GetComponent<PickUp>();
                pickUp.SetData(randomWand);
            }
        }
    }
    /// <summary>
    /// Selects a random wand
    /// </summary>
    /// <returns></returns>
    private WandData GetRandomWand()
    {
        List<ItemDataBase> potentialWands = new List<ItemDataBase>();

        foreach (WandData wand in InventoryManager.Instance.LoadedWands)
        {
            // Adjust probability of adding the wand to the pool based on rarity
            switch (wand.Tier)
            {
                case RaretyTier.Common:
                    AddItemWithChance(potentialWands, wand, 50); // 50% chance
                    break;
                case RaretyTier.Uncommon:
                    AddItemWithChance(potentialWands, wand, 30); // 30% chance
                    break;
                case RaretyTier.Epic:
                    AddItemWithChance(potentialWands, wand, 15); // 15% chance
                    break;
                case RaretyTier.Legendary:
                    AddItemWithChance(potentialWands, wand, 5); // 5% chance
                    break;
            }
        }

        // Select a random wand from the potential list
        if (potentialWands.Count > 0)
        {
            return (WandData)potentialWands[UnityEngine.Random.Range(0, potentialWands.Count)];
        }

        return null;  // If no wands are available, return null
    }
    private void AddItemWithChance(List<ItemDataBase> list, ItemDataBase wand, int chancePercent)
    {
        if (UnityEngine.Random.Range(0, 100) < chancePercent)
        {
            list.Add(wand);
        }
    }
    /// <summary>
    ///  Selects a random spell
    /// </summary>
    /// <returns></returns>
    private SpellData GetRandomSpell()
    {
        List<ItemDataBase> potentialSpells = new List<ItemDataBase>();

        foreach (SpellData spell in InventoryManager.Instance.LoadedSpells)
        {
            // Adjust probability of adding the wand to the pool based on rarity
            switch (spell.Tier)
            {
                case RaretyTier.Common:
                    AddItemWithChance(potentialSpells, spell, 50); // 50% chance
                    break;
                case RaretyTier.Uncommon:
                    AddItemWithChance(potentialSpells, spell, 30); // 30% chance
                    break;
                case RaretyTier.Epic:
                    AddItemWithChance(potentialSpells, spell, 15); // 15% chance
                    break;
                case RaretyTier.Legendary:
                    AddItemWithChance(potentialSpells, spell, 5); // 5% chance
                    break;
            }
        }

        // Select a random spell from the potential list
        if (potentialSpells.Count > 0)
        {
            return (SpellData)potentialSpells[UnityEngine.Random.Range(0, potentialSpells.Count)];
        }

        return null;  // If no spells are available, return null
    }
}
