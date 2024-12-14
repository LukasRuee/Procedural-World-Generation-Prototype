using MyBox;
using System.Collections.Generic;
using UnityEngine;

public class PassiveMob : Mob
{
    public override void Roam()
    {
        ChangeState(new RoamingState(this, UnityEngine.Random.Range(roamTime.x, roamTime.y), walkRange));
    }
    public override void Idle()
    {
        ChangeState(new IdleState(this, UnityEngine.Random.Range(idleTime.x, idleTime.y)));
    }
    public override void Evade(Transform target)
    {
        ChangeState(new EvadeState(this, target));
    }
    private void Update()
    {
        currentState.Update();
    }
}
