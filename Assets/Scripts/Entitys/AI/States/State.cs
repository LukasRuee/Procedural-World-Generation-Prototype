using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State
{
    protected Mob mob;
    public State(Mob mob)
    {
        this.mob = mob;
    }
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
