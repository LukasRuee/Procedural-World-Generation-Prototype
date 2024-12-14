using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWand", menuName = "Inventory/Wand")]
public class WandData : ItemDataBase
{
    [field: SerializeField] public float MaxMana { get; private set; } = 100;
    [field: SerializeField] public float ManaRechargeRate { get; private set; } = 10;
    [field: SerializeField] public int MaxSpellSlots { get; private set; } = 3;
    [field: SerializeField] public float CastDelay { get; private set; } = 0.1f;
}

