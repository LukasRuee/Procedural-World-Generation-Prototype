using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : State
{
    private float idleTime;
    private float timer;
    public IdleState(Mob mob, float duration) : base(mob)
    {
        idleTime = duration;
    }
    public override void Enter()
    {
        timer = 0;
    }
    public override void Update()
    {
        timer += Time.deltaTime;

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
        
        if (timer >= idleTime)
        {
            mob.Roam();
        }
    }
    public override void Exit() { }
}