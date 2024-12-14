using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    private enum MoveType
    {
        InFluid,
        Grounded,
        InAir
    }
    private enum GroundType
    {
        None,
        Slippery,
        Slowed
    }
    private GroundType groundType;
    private MoveType moveType;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float airMultiplier = 0.4f;
    [SerializeField] private float fluidMultiplier = 0.2f;
    [SerializeField] private float movementMultiplier = 10f;

    [Header("Sprinting")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float acceleration = 10f;

    [Header("Sprinting Effects")]
    [SerializeField] private Camera cam;
    [SerializeField] private float sprintFOV = 100f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpRate = 15f;
    [SerializeField] private float swinmForce = 50f;

    [Header("Crouching")]
    [SerializeField] private float crouchScale = 0.75f;
    [SerializeField] private float crouchSpeed = 1f;
    [SerializeField] private float crouchMultiplier = 5f;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftShift;

    [Header("Drag")]
    [SerializeField] private float waterDrag = 18f;
    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float slowedGroundDragMultiplier;
    [SerializeField] private float slipperyGroundDragMultiplier = 1.2f;

    [Header("Gravity")]
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float waterGravityMultiplier = 0.5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckPositionOffset;

    [Header("Player")]
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private MeshCollider capsuleCollider;

    [Header("Other")]
    [SerializeField] List<VoxelObject> slipperyGrounds = new List<VoxelObject>();
    [SerializeField] List<VoxelObject> slowedGrounds = new List<VoxelObject>();
    [SerializeField] Transform playerFeet;
    [SerializeField] private Transform orientation;
    [SerializeField] private GameObject waterSplashPrefab;
    private VoxelParticleSystem waterSplash;
    [SerializeField] float voxelVectorForce = 1;

    private float horizontalMovement;
    private float verticalMovement;

    private bool isCrouching;
    private bool isSprinting;
    private bool isMoving;

    private Vector3 moveDirection;
    private float nextTimeToJump = 0f;

    private bool freezePlayer;
    private Bounds playerBounds;

    List<Voxel> intersectingVoxels = new List<Voxel>();
    private void Awake()
    {
        waterSplash = Instantiate(waterSplashPrefab, transform).GetComponent<VoxelParticleSystem>();
    }
    private void Start()
    {
        playerBounds = capsuleCollider.bounds;
        rigidBody.position = Vector3.one * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize / 2;
    }
    private void Update()
    {
        if (WorldManager.Instance.GetChunk(VoxelMethods.GetChunkKeyFromWorldPosition(transform.position), out Chunk chunk))
        {
            if (freezePlayer)
            {
                rigidBody.useGravity = true;
                freezePlayer = false;
            }
            UpdatePlayer();
        }
        else
        {
            rigidBody.useGravity = false;
            rigidBody.velocity = Vector3.zero;
            freezePlayer = true;
            if (!freezePlayer)
            {
                rigidBody.useGravity = false;
                rigidBody.velocity = Vector3.zero;
                freezePlayer = true;
            }
        }
    }
    private void FixedUpdate()
    {
        if (freezePlayer) return;
        ApplyCustomGravity();
        MovePlayer();
        Swim();
    }
    /// <summary>
    /// Updates the player
    /// </summary>
    private void UpdatePlayer()
    {
        if (CheckTriggerOfType(PhysicsType.Fluid))
        {
            if(moveType != MoveType.InFluid)
            {
                foreach(Voxel voxel in intersectingVoxels)
                {
                    Material material = VoxelDefines.Instance.GetVoxelObject(voxel.ID).Material;
                    if (voxel.PhysicsType == PhysicsType.Fluid && material != null)
                    {
                        waterSplash.SetParticleMaterial(material);
                        waterSplash.Emit();
                        break;
                    }
                }
            }
            moveType = MoveType.InFluid;
        }
        else if (GroundCheck())
        {
            moveType = MoveType.Grounded;
        }
        else
        {
            moveType = MoveType.InAir;
        }

        if (moveDirection == new Vector3(0f, 0f, 0f))
        {
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }

        GetMovementInput();
        ControlDrag();
        ControlSpeed();

        if (Input.GetKey(jumpKey) && Time.time >= nextTimeToJump)
        {
            nextTimeToJump = Time.time + 1f / jumpRate;
            Jump();
        }

        Crouch();

        if (isSprinting && isMoving)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, sprintFOV, 8f * Time.deltaTime);
        }
        else if (!isSprinting && isMoving || !isSprinting && !isMoving)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 90f, 8f * Time.deltaTime);
        }
    }
    /// <summary>
    /// Checks if the player is intersecting with a certain phycics type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool CheckTriggerOfType(PhysicsType type)
    {
        intersectingVoxels.Clear();
        if (WorldManager.Instance.GetChunk(VoxelMethods.GetChunkKeyFromWorldPosition(transform.position), out Chunk currentChunk))
        {
            playerBounds = capsuleCollider.bounds;
            Voxel voxel;
            float3 pos;
            for (int i = 0; i < currentChunk.VoxelArray.Length; i++)
            {
                voxel = currentChunk.VoxelArray[i];
                if (currentChunk.VoxelArray[i].PhysicsType == type)
                {
                    pos = VoxelMethods.GetPositionFromIndex(i);
                    pos += currentChunk.Key * VoxelDefines.Instance.ChunkSize;
                    pos *= VoxelDefines.Instance.VoxelSize;

                    Bounds bounds = new Bounds(pos, Vector3.one * VoxelDefines.Instance.VoxelSize);
                    if (playerBounds.Intersects(bounds))
                    {
                        intersectingVoxels.Add(currentChunk.VoxelArray[i]);
                        return true;
                    }
                }
            }
        }
        return false;
    }
    /// <summary>
    /// Checks if player is grounded
    /// </summary>
    /// <returns></returns>
    private bool GroundCheck()
    {
        Vector3 groundCheckPosition = transform.position + Vector3.down * groundCheckPositionOffset;
        if(Physics.CheckSphere(groundCheckPosition, groundCheckRadius, groundMask))
        {
            VoxelGroundCheck();
            return true;
        }
        groundType = GroundType.None;
        return false;
    }
    /// <summary>
    /// Sets the groundtype after the vorxel the player is standing on
    /// </summary>
    /// <returns></returns>
    private bool VoxelGroundCheck()
    {
        if(VoxelMethods.GetVoxelIDFromWorldPosition(playerFeet.position, out int id))
        {
            foreach (VoxelObject slipperyVoxel in slipperyGrounds)
            {
                if (slipperyVoxel.ID == id)
                {
                    groundType = GroundType.Slippery;
                    return true;
                }
                else
                {
                    groundType = GroundType.None;
                }
            }
            foreach (VoxelObject slowVoxel in slowedGrounds)
            {
                if (slowVoxel.ID == id)
                {
                    groundType = GroundType.Slowed;
                    return true;
                }
                else
                {
                    groundType = GroundType.None;
                }
            }
            return true;
        }
        else
        {
                    groundType = GroundType.None;
        }
        return false;
    }
    /// <summary>
    /// Gets the movement input
    /// </summary>
    private void GetMovementInput()
    {
        horizontalMovement = Input.GetAxisRaw("Horizontal");
        verticalMovement = Input.GetAxisRaw("Vertical");

        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
    }
    /// <summary>
    /// Handles jumping
    /// </summary>
    private void Jump()
    {
        if (moveType == MoveType.Grounded)
        {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);
            rigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
        else if (moveType == MoveType.InFluid)
        {
            rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);
            rigidBody.AddForce(transform.up * swinmForce, ForceMode.Force);
        }
    }
    /// <summary>
    /// Handles crouching
    /// </summary>
    private void Crouch()
    {
        if (Input.GetKeyDown(crouchKey) && !isCrouching)
        {
            Vector3 _crouchScale = new Vector3(transform.localScale.x, crouchScale, transform.localScale.z);
            transform.localScale = _crouchScale;

            isCrouching = true;
        }
        else if (Input.GetKeyUp(crouchKey) && isCrouching)
        {
            Vector3 normalScale = new Vector3(transform.localScale.x, 0.9f, transform.localScale.z);
            transform.localScale = normalScale;

            isCrouching = false;
        }
    }
    /// <summary>
    /// Controls speed
    /// </summary>
    private void ControlSpeed()
    {
        if (Input.GetKey(sprintKey) && isMoving)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, sprintSpeed, acceleration * Time.deltaTime);
            isSprinting = true;
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
            isSprinting = false;
        }

        if (Input.GetKey(crouchKey))
        {
            moveSpeed = Mathf.Lerp(moveSpeed, crouchSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            moveSpeed = Mathf.Lerp(moveSpeed, walkSpeed, acceleration * Time.deltaTime);
        }
    }
    /// <summary>
    /// Controls drag
    /// </summary>
    private void ControlDrag()
    {
        switch(moveType)
        {
            case MoveType.InFluid:
                rigidBody.drag = waterDrag;
                break;
            case MoveType.Grounded:
                rigidBody.drag = groundDrag;
                break;
            case MoveType.InAir:
                rigidBody.drag = airDrag;
                break;
        }

        if(moveType  == MoveType.Grounded)
        {
            switch (groundType)
            {
                case GroundType.Slippery:
                    rigidBody.drag = groundDrag / slipperyGroundDragMultiplier;
                    break;
                case GroundType.Slowed:
                    rigidBody.drag *= slowedGroundDragMultiplier;
                    break;
            }
        }

    }
    /// <summary>
    /// Handles swimming
    /// </summary>
    public void Swim()
    {
        if (Input.GetKey(jumpKey) && moveType == MoveType.InFluid)
        {
            rigidBody.AddForce(Vector3.up * moveSpeed * movementMultiplier * fluidMultiplier, ForceMode.Acceleration);
        }
        if (moveType == MoveType.InFluid)
        {
            Vector3 pullDirection = new Vector3(0, 0, 0);
            foreach (Voxel voxel in intersectingVoxels)
            {
                pullDirection += (Vector3)voxel.ForceDirection;
            }
            pullDirection.Normalize();
            Debug.DrawLine(transform.position, transform.position + pullDirection, Color.green);
            rigidBody.AddForce(pullDirection.normalized * voxelVectorForce, ForceMode.Force);
        }
    }
    /// <summary>
    /// Applys custom gravity
    /// </summary>
    private void ApplyCustomGravity()
    {
        if (moveType == MoveType.InAir)
        {
            rigidBody.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
        if (moveType == MoveType.InFluid)
        {
            rigidBody.AddForce(Physics.gravity * waterGravityMultiplier, ForceMode.Acceleration);
        }
    }
    /// <summary>
    /// Moves the player
    /// </summary>
    private void MovePlayer()
    {
        switch(moveType)
        {
            case MoveType.InFluid:
                if (isCrouching)
                {
                    rigidBody.AddForce(moveDirection.normalized * moveSpeed * crouchMultiplier, ForceMode.Acceleration);
                }
                else
                {
                    rigidBody.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier * fluidMultiplier, ForceMode.Acceleration);
                }
                break;

            case MoveType.Grounded:
                rigidBody.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier, ForceMode.Acceleration);
                break;

            case MoveType.InAir:
                rigidBody.AddForce(moveDirection.normalized * moveSpeed * movementMultiplier * airMultiplier, ForceMode.Acceleration);
                break;

            default:
                if (isCrouching)
                {
                    rigidBody.AddForce(moveDirection.normalized * moveSpeed * crouchMultiplier, ForceMode.Acceleration);
                }
                break;
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 groundCheckPosition = transform.position + Vector3.down * groundCheckPositionOffset;
        Gizmos.DrawWireSphere(groundCheckPosition, groundCheckRadius);
    }
}
