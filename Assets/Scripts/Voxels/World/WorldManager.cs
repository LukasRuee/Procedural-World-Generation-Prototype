using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    private enum UpdateMode
    {
        none,
        async,
        sync,
        jobs
    }

    [Header("Refs")]
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Camera cam;
    [SerializeField] UpdateMode updateMode;

    [Header("Rendering")]
    [SerializeField] private int renderDistance = 4;
    [SerializeField] private int chunkGenerationsPerFrame = 2;

    [Header("Chunk updating")]
    [SerializeField] private float tickRateMS = 1000;
    [SerializeField] private int simulationDistance;
    [SerializeField] private int updateChunksPerFrame = 1;

    private int evenOrOdd;
    private float nextTick;
    private float subTickRateMS;
    private float nextSubTick;

    private int currentYSliceIndex = 0;
    private int currentChunkIndex;
    private int ySlice;

    private List<Chunk> chunksToUpdate = new List<Chunk>();
    private List<Task> updateTasks = new List<Task>();
    private List<int3> positions = new List<int3>();
    private List<int> uniqueYHeights = new List<int>();

    private Dictionary<int3, Chunk> existingChunks = new Dictionary<int3, Chunk>();
    private List<ChunkContainer> containers = new List<ChunkContainer>();
    private Queue<ChunkContainer> chunkQueue = new Queue<ChunkContainer>();
    private List<int3> positionsToLoad = new List<int3>();

    private int chunksGeneratedThisFrame;
    private Vector3 playerPosition;
    public static WorldManager Instance { get; private set; }

    private void Awake()
    {
        subTickRateMS = tickRateMS / (simulationDistance * simulationDistance * simulationDistance);
        if (Instance == null)
        {
            Instance = this;
        }
    }
    private void Start()
    {
        for (int dx = -renderDistance; dx <= renderDistance; dx++)
        {
            for (int dy = -renderDistance; dy <= renderDistance; dy++)
            {
                for (int dz = -renderDistance; dz <= renderDistance; dz++)
                {
                    GameObject chunk = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity, transform);
                    ChunkContainer chunkContainer = chunk.GetComponent<ChunkContainer>();
                    chunkContainer.InitializeContainer();
                    chunkQueue.Enqueue(chunkContainer);
                    containers.Add(chunkContainer);
                }
            }
        }
    }
    private void LateUpdate()
    {
        playerPosition = player.position;
        positionsToLoad = GetChunksAroundPlayer(renderDistance);
        UnloadChunks();
        LoadChunks();
        DynamicOcclusionCulling();
    }
    private void Update()
    {
        if (Menu.Instance.CurrentGameState != Menu.GameState.Playing) return;
        switch (updateMode)
        {
            case UpdateMode.sync: UpdateChunksSync(); break;
            case UpdateMode.async: UpdateChunksTask(); break;
            case UpdateMode.jobs: UpdateChunksJob(); break;
            case UpdateMode.none: break;
        }
    }
    #region Chunk updating
    /// <summary>
    /// Updates chunks synchron
    /// </summary>
    private void UpdateChunksSync()
    {
        // Reset and initialize if necessary
        if (positions == null || positions.Count == 0)
        {
            positions = GetChunksAroundPlayer(simulationDistance);

            foreach (int3 pos in positions)
            {
                uniqueYHeights.Add(pos.y);
            }

            uniqueYHeights = uniqueYHeights.Distinct().ToList();
            uniqueYHeights.Sort();

            currentYSliceIndex = 0;
            currentChunkIndex = 0;
        }

        for (int chunksUpdated = 0; chunksUpdated < updateChunksPerFrame; chunksUpdated++)
        {
            if (currentYSliceIndex < uniqueYHeights.Count)
            {
                ySlice = uniqueYHeights[currentYSliceIndex];
                for (; currentChunkIndex < positions.Count; currentChunkIndex++)
                {
                    int3 pos = positions[currentChunkIndex];
                    if (pos.y == ySlice && existingChunks.TryGetValue(pos, out Chunk chunk))
                    {
                        int sum = chunk.Key.x + chunk.Key.y + chunk.Key.z;
                        int normalizedSum = ((sum % 2) + 2) % 2;
                        if (chunk.Initialized && normalizedSum == evenOrOdd)
                        {
                            chunk.UdpateVoxel();
                            currentChunkIndex++;
                            break; // Exit the inner for loop after updating one chunk
                        }
                    }
                }

                // Move to the next Y slice if all chunks in the current slice are processed
                if (currentChunkIndex >= positions.Count)
                {
                    currentChunkIndex = 0;
                    currentYSliceIndex++;
                }
            }
            else
            {
                // Switch to next even/odd set and reset indices
                evenOrOdd = evenOrOdd == 0 ? 1 : 0;
                currentYSliceIndex = 0;
                currentChunkIndex = 0;

                // Clear lists and reset positions
                updateTasks.Clear();
                positions.Clear();
                uniqueYHeights.Clear();
                chunksToUpdate.Clear();

                // Reinitialize positions and uniqueYHeights for the next cycle
                positions = GetChunksAroundPlayer(simulationDistance);

                foreach (int3 pos in positions)
                {
                    uniqueYHeights.Add(pos.y);
                }

                uniqueYHeights = uniqueYHeights.Distinct().ToList();
                uniqueYHeights.Sort();
            }
        }
    }
    /// <summary>
    /// Updates chunks pe tasks
    /// </summary>
    private void UpdateChunksTask()
    {
        // Reset and initialize if necessary
        if (positions == null || positions.Count == 0)
        {
            positions = GetChunksAroundPlayer(simulationDistance);

            foreach (int3 pos in positions)
            {
                uniqueYHeights.Add(pos.y);
            }

            uniqueYHeights = uniqueYHeights.Distinct().ToList();
            uniqueYHeights.Sort();

            currentYSliceIndex = 0;
            currentChunkIndex = 0;
        }

        for (int chunksUpdated = 0; chunksUpdated < updateChunksPerFrame; chunksUpdated++)
        {
            if (currentYSliceIndex < uniqueYHeights.Count)
            {
                ySlice = uniqueYHeights[currentYSliceIndex];
                for (; currentChunkIndex < positions.Count; currentChunkIndex++)
                {
                    int3 pos = positions[currentChunkIndex];
                    if (pos.y == ySlice && existingChunks.TryGetValue(pos, out Chunk chunk))
                    {
                        int sum = chunk.Key.x + chunk.Key.y + chunk.Key.z;
                        int normalizedSum = ((sum % 2) + 2) % 2;
                        if (chunk.Initialized && normalizedSum == evenOrOdd && chunk.IsUpdating == false)
                        {
                            //chunk.UdpateVoxel();
                            Task.Run(() => chunk.UdpateVoxTask());
                            currentChunkIndex++;
                            break; // Exit the inner for loop after updating one chunk
                        }
                    }
                }

                // Move to the next Y slice if all chunks in the current slice are processed
                if (currentChunkIndex >= positions.Count)
                {
                    currentChunkIndex = 0;
                    currentYSliceIndex++;
                }
            }
            else
            {
                // Switch to next even/odd set and reset indices
                evenOrOdd = evenOrOdd == 0 ? 1 : 0;
                currentYSliceIndex = 0;
                currentChunkIndex = 0;

                // Clear lists and reset positions
                updateTasks.Clear();
                positions.Clear();
                uniqueYHeights.Clear();
                chunksToUpdate.Clear();

                // Reinitialize positions and uniqueYHeights for the next cycle
                positions = GetChunksAroundPlayer(simulationDistance);

                foreach (int3 pos in positions)
                {
                    uniqueYHeights.Add(pos.y);
                }

                uniqueYHeights = uniqueYHeights.Distinct().ToList();
                uniqueYHeights.Sort();
            }
        }
    }
    /// <summary>
    /// Updates chunks per job system (EXPERIMENTELL)
    /// </summary>
    private void UpdateChunksJob()
    {
        // Reset everything
        positions.Clear();
        positions = GetChunksAroundPlayer(simulationDistance);

        foreach (int3 pos in positions)
        {
            if (GetChunk(pos, out Chunk chunk))
            {
                if(!chunk.IsUpdating)
                {
                    chunk.UdpateVoxelJobs();
                }
            }
        }
    }

    #endregion
    #region Chunk loading
    /// <summary>
    /// Gets all the chunk positions around the player
    /// </summary>
    private List<int3> GetChunksAroundPlayer(int maxDistance)
    {
        List<int3> positions = new List<int3>();
        int3 playerChunk = new int3(Mathf.FloorToInt(playerPosition.x / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize)),
                                    Mathf.FloorToInt(playerPosition.y / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize)),
                                    Mathf.FloorToInt(playerPosition.z / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize)));
        //Load chunks
        for (int dx = -maxDistance; dx <= maxDistance; dx++)
        {
            for (int dy = -maxDistance; dy <= maxDistance; dy++)
            {
                for (int dz = -maxDistance; dz <= maxDistance; dz++)
                {
                    int3 chunkPos = new int3(playerChunk.x + dx,
                                             playerChunk.y + dy,
                                             playerChunk.z + dz);

                    positions.Add(chunkPos);
                }
            }
        }
        return positions;
    }
    /// <summary>
    /// Loads the chunks for the player
    /// </summary>
    private void LoadChunks()
    {
        chunksGeneratedThisFrame = 0;
        if (chunkQueue.Count > 0)
        {
            foreach (int3 pos in positionsToLoad)
            {
                if (GetChunk(pos, out Chunk chunk))
                {
                    if (!chunk.Loaded)
                    {
                        ActivateChunk(chunk);
                    }
                }
                else if (chunksGeneratedThisFrame < chunkGenerationsPerFrame)
                {
                    chunksGeneratedThisFrame++;
                    Chunk newChunk = new Chunk();
                    existingChunks.Add(pos, newChunk);
                    WorldGenerator.Instance.SpawnChunk(pos, newChunk);
                }
            }
        }
    }
    /// <summary>
    /// Unloads chunks if player too far away
    /// </summary>
    private void UnloadChunks()
    {
        foreach (ChunkContainer container in containers)
        {
            if (positionsToLoad.Contains(container.ChunkKey) == false && container.Chunk != null)
            {
                container.DeactivateChunk();
                chunkQueue.Enqueue(container);
            }

        }
    }
    /// <summary>
    /// Activates a chunk from a queue
    /// </summary>
    /// <param name="chunk"></param>
    private void ActivateChunk(Chunk chunk)
    {
        if (chunkQueue.Count == 0) return;
        ChunkContainer chunkContainer = chunkQueue.Dequeue();

        chunkContainer.transform.position = new Vector3(
                                            chunk.Key.x * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize,
                                            chunk.Key.y * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize,
                                            chunk.Key.z * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize);

        chunkContainer.ActivateChunk(chunk);
    }
    /// <summary>
    /// Regenerate meshes of a chunk
    /// </summary>
    /// <param name="chunk"></param>
    public void RegenerateMeshes(Chunk chunk)
    {
        foreach (ChunkContainer container in containers)
        {
            if (container.Chunk.Key.Equals(chunk.Key))
            {
                chunk.GenerateMesh();
            }
        }
    }
    #endregion

    /// <summary>
    /// Dynamically perform occlusion culling for chunks
    /// </summary>
    private void DynamicOcclusionCulling()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        foreach (ChunkContainer container in containers)
        {
            container.CheckVisibility(planes, playerPosition);
        }
    }
    /// <summary>
    /// Checks if a chunks exists at a position, if out of bounds, it can correct the position
    /// </summary>
    /// <param name="currentChunkKey"></param>
    /// <param name="targetPos"></param>
    /// <param name="chunk"></param>
    /// <param name="correctedTargetPos"></param>
    /// <returns></returns>
    public bool ChunkExists(int3 currentChunkKey, int3 targetPos, out Chunk chunk, out int3 correctedTargetPos)
    {
        int3 chunkKey = currentChunkKey;

        // Adjust x position and chunk key
        if (targetPos.x < 0)
        {
            chunkKey.x -= 1;
            targetPos.x += VoxelDefines.Instance.ChunkSize;
        }
        else if (targetPos.x >= VoxelDefines.Instance.ChunkSize)
        {
            chunkKey.x += 1;
            targetPos.x -= VoxelDefines.Instance.ChunkSize;
        }

        // Adjust y position and chunk key
        if (targetPos.y < 0)
        {
            chunkKey.y -= 1;
            targetPos.y += VoxelDefines.Instance.ChunkSize;
        }
        else if (targetPos.y >= VoxelDefines.Instance.ChunkSize)
        {
            chunkKey.y += 1;
            targetPos.y -= VoxelDefines.Instance.ChunkSize;
        }

        // Adjust z position and chunk key
        if (targetPos.z < 0)
        {
            chunkKey.z -= 1;
            targetPos.z += VoxelDefines.Instance.ChunkSize;
        }
        else if (targetPos.z >= VoxelDefines.Instance.ChunkSize)
        {
            chunkKey.z += 1;
            targetPos.z -= VoxelDefines.Instance.ChunkSize;
        }

        correctedTargetPos = targetPos;

        if (existingChunks.TryGetValue(chunkKey, out Chunk newChunk))
        {
            if (newChunk.Initialized)
            {
                chunk = newChunk;
                return true;
            }
        }

        chunk = new Chunk();
        return false;
    }
    /// <summary>
    /// Gets a chunk if it exists
    /// </summary>
    /// <param name="chunkKey"></param>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public bool GetChunk(int3 chunkKey, out Chunk chunk)
    {
        if (existingChunks.TryGetValue(chunkKey, out Chunk targetChunk))
        {
            if(targetChunk.Initialized)
            {
                chunk = targetChunk;
                return true;
            }
        }
        chunk = null;
        return false;
    }
}

