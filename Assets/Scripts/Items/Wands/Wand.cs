using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wand : ItemBase
{
    public WandData wandData { get; private set; }
    public float CurrentMana {  get; private set; }
    [SerializeField] private Transform spellOutput;
    private int spellIndex = 0;
    private bool isCastingASpell;
    public SpellData[] Spells { get; private set; }

    private void Awake()
    {
        wandData = (WandData)base.Data;
        Spells = new SpellData[wandData.MaxSpellSlots];
        CurrentMana = wandData.MaxMana;
    }
    private void Update()
    {
        CurrentMana = Mathf.Min(wandData.MaxMana, CurrentMana + (wandData.ManaRechargeRate * Time.deltaTime));
    }
    /// <summary>
    /// Adds a spell
    /// </summary>
    /// <param name="spell"></param>
    /// <param name="index"></param>
    public void AddSpell(SpellData spell, int index)
    {
        if (index < wandData.MaxSpellSlots)
        {
            Spells[index] = spell;
        }
    }
    /// <summary>
    /// Removes a spell
    /// </summary>
    /// <param name="index"></param>
    public void RemoveSpell(int index)
    {
        if (index < wandData.MaxSpellSlots)
        {
            Spells[index] = null;
        }
    }
    /// <summary>
    /// Casts a spell
    /// </summary>
    public void CastSpell()
    {
        if (!isCastingASpell)
        {
            StartCoroutine(CastSpellRoutine());
        }
    }
    /// <summary>
    /// Casts a spell
    /// </summary>
    /// <returns></returns>
    private IEnumerator CastSpellRoutine()
    {
        isCastingASpell = true;

        if (Spells.Length > 0)
        {
            int validSpellsCount = 0;

            foreach (var spell in Spells)
            {
                if (spell != null)
                {
                    validSpellsCount++;
                }
            }
            if (validSpellsCount > 0)
            {
                while (Spells[spellIndex] == null)
                {
                    spellIndex++;
                    if (spellIndex >= Spells.Length)
                    {
                        spellIndex = 0;
                    }
                }

                SpellData spellToCast = Spells[spellIndex];

                if (spellToCast != null)
                {
                    if (CurrentMana >= spellToCast.ManaCost)
                    {
                        CurrentMana -= spellToCast.ManaCost;
                        SpellBase spell = Instantiate(spellToCast.Prefab, spellOutput.position, spellOutput.rotation).GetComponent<SpellBase>();
                        spell.Cast();
                        yield return new WaitForSeconds(wandData.CastDelay);
                    }
                }

                spellIndex++;
                if (spellIndex >= Spells.Length)
                {
                    spellIndex = 0;
                }
            }
        }

        isCastingASpell = false;
    }

}
