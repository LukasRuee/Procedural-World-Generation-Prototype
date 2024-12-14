using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct VoxelInteraction : ISerializationCallbackReceiver
{
    [field: SerializeField] public VoxelObject TargetVoxelToReplace { get; private set; }
    [field: SerializeField] public VoxelObject ReplacementVoxel { get; private set; }
    [field: SerializeField] public GameObject ParticleSystemPrefab { get; private set; }
    [field: SerializeField] public bool useReplacementVoxelMaterial { get; private set; }
    public VoxelParticleSystem VoxelParticleSystem { get; private set; }
    void OnValidate()
    {
        if(ParticleSystemPrefab != null)
        {
            if (ParticleSystemPrefab.TryGetComponent(out VoxelParticleSystem system))
            {
                VoxelParticleSystem = system;
            }
        }
    }
    void ISerializationCallbackReceiver.OnBeforeSerialize() => this.OnValidate();
    void ISerializationCallbackReceiver.OnAfterDeserialize() { }
}
[CreateAssetMenu(fileName = "NewSpell", menuName = "Inventory/Spell")]
public class SpellData : ItemDataBase
{
    [field: SerializeField] public float ManaCost { get; private set; }
    [field: SerializeField] public float Damage { get; private set; }
    [field: SerializeField] public float LiveTime { get; private set; }
    [field: SerializeField] public float EffectRadius { get; private set; }
    [field: SerializeField] public List<VoxelInteraction> VoxelInteraction { get; private set; } = new List<VoxelInteraction>();
}

