using MyBox;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(HealthSystem))]
public abstract class Mob : MonoBehaviour
{
    [SerializeField] protected UnityEvent startEvent;
    [SerializeField] private Rigidbody rigidBody;
    [field: SerializeField] public bool IsAggressive { get; private set; }
    [Header("Obstacles")]
    [SerializeField] private float obstacleCheckSize;
    [SerializeField] private float obstacleCheckOffset = 0.1f;
    [SerializeField] private float3 groundDetectionBoxSize;
    [SerializeField] private float groundDetectionOffset;
    [Header("Entity Detection")]
    [SerializeField] protected float detectionRange = 10f;
    protected Transform targetEnemy;
    [Header("Roaming")]
    [SerializeField] protected float2 roamTime;
    [SerializeField] protected float walkRange = 15f;
    protected Vector3 targetRoamPosition;
    [Header("Idle")]
    [SerializeField] protected float2 idleTime;
    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float jumpCooldown = 2f; 

    private float lastJumpTime;
    protected State currentState;
    protected MobManager mobManager;
    [field: SerializeField] public HealthSystem HealthSystem {  get; private set; }
    public List<GameObject> MobsInView { get; private set; } = new List<GameObject>();
    public virtual void AwakeEntity(MobManager mobManager)
    {
        this.mobManager = mobManager;
        ForceSetState(new IdleState(this, UnityEngine.Random.Range(idleTime.x, idleTime.y)));
        startEvent.Invoke();
    }
    public void ForceSetState(State newState)
    {
        currentState = newState;
    }
    public void ChangeState(State newState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
    public virtual void Idle() { }
    public virtual void Roam() { }
    public virtual void Evade(Transform target) { }
    public virtual void Attack(Transform target) { }
    public virtual void Chase(Transform target) { }
    /// <summary>
    /// Selects a target
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool GetTarget(out Transform target)
    {
        target = null;
        if (GetEntitiesInView("PassiveEntity", "Player"))
        {
            foreach (GameObject gameObject in MobsInView)
            {
                if (gameObject.CompareTag("Player"))
                {
                    target = gameObject.transform;
                    return true;
                }
                else if (gameObject.CompareTag("PassiveEntity"))
                {
                    target = gameObject.transform;
                }
            }
            return true;
        }
        return false;
    }
    /// <summary>
    /// Checks if the enemy is out of reach
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool EnemyIsOutOfReach(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) >= detectionRange;
    }
    /// <summary>
    /// Gets all Entitys in view
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public bool GetEntitiesInView(params string[] tags)
    {
        MobsInView.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange);
        foreach (var hitCollider in hitColliders)
        {
            foreach (string tag in tags)
            {
                if (hitCollider.CompareTag(tag))
                {
                    MobsInView.Add(hitCollider.gameObject);
                    break;
                }
            }
        }
        return MobsInView.Count > 0;
    }
    /// <summary>
    /// Checks if a obstacle blocks the entity
    /// </summary>
    /// <returns></returns>
    private bool IsObstacleInFront()
    {
        Vector3 offset = transform.forward * obstacleCheckOffset;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position + offset, obstacleCheckSize);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Voxel"))
            {
                return true;
            }
        }
        return false;
    }
    private void Jump()
    {
        Vector3 position = transform.position;
        position.y += groundDetectionOffset;
        if (Physics.BoxCast(position, groundDetectionBoxSize, Vector3.down, rigidBody.rotation, Mathf.Infinity) && Time.time >= lastJumpTime + jumpCooldown)
        {
            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
        }
    }
    /// <summary>
    /// Lets the entity move towards the target position
    /// </summary>
    /// <param name="target"></param>
    public void MoveTo(Vector3 target)
    {
        RotateToPos(target);
        if (IsObstacleInFront())
        {
            Jump();
        }
        Vector3 direction = (target - transform.position).normalized;
        rigidBody.velocity = new Vector3(direction.x * speed, rigidBody.velocity.y, direction.z * speed);
    }
    /// <summary>
    /// Rotates the entity towards a target position
    /// </summary>
    /// <param name="targetPosition"></param>
    private void RotateToPos(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        directionToTarget.y = 0;

        if (directionToTarget == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
