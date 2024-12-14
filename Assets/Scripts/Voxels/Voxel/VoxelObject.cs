using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public enum PhysicsType : uint
{
    None = 0,
    Solid = 1,
    Fluid = 2,
    Gas = 4,
    Movable = 8
}
public static class Utility
{
    // Initialize with a seed (you can use a constant or a dynamic value like a timestamp)
    private static Unity.Mathematics.Random rng = new Unity.Mathematics.Random((uint)Environment.TickCount);

    public static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int k = rng.NextInt(i + 1); // Generate random index
            // Swap elements
            T value = list[k];
            list[k] = list[i];
            list[i] = value;
        }
    }
    public static readonly List<int3> spreadDirections = new List<int3>
    {
        new int3(-1, 0, -1),    new int3(-1, 0, 0),     new int3(-1, 0, 1),
        new int3(0, 0, -1),                             new int3(0, 0, 1),
        new int3(1, 0, -1),     new int3(1, 0, 0),      new int3(1, 0, 1),
    };
}
public struct Voxel
{
    public PhysicsType PhysicsType;
    public bool IsTransparent;
    public float Mass;
    public float3 ForceDirection;
    public int ID;
    public int Value;
    public bool Updated;
    public Voxel(PhysicsType physicsType, bool isTransparent, float mass, int id, int value)
    {
        PhysicsType = physicsType;
        IsTransparent = isTransparent;
        Mass = mass;
        ForceDirection = new float3();
        ID = id;
        Value = value;
        Updated = false;
    }
    /// <summary>
    /// Updates the voxels position
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="currentChunkKey"></param>
    /// <param name="targetIndex"></param>
    /// <param name="targetChunk"></param>
    /// <returns></returns>
    public bool Step(int3 currentPos, int3 currentChunkKey, out int targetIndex, out Chunk targetChunk)
    {
        targetIndex = 0;
        targetChunk = null;
        if (Updated) { return false; }
        Updated = true;
        int3 targetPos = currentPos;
        targetIndex = 0;
        int3 correctedTargetPos;
        switch (PhysicsType)
        {
            case PhysicsType.Movable:
                targetPos.y -= 1;

                if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                {
                    targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                    if (targetChunk.VoxelArray[targetIndex].Mass < Mass)
                    {
                        return true;
                    }
                }

                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y - 1, currentPos.z + dz);

                        if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                        {
                            targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                            if (targetChunk.VoxelArray[targetIndex].Mass < Mass)
                            {
                                return true;
                            }
                        }
                    }
                }

                break;
            case PhysicsType.Fluid:
                targetPos.y -= 1;

                if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                {
                    targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                    if (targetChunk.VoxelArray[targetIndex].Mass < Mass)
                    {
                        return true;
                    }
                    else if(targetChunk.VoxelArray[targetIndex].ID == ID)
                    {
                        List<int3> randomizedSpreadDirections = new List<int3>(Utility.spreadDirections);
                        Utility.Shuffle(randomizedSpreadDirections);

                        foreach (var direction in randomizedSpreadDirections)
                        {
                            targetPos = currentPos + direction;

                            if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                            {
                                targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                                if (targetChunk.VoxelArray[targetIndex].Mass < Mass)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                // Check around
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y - 1, currentPos.z + dz);

                        if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                        {
                            targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                            if (targetChunk.VoxelArray[targetIndex].Mass < Mass)
                            {
                                return true;
                            }
                        }
                    }
                }
                break;
            case PhysicsType.Gas:
                targetPos.y += 1;

                if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                {
                    targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                    if (targetChunk.VoxelArray[targetIndex].Mass > Mass && targetChunk.VoxelArray[targetIndex].PhysicsType != PhysicsType.Solid)
                    {
                        return true;
                    }
                }

                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y + 1, currentPos.z + dz);

                        if (WorldManager.Instance.ChunkExists(currentChunkKey, targetPos, out targetChunk, out correctedTargetPos))
                        {
                            targetIndex = VoxelMethods.GetIndexFromPosition(correctedTargetPos);

                            if (targetChunk.VoxelArray[targetIndex].Mass > Mass && targetChunk.VoxelArray[targetIndex].PhysicsType != PhysicsType.Solid)
                            {
                                return true;
                            }
                        }
                    }
                }
                break;
            default:
                if (WorldManager.Instance.GetChunk(currentChunkKey, out targetChunk) == false) return false;
                break;
        }
        return false;
    }
    /// <summary>
    /// Sets the direction of the vector force
    /// </summary>
    /// <param name="dir"></param>
    public void SetForceDirection(float3 dir)
    {
        ForceDirection = dir;
    }
}

[CreateAssetMenu(fileName = "New Voxel", menuName = "ScriptableObjects/Voxel", order = 2)]
public class VoxelObject : ScriptableObject
{
    //[InstanceID] public int ID;
    [field: SerializeField] public int ID;
    [field: SerializeField] public Material Material { get; private set; }
    [field: SerializeField] public PhysicsType PhysicsType { get; private set; }
    [field: SerializeField] public bool IsTransparent { get; private set; }
    [field: SerializeField] public float Mass { get; private set; }
    [field  : SerializeField] public int Value { get; private set; }
    /// <summary>
    /// Clones the data to a Voxel
    /// </summary>
    /// <returns></returns>
    public Voxel Clone()
    {
        return new Voxel(PhysicsType, IsTransparent, Mass, ID, Value);
    }
}

    public class InstanceIDAttribute : PropertyAttribute { }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InstanceIDAttribute))]
    public class ScriptableObjectIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;

            if (property.intValue == 0)
            {
                property.intValue = property.objectReferenceInstanceIDValue;
            }

            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
    #endif
