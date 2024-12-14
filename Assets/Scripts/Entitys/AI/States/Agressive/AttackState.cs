using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State
{
    private Transform target;
    private float attackRange;
    private float attackDamage;
    private float attackCooldown;
    private float lastAttackTime;
    public AttackState(Mob mob, Transform target, float attackRange, float attackDamage, float attackCooldown) : base(mob)
    {
        this.target = target;
        this.attackRange = attackRange;
        this.attackDamage = attackDamage;
        this.attackCooldown = attackCooldown;
    }
    public override void Enter()
    {
        AttackEnemy();
    }
    public override void Update()
    {
        if (!CanReachEnemy())
        {
            mob.Chase(target);
        }
        else
        {
            AttackEnemy();
        }
    }
    /// <summary>
    /// Attacks the target when in range
    /// </summary>
    public void AttackEnemy()
    {
        if (target != null && Time.time >= lastAttackTime + attackCooldown)
        {
            target.gameObject.GetComponentInParent<HealthSystem>().TakeDamage(attackDamage);
            lastAttackTime = Time.time;
        }
    }
    /// <summary>
    /// Check if the target is in attack range
    /// </summary>
    /// <returns></returns>
    public bool CanReachEnemy()
    {
        if (target == null) return false;
        return Vector3.Distance(mob.transform.position, target.position) <= attackRange;
    }
    public override void Exit() { }
}