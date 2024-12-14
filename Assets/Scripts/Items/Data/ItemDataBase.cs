using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemDataBase : ScriptableObject
{
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public string ItemDescription { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField] public RaretyTier Tier { get; protected set; }
}
public enum RaretyTier
{
    Common,
    Uncommon,
    Epic,
    Legendary
}
public abstract class ItemBase : MonoBehaviour
{
    [field: SerializeField] public ItemDataBase Data {  get; private set; }
}
