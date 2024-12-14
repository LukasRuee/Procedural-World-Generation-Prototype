using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public abstract class SpellBase : ItemBase
{
    public SpellData SpellData { get; private set; }
    private void Awake()
    {
        SpellData = (SpellData)base.Data;
    }
    public void SetUp(SpellData data)
    {
        SpellData = data;
    }

    public abstract void Cast();
}