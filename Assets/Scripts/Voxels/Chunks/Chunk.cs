using System;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Chunk
{
    public bool Initialized { get; private set; }
    public bool Loaded { get; private set; }
    public bool Awake { get; private set; }
    public bool IsUpdating { get; private set; }
    public bool IsGeneratingMesh { get; private set; }
    public Voxel[] VoxelArray { get; private set; }
    public int3 Key { get; private set; }
    private Mesh[] Meshes;
    public bool ShouldGenerateNewMesh;
    private int[] meshGenBuffer;
    /// <summary>
    /// Gets mesh data
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Mesh GetMesh(int index)
    {
        return Meshes[index];
    }
    /// <summary>
    /// Set up meshes
    /// </summary>
    public void SetUpMeshes()
    {
        Meshes = new Mesh[VoxelDefines.Instance.Voxels.Count];
        for (int i = 0; i < VoxelDefines.Instance.Voxels.Count; i++)
        {
            Meshes[i] = new Mesh();
            //Meshes[i].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            Meshes[i].indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        }
    }
    /// <summary>
    /// Initialize chunk
    /// </summary>
    /// <param name="newKey"></param>
    /// <param name="voxelArray"></param>
    public void Initialize(int3 newKey, Voxel[] voxelArray)
    {
        Key = newKey;
        Awake = true;
        meshGenBuffer = new int[VoxelDefines.Instance.TotalVoxelsPerChunk];
        VoxelArray = new Voxel[VoxelDefines.Instance.TotalVoxelsPerChunk];
        voxelArray.CopyTo(VoxelArray, 0);
        Initialized = true;
        ShouldGenerateNewMesh = true;
    }
    /// <summary>
    /// Load chunk
    /// </summary>
    public void Load()
    {
        Awake = true;
        Loaded = true;
    }
    /// <summary>
    /// Unload chunk
    /// </summary>
    public void Unload()
    {
        Loaded = false;
    }
    /// <summary>
    /// Generates meshes for each voxel
    /// </summary>
    /// <returns></returns>
    public bool GenerateMesh()
    {
        if (Loaded && ShouldGenerateNewMesh)
        {
            for (int i = 0; i < Meshes.Length; i++)
            {
                for (int j = 0; j < VoxelArray.Length; j++)
                {
                    if (VoxelArray[j].ID == VoxelDefines.Instance.Voxels[i].ID)
                    {
                        meshGenBuffer[j] = 1;
                    }
                    else
                    {
                        meshGenBuffer[j] = 0;
                    }
                }
                if (VoxelDefines.Instance.Voxels[i].Material != null)
                {
                    ComputeShaderBinaryMeshing.Instance.UpdateMesh(meshGenBuffer, Meshes[i]);
                }
            }
            ShouldGenerateNewMesh = false;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Force generate meshes (do not use in too short intervals)
    /// </summary>
    public void ForceGenerateMesh()
    {
        Awake = true;
        ShouldGenerateNewMesh = true;
    }
    ///////////////////////////////////////////////////////////////////////////////
    
    /// <summary>
    /// Updates the voxel array with each voxel
    /// </summary>
    public void UdpateVoxel()
    {
        if (Awake == false) return;
        Awake = false;
        
        for (int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            VoxelArray[i].Updated = false;
        }

        Chunk targetChunk;
        for (int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            if (VoxelArray[i].Step(VoxelMethods.GetPositionFromIndex(i), Key, out int targetIndex, out targetChunk))
            {
                SwitchPosition(i, targetIndex, targetChunk);
                Awake = true;
                targetChunk.ShouldGenerateNewMesh = true;
                targetChunk.Awake = true;
            }
        }
        if (Awake)
        {
            ShouldGenerateNewMesh = true;
        }
    }
    /// <summary>
    /// Updates the voxel array with each voxel as a Task
    /// </summary>
    public Task UdpateVoxTask()
    {
        if (Awake == false) return Task.CompletedTask;
        IsUpdating = true;
        Awake = false;

        for (int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            VoxelArray[i].Updated = false;
        }

        Chunk targetChunk;
        for (int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            if (VoxelArray[i].Step(VoxelMethods.GetPositionFromIndex(i), Key, out int targetIndex, out targetChunk))
            {
                SwitchPosition(i, targetIndex, targetChunk);
                Awake = true;
                targetChunk.ShouldGenerateNewMesh = true;
                targetChunk.Awake = true;
            }
        }
        if (Awake)
        {
            ShouldGenerateNewMesh = true;
        }
        IsUpdating = false;
        return Task.CompletedTask;
    }
    /// <summary>
    /// Switches the position of two voxels
    /// </summary>
    /// <param name="indexA"></param>
    /// <param name="indexB_newChunk"></param>
    /// <param name="newChunk"></param>
    private void SwitchPosition(int indexA, int indexB_newChunk, Chunk newChunk)
    {
        Voxel tempVoxelA = VoxelArray[indexA];
        Voxel tempVoxelB = newChunk.VoxelArray[indexB_newChunk];

        // Calculate the ForceDirection
        int3 newPosition = VoxelMethods.GetPositionFromIndex(indexB_newChunk);
        int3 oldPosition = VoxelMethods.GetPositionFromIndex(indexA);

        tempVoxelA.SetForceDirection(newPosition - oldPosition);
        tempVoxelB.SetForceDirection(-tempVoxelA.ForceDirection);

        VoxelArray[indexA] = tempVoxelB;
        newChunk.VoxelArray[indexB_newChunk] = tempVoxelA;
    }
    ///////////////////////////////// EXPERIMENTAL /////////////////////////////////
    public JobHandle handle;
    NativeArray<Voxel> voxelBuffer;
    /// <summary>
    /// Updates the voxel array using the job system
    /// </summary>
    public void UdpateVoxelJobs()
    {
        IsUpdating = true;
        voxelBuffer = new NativeArray<Voxel>(VoxelDefines.Instance.TotalVoxelsPerChunk, Allocator.TempJob);

        voxelBuffer.CopyFrom(VoxelArray);

        UpdateVoxelArrayFor job = new UpdateVoxelArrayFor()
        {
            Array = voxelBuffer,
            ShouldGenerateNewMesh = ShouldGenerateNewMesh,
            ChunkSize = VoxelDefines.Instance.ChunkSize
        };
        handle = job.Schedule(VoxelDefines.Instance.TotalVoxelsPerChunk, handle);

        handle.Complete();
        while (!handle.IsCompleted) ;
        voxelBuffer.CopyTo(VoxelArray);
        voxelBuffer.Dispose();
        IsUpdating = false;
        ShouldGenerateNewMesh = true;
    }
}
[BurstCompile]
public struct UpdateVoxelArray : IJob
{
    public NativeArray<Voxel> Array;
    [WriteOnly]
    public bool ShouldGenerateNewMesh;
    [ReadOnly]
    public int ChunkSize;
    public void Execute()
    {
        for (int currentIndex = 0; currentIndex < Array.Length; currentIndex++)
        {
            ProcessVoxel(currentIndex);
        }
    }
    private void ProcessVoxel(int currentIndex)
    {
        int3 currentPos = VoxelMethods.GetPositionFromIndex(currentIndex, ChunkSize);
        int3 targetPos = currentPos;
        int targetIndex;

        switch (Array[currentIndex].PhysicsType)
        {
            case PhysicsType.Movable:
                // Determine the new position based on velocity
                targetPos.y -= 1;
                targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                {
                    if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                    {
                        SwitchPositionJob(currentIndex, targetIndex);
                        ShouldGenerateNewMesh = true;
                        break;
                    }
                }

                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y - 1, currentPos.z + dz);
                        targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                        if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                        {
                            if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                            {
                                SwitchPositionJob(currentIndex, targetIndex);
                                ShouldGenerateNewMesh = true;
                                break;
                            }
                        }
                    }
                }

                break;
            case PhysicsType.Fluid:
                // Determine the new position based on velocity
                targetPos.y -= 1;
                targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                {
                    if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                    {
                        SwitchPositionJob(currentIndex, targetIndex);
                        ShouldGenerateNewMesh = true;
                        break;
                    }
                }

                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y - 1, currentPos.z + dz);
                        targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                        if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                        {
                            if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                            {
                                SwitchPositionJob(currentIndex, targetIndex);
                                ShouldGenerateNewMesh = true;
                                break;
                            }
                        }
                    }
                }
                break;
            case PhysicsType.Gas:
                // Determine the new position based on velocity
                targetPos.y += 1;
                targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                {
                    if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                    {
                        SwitchPositionJob(currentIndex, targetIndex);
                        ShouldGenerateNewMesh = true;
                        break;
                    }
                }
                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y + 1, currentPos.z + dz);
                        targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                        if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                        {
                            if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                            {
                                SwitchPositionJob(currentIndex, targetIndex);
                                ShouldGenerateNewMesh = true;
                                break;
                            }
                        }
                    }
                }
                break;
        }
    }
    private void SwitchPositionJob(int indexA, int indexB_newChunk)
    {
        Voxel tempVoxelA = Array[indexA];
        Voxel tempVoxelB = Array[indexB_newChunk];

        // Calculate the ForceDirection
        int3 newPosition = VoxelMethods.GetPositionFromIndex(indexB_newChunk, ChunkSize);
        int3 oldPosition = VoxelMethods.GetPositionFromIndex(indexA, ChunkSize);

        tempVoxelA.SetForceDirection(newPosition - oldPosition);
        tempVoxelB.SetForceDirection(-tempVoxelA.ForceDirection);

        Array[indexA] = tempVoxelB;
        Array[indexB_newChunk] = tempVoxelA;
    }
}
[BurstCompile]
public struct UpdateVoxelArrayFor : IJobFor
{
    public NativeArray<Voxel> Array;
    [WriteOnly]
    public bool ShouldGenerateNewMesh;
    [ReadOnly]
    public int ChunkSize;
    public void Execute(int index)
    {
        ProcessVoxel(index);
    }
    private void ProcessVoxel(int currentIndex)
    {
        int3 currentPos = VoxelMethods.GetPositionFromIndex(currentIndex, ChunkSize);
        int3 targetPos = currentPos;
        int targetIndex;

        switch (Array[currentIndex].PhysicsType)
        {
            case PhysicsType.Movable:
                // Determine the new position based on velocity
                targetPos.y -= 1;
                targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                {
                    if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                    {
                        SwitchPositionJob(currentIndex, targetIndex);
                        ShouldGenerateNewMesh = true;
                        break;
                    }
                }

                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y - 1, currentPos.z + dz);
                        targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                        if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                        {
                            if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                            {
                                SwitchPositionJob(currentIndex, targetIndex);
                                ShouldGenerateNewMesh = true;
                                break;
                            }
                        }
                    }
                }

                break;
            case PhysicsType.Fluid:
                // Determine the new position based on velocity
                targetPos.y -= 1;
                targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                {
                    if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                    {
                        SwitchPositionJob(currentIndex, targetIndex);
                        ShouldGenerateNewMesh = true;
                        break;
                    }
                }

                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y - 1, currentPos.z + dz);
                        targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                        if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                        {
                            if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                            {
                                SwitchPositionJob(currentIndex, targetIndex);
                                ShouldGenerateNewMesh = true;
                                break;
                            }
                        }
                    }
                }
                break;
            case PhysicsType.Gas:
                // Determine the new position based on velocity
                targetPos.y += 1;
                targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                {
                    if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                    {
                        SwitchPositionJob(currentIndex, targetIndex);
                        ShouldGenerateNewMesh = true;
                        break;
                    }
                }
                // Check around
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        targetPos = new int3(currentPos.x + dx, currentPos.y + 1, currentPos.z + dz);
                        targetIndex = VoxelMethods.GetIndexFromPosition(targetPos, ChunkSize);

                        if (VoxelMethods.IsIndexInsideCurrentChunk(targetIndex, ChunkSize))
                        {
                            if (Array[targetIndex].Mass < Array[currentIndex].Mass)
                            {
                                SwitchPositionJob(currentIndex, targetIndex);
                                ShouldGenerateNewMesh = true;
                                break;
                            }
                        }
                    }
                }
                break;
        }
    }
    private void SwitchPositionJob(int indexA, int indexB_newChunk)
    {
        Voxel tempVoxelA = Array[indexA];
        Voxel tempVoxelB = Array[indexB_newChunk];

        // Calculate the ForceDirection
        int3 newPosition = VoxelMethods.GetPositionFromIndex(indexB_newChunk, ChunkSize);
        int3 oldPosition = VoxelMethods.GetPositionFromIndex(indexA, ChunkSize);

        tempVoxelA.SetForceDirection(newPosition - oldPosition);
        tempVoxelB.SetForceDirection(-tempVoxelA.ForceDirection);

        Array[indexA] = tempVoxelB;
        Array[indexB_newChunk] = tempVoxelA;
    }
}