using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerInteractions : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [HideInInspector, SerializeField] private float maxDistance = 10;
    [HideInInspector, Min(0.01f), SerializeField] private float stepSize = 0.1f;
    private void Update()
    {
        if (Menu.Instance.CurrentGameState != Menu.GameState.Playing) return;
        if (Input.GetMouseButton(0))
        {
            InventoryManager.Instance.UseItem();
        }
    }
    /// <summary>
    /// Custom raycasting for in chunks
    /// </summary>
    /// <returns></returns>
    public (int3 chunkKey, int index) RayCast()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 origin = ray.origin;
        Vector3 direction = ray.direction;
        Vector3 currentWorldPosition = origin;

        for (float distance = 0; distance < maxDistance; distance += stepSize)
        {
            int3 chunkKey = VoxelMethods.GetChunkKeyFromWorldPosition(currentWorldPosition);
            if (WorldManager.Instance.GetChunk(chunkKey, out Chunk chunk))
            {
                int3 localPositionInt = VoxelMethods.GetLocalVoxelPosition(currentWorldPosition, chunkKey);

                int index = VoxelMethods.GetIndexFromPosition(localPositionInt);

                if (index >= 0 && index < chunk.VoxelArray.Length)
                {
                    Voxel voxel = chunk.VoxelArray[index];
                    if (voxel.PhysicsType != PhysicsType.None && voxel.IsTransparent == false)
                    {
                        return (chunkKey, index);
                    }
                }
            }
            currentWorldPosition += direction * stepSize;
        }

        return (int3.zero, -1);
    }
}
