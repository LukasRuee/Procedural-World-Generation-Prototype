using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvadeState : State
{
    private Transform target;
    public EvadeState(Mob mob, Transform target) : base(mob)
    {
        this.target = target;
    }
    public override void Enter() { }
    public override void Update()
    {
        if (target == null)
        {
            mob.Idle();
        }
        else if (mob.EnemyIsOutOfReach(target))
        {
            mob.Idle();
        }
        else
        {
            Vector3 directionToEnemy = (target.position - mob.transform.position).normalized;
            mob.MoveTo(-directionToEnemy);
        }
    }
    public override void Exit() { }
}