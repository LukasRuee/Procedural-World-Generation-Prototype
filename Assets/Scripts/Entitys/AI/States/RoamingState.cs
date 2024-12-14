using MyBox;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
public class RoamingState : State
{
    private Vector3 roamPosition;
    private float duration;
    private float timer;
    private float walkRange;
    public RoamingState(Mob mob, float duration, float walkRange) : base(mob)
    {
        this.duration = duration;
        this.walkRange = walkRange;
    }
    public override void Enter()
    {
        timer = 0f;
        if (GetRoamPosition() == false)
        {
            mob.Idle();
            return;
        }
    }
    public override void Update()
    {
        if (mob.IsAggressive)
        {
            if (mob.GetEntitiesInView("Player", "PassiveEntity"))
            {
                mob.GetTarget(out Transform target);
                mob.Chase(target);
            }
        }
        else
        {
            if (mob.GetEntitiesInView("Player", "AggressiveEntity"))
            {
                mob.GetTarget(out Transform target);
                mob.Evade(target);
            }
        }
        timer += Time.deltaTime;

        if (timer < duration)
        {
            mob.MoveTo(roamPosition);

            if (HasReachedDestination(roamPosition))
            {
                mob.Idle();
            }
        }
        else
        {
            mob.Idle();
        }
    }
    public override void Exit() { }
    /// <summary>
    /// Checks if the set roam position is reached
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public bool HasReachedDestination(Vector3 destination)
    {
        return Vector3.Distance(mob.transform.position, destination) < 1f * VoxelDefines.Instance.VoxelSize;
    }
    /// <summary>
    /// Selects a new position to roam to
    /// </summary>
    /// <returns></returns>
    public bool GetRoamPosition()
    {
        Vector3 pos = UnityEngine.Random.insideUnitSphere * walkRange;
        pos += mob.transform.position;
        Vector3Int temp = pos.ToVector3Int();
        int3 startPosition = new int3
        {
            x = temp.x,
            y = temp.y,
            z = temp.z
        };
        int3 chunkKey;

        Chunk targetChunk;
        bool canCheck = true;
        int maxTries = 10;
        int counter = 0;
        int3 arrayPos;
        bool lastCheckedVoxelTransparent = false;

        while (canCheck)
        {
            counter++;
            VoxelMethods.GetChunkKeyAndPositionFromWorldPosition(startPosition, out chunkKey, out arrayPos);

            if (WorldManager.Instance.GetChunk(chunkKey, out targetChunk))
            {
                if (counter == 1)
                {
                    lastCheckedVoxelTransparent = targetChunk.VoxelArray[VoxelMethods.GetIndexFromPosition(arrayPos)].IsTransparent;
                }

                if (lastCheckedVoxelTransparent == true && targetChunk.VoxelArray[VoxelMethods.GetIndexFromPosition(arrayPos)].IsTransparent == false)
                {
                    startPosition.y++;
                    roamPosition = (float3)startPosition;
                    return true;
                }
                else
                {
                    startPosition.y--;
                }
                lastCheckedVoxelTransparent = targetChunk.VoxelArray[VoxelMethods.GetIndexFromPosition(arrayPos)].IsTransparent;
            }
            else
            {
                return false;
            }

            if (counter >= maxTries)
            {
                return false;
            }
        }
        return false;
    }
}