public static class VoxelMethods
{
    /// <summary>
    /// Calculates the index in an array of the position inside the chunk
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static int GetIndexFromPosition(int3 position)
    {
        return position.x + VoxelDefines.Instance.ChunkSize * (position.y + VoxelDefines.Instance.ChunkSize * position.z);
    }
    /// <summary>
    /// Calculates the index in an array of the position inside the chunk
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static int GetIndexFromPosition(int3 position, int chunkSize)
    {
        return position.x + chunkSize * (position.y + chunkSize * position.z);
    }
    /// <summary>
    /// Checks if the index is inside a chunks bounds
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static bool IsIndexInsideChunkBounds(int index)
    {
        return index >= 0 && index < VoxelDefines.Instance.TotalVoxelsPerChunk;
    }
    /// <summary>
    /// Checks if the index is inside a chunks bounds
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static bool IsIndexInsideCurrentChunk(int index, int chunkSize)
    {
        return index >= 0 && index < chunkSize;
    }
    /// <summary>
    /// Checks if the position is inside a chunks bounds
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static bool IsPositionInsideChunkBounds(int3 pos)
    {
        return pos.x >= 0 && pos.x < VoxelDefines.Instance.ChunkSize &&
                pos.y >= 0 && pos.y < VoxelDefines.Instance.ChunkSize &&
                pos.z >= 0 && pos.z < VoxelDefines.Instance.ChunkSize;
    }
    /// <summary>
    /// Calculates the position of an index
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static int3 GetPositionFromIndex(int index)
    {
        int x = index % VoxelDefines.Instance.ChunkSize;
        int y = (index / VoxelDefines.Instance.ChunkSize) % VoxelDefines.Instance.ChunkSize;
        int z = index / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.ChunkSize);
        return new int3(x, y, z);
    }
    /// <summary>
    /// Calculates the position of an index
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static int3 GetPositionFromIndex(int index, int chunkSize)
    {
        int x = index %  chunkSize;
        int y = (index / chunkSize) % chunkSize;
        int z = index / (chunkSize * chunkSize);
        return new int3(x, y, z);
    }
    /// <summary>
    /// Calculates the world position of an chunkkey and the voxels position inside the chunk
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static float3 GetWorldPositionFromChunkKeyAndVoxelPosition(int3 chunkKey, int3 voxelPosition)
    {
        float3 chunkWorldPosition = new float3(
            chunkKey.x * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize,
            chunkKey.y * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize,
            chunkKey.z * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize
        );

        float3 voxelWorldPosition = new float3(
            voxelPosition.x * VoxelDefines.Instance.VoxelSize,
            voxelPosition.y * VoxelDefines.Instance.VoxelSize,
            voxelPosition.z * VoxelDefines.Instance.VoxelSize
        );

        return chunkWorldPosition + voxelWorldPosition;
    }
    /// <summary>
    /// Calculates a voxels position inside a chunk after a world positio
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="chunkKey"></param>
    /// <returns></returns>
    public static int3 GetLocalVoxelPosition(Vector3 worldPosition, int3 chunkKey)
    {
        // Convert world position to a local position relative to the chunk
        float3 localPos = (float3)worldPosition - (float3)chunkKey * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize;
        localPos /= VoxelDefines.Instance.VoxelSize;

        // Convert the local position to integer coordinates
        int3 localPositionInt = new int3(
            Mathf.FloorToInt(localPos.x),
            Mathf.FloorToInt(localPos.y),
            Mathf.FloorToInt(localPos.z)
        );

        return localPositionInt;
    }
    /// <summary>
    /// Gets a chunkkey from a world position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public static int3 GetChunkKeyFromWorldPosition(float3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize));
        int y = Mathf.FloorToInt(worldPosition.y / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize));
        int z = Mathf.FloorToInt(worldPosition.z / (VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize));
        return new int3(x, y, z);
    }
    /// <summary>
    /// Gets a chunkkey and the voxels position from a world position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="chunkKey"></param>
    /// <param name="position"></param>
    public static void GetChunkKeyAndPositionFromWorldPosition(float3 worldPosition, out int3 chunkKey, out int3 position)
    {
        chunkKey = GetChunkKeyFromWorldPosition(worldPosition);
        position = GetLocalVoxelPosition(worldPosition, chunkKey);
    }
    /// <summary>
    /// Gets a voxels ID from a world position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool GetVoxelIDFromWorldPosition(float3 worldPosition, out int id)
    {
        id = 0;
        int3 chunkKey = GetChunkKeyFromWorldPosition(worldPosition);
        int3 position = GetLocalVoxelPosition(worldPosition, chunkKey);
        int index = GetIndexFromPosition(position);

        WorldManager.Instance.GetChunk(chunkKey, out Chunk chunk);
        if(chunk != null)
        {
            id = chunk.VoxelArray[index].ID;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Gets a worldposition from a chunkkey and a voxel index
    /// </summary>
    /// <param name="chunkKey"></param>
    /// <param name="voxelIndex"></param>
    /// <returns></returns>
    public static float3 GetWorldPositionFromChunkKeyAndVoxelIndex(int3 chunkKey, int voxelIndex)
    {
        int3 voxelPosition = GetPositionFromIndex(voxelIndex);

        float3 chunkWorldPosition = new float3(
            chunkKey.x * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize,
            chunkKey.y * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize,
            chunkKey.z * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize
        );

        float3 voxelWorldPosition = new float3(
            voxelPosition.x * VoxelDefines.Instance.VoxelSize,
            voxelPosition.y * VoxelDefines.Instance.VoxelSize,
            voxelPosition.z * VoxelDefines.Instance.VoxelSize
        );

        return chunkWorldPosition + voxelWorldPosition;
    }
}