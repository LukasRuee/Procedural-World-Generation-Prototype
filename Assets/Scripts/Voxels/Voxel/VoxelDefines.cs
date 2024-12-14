using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VoxelDefines : MonoBehaviour
{
    public static VoxelDefines Instance;

    public List<VoxelObject> Voxels { get; private set; }
    [SerializeField] private string dataPath = "Voxels";
    [field: SerializeField] public float VoxelSize { get; private set; } = 1;
    [field: SerializeField] public int ChunkSize { get; private set; } = 32;
    public int TotalVoxelsPerChunk { get; private set; } = 32 * 32 * 32;
    private void OnValidate()
    {
        TotalVoxelsPerChunk = ChunkSize * ChunkSize * ChunkSize;
    }

    private void Awake()
    {
        LoadVoxels();
        if (Instance == null)
        {
            Instance = this;
        }
    }
    /// <summary>
    /// Load all voxels from resources
    /// </summary>
    private void LoadVoxels()
    {
        Voxels = new List<VoxelObject>(Resources.LoadAll<VoxelObject>(dataPath));
        if (Voxels.Count == 0)
        {
            Debug.LogWarning("No voxels found in Resources/." + dataPath);
        }
        TotalVoxelsPerChunk = ChunkSize * ChunkSize * ChunkSize;
    }
    /// <summary>
    /// Gets a voxel after its id
    /// </summary>
    /// <param name="ID"></param>
    /// <returns></returns>
    public Voxel GetVoxel(int ID)
    {
        Voxel voxel = new Voxel();
        try
        {
            foreach (VoxelObject voxelObject in Voxels)
            {
                if (voxelObject.ID == ID)
                {
                    voxel = voxelObject.Clone();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Could not find voxel with ID: " + ID + "Error: " + e);
        }
        return voxel;
    }
    /// <summary>
    /// Gets a voxel after its id
    /// </summary>
    /// <param name="ID"></param>
    /// <returns></returns>
    public VoxelObject GetVoxelObject(int ID)
    {
        try
        {
            foreach (VoxelObject voxelObject in Voxels)
            {
                if (voxelObject.ID == ID)
                {
                    return voxelObject;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Could not find voxel with ID: " + ID + "Error: " + e);
        }
        return Voxels[0];
    }
}