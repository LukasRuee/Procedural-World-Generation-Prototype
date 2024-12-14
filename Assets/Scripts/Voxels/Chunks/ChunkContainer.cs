using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkContainer : MonoBehaviour
{
    public Chunk Chunk { get; private set; }
    public int3 ChunkKey { get; private set; }
    [HideInInspector] public Bounds chunkBounds; ///Must be public???
    private List<MeshContainer> meshContainers;
    public bool isVisibleToCamera { get; private set; }

    [SerializeField] private GameObject meshContainerPrefab;
    [SerializeField] private bool drawForceDirection;
    /// <summary>
    /// Initializes the container
    /// </summary>
    public void InitializeContainer()
    {
        meshContainers = new List<MeshContainer>();
        foreach (VoxelObject voxelObject in VoxelDefines.Instance.Voxels)
        {
            GameObject prefab = Instantiate(meshContainerPrefab, Vector3.zero, Quaternion.identity, transform);
            prefab.transform.parent = transform;

            if(voxelObject.Material != null)
            {
                MeshContainer meshContainer = prefab.GetComponent<MeshContainer>();
                meshContainer.SetMaterial(voxelObject.Material);
                meshContainers.Add(meshContainer);
            }
            else
            {
                MeshContainer meshContainer = prefab.GetComponent<MeshContainer>();
                meshContainer.Deactivate();
                meshContainers.Add(meshContainer);
            }
        }
        // Calculate chunk bounds
        chunkBounds = new Bounds(Vector3.zero, new Vector3(VoxelDefines.Instance.ChunkSize, VoxelDefines.Instance.ChunkSize, VoxelDefines.Instance.ChunkSize));
    }
    private void Update()
    {
        if (Chunk == null) return;
        if (!Chunk.Initialized) return;

        chunkBounds.center = transform.position + (chunkBounds.size / 2);

        if (isVisibleToCamera)
        {
            if (Chunk.GenerateMesh())
            {
                OverwriteMeshes();
            }
        }
    }
    /// <summary>
    /// Checks if the chunk is visible to the camera
    /// </summary>
    /// <param name="planes"></param>
    /// <param name="playerPosition"></param>
    public void CheckVisibility(Plane[] planes, Vector3 playerPosition)
    {
        isVisibleToCamera = GeometryUtility.TestPlanesAABB(planes, chunkBounds);
        if (chunkBounds.Contains(playerPosition))
        {
            isVisibleToCamera = true;
        }
        else
        {
            //isVisibleToCamera = CheckOcclusion(cameraPosition);
        }

        foreach (MeshContainer container in meshContainers)
        {
            container.SetVisibility(isVisibleToCamera);
        }
    }
    /// <summary>
    /// Occlusion culls the chunk (NOT WORKING)
    /// </summary>
    /// <param name="cameraPosition"></param>
    /// <returns></returns>
    private bool CheckOcclusion(Vector3 cameraPosition)
    {
        Ray ray = new Ray(cameraPosition, chunkBounds.center - cameraPosition);
        Debug.DrawRay(cameraPosition, chunkBounds.center - cameraPosition, Color.red, 1.0f); // For visualization in scene view

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.distance < Vector3.Distance(cameraPosition, chunkBounds.center) &&
                hit.collider.gameObject != gameObject)
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Activates gameobject and loads chunk
    /// </summary>
    /// <param name="newChunk">chunk as data value for container</param>
    public void ActivateChunk(Chunk newChunk)
    {
        newChunk.Load();
        Chunk = newChunk;
        ChunkKey = Chunk.Key;
        OverwriteMeshes();
        gameObject.SetActive(true);
    }
    /// <summary>
    /// Deactivates gameobject and unloads chunk
    /// </summary>
    public void DeactivateChunk()
    {
        Chunk.Unload();
        Chunk = null;
        gameObject.SetActive(false);
        foreach (MeshContainer container in meshContainers)
        {
            container.Clear();
        }
    }
    /// <summary>
    /// Sets the mesh of the chunks data
    /// </summary>
    private void OverwriteMeshes()
    {
        int index = 0;
        foreach (MeshContainer container in meshContainers)
        {
            container.SetMesh(Chunk.GetMesh(index));
            if (Chunk.GetMesh(index).vertexCount < 4)
            {
                container.SetCollider(null);
            }
            else if (!VoxelDefines.Instance.Voxels[index].IsTransparent)
            {
                container.SetCollider(Chunk.GetMesh(index));
            }
            index++;
        }
    }
    private void OnDrawGizmos()
    {
        float3 pos;
        Vector3 posStart;
        Vector3 posEnd;

        if (drawForceDirection)
        {
            for (int i = 0; i < Chunk.VoxelArray.Length; i++)
            {
                if (!Chunk.VoxelArray[i].ForceDirection.Equals(0) && Chunk.VoxelArray[i].ID != 0)
                {
                    pos = VoxelMethods.GetPositionFromIndex(i);
                    pos += Chunk.Key * VoxelDefines.Instance.ChunkSize;
                    pos *= VoxelDefines.Instance.VoxelSize;

                    posStart = new Vector3(pos.x, pos.y, pos.z);
                    Vector3 direction = Chunk.VoxelArray[i].ForceDirection;
                    direction.Normalize();
                    direction *= VoxelDefines.Instance.VoxelSize / 2;

                    Vector3 offset = new Vector3(VoxelDefines.Instance.VoxelSize / 2, VoxelDefines.Instance.VoxelSize / 2, VoxelDefines.Instance.VoxelSize / 2);

                    posEnd = new Vector3(pos.x + direction.x, pos.y + direction.y, pos.z + direction.z);

                    Gizmos.DrawLine(posStart + offset, posEnd + offset);
                }
            }
        }
    }
}
