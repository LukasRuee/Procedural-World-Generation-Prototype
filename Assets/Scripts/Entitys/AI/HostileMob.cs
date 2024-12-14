using MyBox;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class HostileMob : Mob
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = -2f;
    [SerializeField] private float attackCooldown = 1.5f;
    public override void Attack(Transform target)
    {
        ChangeState(new AttackState(this, target, attackRange, attackDamage, attackCooldown));
    }
    public override void Roam()
    {
        ChangeState(new RoamingState(this, UnityEngine.Random.Range(roamTime.x, roamTime.y), walkRange));
    }
    public override void Idle()
    {
        ChangeState(new IdleState(this, UnityEngine.Random.Range(idleTime.x, idleTime.y)));
    }
    public override void Chase(Transform target)
    {
        ChangeState(new ChaseState(this, target, attackRange));
    }
    private void Update()
    {
        currentState.Update();
    }
}