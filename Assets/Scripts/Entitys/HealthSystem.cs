using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth;
    public float Health {  get; private set; }
    public delegate void OnDie();
    [HideInInspector] public event OnDie onDieEvent; //Special for programming only use
    [SerializeField] private UnityEvent onDie;
    [SerializeField] private UnityEvent onHit;
    private void Start()
    {
        SetAlive();
    }
    /// <summary>
    /// Sets the max health
    /// </summary>
    public void SetAlive()
    {
        Health = maxHealth;
    }
    /// <summary>
    /// Takes damage
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(float damage)
    {
        Health -= damage;

        if (Health < 0) Health = 0;
        else if (Health > maxHealth) Health = maxHealth;

        if (Health <= 0)
        {
            Die();
        }
        onHit?.Invoke();
    }
    /// <summary>
    /// Invokes all on die events
    /// </summary>
    private void Die()
    {
        onDie?.Invoke();
        onDieEvent?.Invoke();
    }
}
