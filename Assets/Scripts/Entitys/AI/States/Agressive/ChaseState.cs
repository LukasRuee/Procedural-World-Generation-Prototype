using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseState : State
{
    private Transform target;
    private float attackRange;
    public ChaseState(HostileMob mob, Transform target, float attackRange) : base(mob)
    {
        this.target = target;
        this.attackRange = attackRange;
    }
    public override void Enter() { }
    public override void Update()
    {
        if (target == null)
        {
            mob.Idle();
        }
        else if (CanReachEnemy())
        {
            mob.Attack(target);
        }
        else if (mob.EnemyIsOutOfReach(target))
        {
            mob.Idle();
        }
        else
        {
            mob.MoveTo(target.position);
        }
    }
    public override void Exit() { }

    /// <summary>
    /// Check if the target is in attack range
    /// </summary>
    /// <returns></returns>
    public bool CanReachEnemy()
    {
        if (target == null) return false;
        return Vector3.Distance(mob.transform.position, target.position) <= attackRange;
    }
}