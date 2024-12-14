using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(ObjectPool))]
public class MobManager : MonoBehaviour
{
    [Header("Spawn Radius")]
    [SerializeField] private int spawnMaxDistance;
    [SerializeField] private int spawnMinDistance;
    [Header("Timers")]
    [SerializeField] private float spawnPause = 10;
    [SerializeField] private float despawnTime = 30f;
    [Header("Entity")]
    [SerializeField] private int maxEnteties;
    [SerializeField] private Transform player;
    [field: SerializeField] public ObjectPool Pool {  get; private set; }
    [Header("Debug")]
    [SerializeField] private bool showGizmos;

    private List<MobInstance> mobInstances = new List<MobInstance>();
    private int currentEntities = 0;
    private int3 chunkKey;
    private float timer;
    private void Awake()
    {
        timer = spawnPause;
        Pool.FillQueue(maxEnteties);
    }
    private void Update()
    {
        if (timer <= 0 && currentEntities < maxEnteties)
        {
            timer = spawnPause;
            if (CanSpawnMob(out int3 spawnPosition))
            {
                SpawnEntity(spawnPosition);
            }
        }
        timer -= Time.deltaTime;
        CheckForDespawn();
    }
    /// <summary>
    /// Checks if a mob can spawn 
    /// </summary>
    /// <returns></returns>
    private bool CanSpawnMob(out int3 spawnPosition)
    {
        spawnPosition = (int3)(float3)player.position +
            (int3)((float3)UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(spawnMinDistance, spawnMaxDistance));

        int3 arrayPos;
        bool lastCheckedVoxelTransparent;
        VoxelMethods.GetChunkKeyAndPositionFromWorldPosition(spawnPosition, out chunkKey, out arrayPos);

        if (WorldManager.Instance.GetChunk(chunkKey, out Chunk targetChunk) == false)
        {
            return false;
        }

        lastCheckedVoxelTransparent = targetChunk.VoxelArray[VoxelMethods.GetIndexFromPosition(arrayPos)].IsTransparent;

        for (int i = 0; i < 25; i++)
        {
            VoxelMethods.GetChunkKeyAndPositionFromWorldPosition(spawnPosition, out chunkKey, out arrayPos);

            if (IsInsideSpawnBounds(spawnPosition) && WorldManager.Instance.GetChunk(chunkKey, out targetChunk))
            {
                if (lastCheckedVoxelTransparent && targetChunk.VoxelArray[VoxelMethods.GetIndexFromPosition(arrayPos)].IsTransparent == false)
                {
                    spawnPosition.y++;
                    return true;
                }
                else
                {
                    spawnPosition.y--;
                }
                lastCheckedVoxelTransparent = targetChunk.VoxelArray[VoxelMethods.GetIndexFromPosition(arrayPos)].IsTransparent;
            }
            else
            {
                return false;
            }
        }
        return false;
    }
    /// <summary>
    /// Checks if the mob is outside the players loaded mob range
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsInsideSpawnBounds(int3 position)
    {
        float distance = math.distance((float3)player.position, (float3)position);
        return distance >= spawnMinDistance && distance <= spawnMaxDistance;
    }
    /// <summary>
    /// Spawns an entity
    /// </summary>
    /// <param name="spawnPosition"></param>
    private void SpawnEntity(int3 spawnPosition)
    {
        currentEntities++;
        Mob mob = Pool.GetObject().GetComponent<Mob>();
        mob.transform.position = (float3)spawnPosition;
        mob.gameObject.SetActive(true);
        mobInstances.Add(new MobInstance(mob, Time.time, spawnPosition, this));
    }
    /// <summary>
    /// Checks if a mob must be despawned
    /// </summary>
    private void CheckForDespawn()
    {
        for (int i = mobInstances.Count - 1; i >= 0; i--)
        {
            MobInstance instance = mobInstances[i];
            instance.UpdatePosition();
            float distance = math.distance((float3)player.position, instance.Position);
            if (distance > spawnMaxDistance)
            {
                instance.TimeOutsideBounds += Time.deltaTime;
                if (instance.TimeOutsideBounds >= despawnTime)
                {
                    DespawnEntity(instance);
                }
            }
            else
            {
                instance.TimeOutsideBounds = 0;
            }
        }
    }
    /// <summary>
    /// Despawns an entity
    /// </summary>
    /// <param name="instance"></param>
    private void DespawnEntity(MobInstance instance)
    {
        Pool.ReturnObject(instance.Mob.gameObject);
        mobInstances.Remove(instance);
        currentEntities--;
    }
    /// <summary>
    /// Despawns a mob
    /// </summary>
    /// <param name="mob"></param>
    public void DespawnMob(Mob mob)
    {
        MobInstance instanceToRemove = null;
        foreach (MobInstance mobInstance in mobInstances)
        {
            if(mobInstance.Mob == mob)
            {
                instanceToRemove = mobInstance;
            }
        }
        if(instanceToRemove != null)
        {
            ItemDropper.Instance.DropRandomItem(mob.transform.position);
            DespawnEntity(instanceToRemove);
        }
    }
    private void OnDrawGizmos()
    {
        if (showGizmos == false) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, spawnMinDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, spawnMaxDistance);
    }
}
public class MobInstance
{
    public Mob Mob;
    public float SpawnTime;
    public float3 Position;
    public float TimeOutsideBounds;
    public MobInstance(Mob mob, float spawnTime, float3 position, MobManager mobManager)
    {
        Mob = mob;
        SpawnTime = spawnTime;
        Position = position;
        TimeOutsideBounds = 0;
        mob.AwakeEntity(mobManager);

        mob.HealthSystem.onDieEvent += () => mobManager.DespawnMob(mob);
    }
    /// <summary>
    /// Updates the position
    /// </summary>
    public void UpdatePosition()
    {
        Position = Mob.transform.position;
    }
}
