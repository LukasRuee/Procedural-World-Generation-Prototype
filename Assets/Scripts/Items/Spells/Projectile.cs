using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : SpellBase
{
    [SerializeField] private float startForce;
    [SerializeField] private float continuesForce;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private int maxBounces = 3; 
    private int currentBounces = 0;

    public override void Cast()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetDirection;
        if (Physics.Raycast(ray, out hit))
        {
            targetDirection = (hit.point - transform.position).normalized;
        }
        else
        {
            targetDirection = Camera.main.transform.forward;
        }

        rigidBody.AddForce(targetDirection * startForce, ForceMode.Impulse);

        StartCoroutine(ApplyContinuousForce(targetDirection));
        StartCoroutine(DestroyAfterTime());
    }
    private IEnumerator ApplyContinuousForce(Vector3 direction)
    {
        while (true)
        {
            rigidBody.AddForce(direction * continuesForce, ForceMode.Force);
            yield return new WaitForFixedUpdate();
        }
    }
    /// <summary>
    /// Destroys the projectile after a time
    /// </summary>
    /// <returns></returns>
    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(SpellData.LiveTime);
        Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out HealthSystem hitEntity))
        {   
            hitEntity.TakeDamage(SpellData.Damage);
            if (SpellData.VoxelInteraction.Count != 0)
            {
                ProceedVoxelInteraction(transform.position, SpellData.EffectRadius);
            }
            Destroy(gameObject);
            return;
        }
        if(collision.gameObject.CompareTag("Voxel"))
        {
            if (SpellData.VoxelInteraction.Count != 0)
            {
                ProceedVoxelInteraction(transform.position, SpellData.EffectRadius);
            }
        }

        currentBounces++;
        if (currentBounces >= maxBounces)
        {
            Destroy(gameObject);
        }

        ContactPoint contact = collision.contacts[0];
        Vector3 normal = contact.normal;
        Vector3 reflectedVelocity = Vector3.Reflect(rigidBody.velocity, normal);
        rigidBody.velocity = reflectedVelocity;
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, rigidBody.velocity * 3);
    }
    private void ProceedVoxelInteraction(Vector3 centerWorldPosition, float radius)
    {
        int value = 0;
        Voxel voxel;
        int index;
        Vector3 localPosition;
        Vector3 currentWorldPosition;
        List<Chunk> updatedChunks = new List<Chunk>();
        int radiusInVoxels = Mathf.CeilToInt(radius / VoxelDefines.Instance.VoxelSize);
        for (int x = -radiusInVoxels; x <= radiusInVoxels; x++)
        {
            for (int y = -radiusInVoxels; y <= radiusInVoxels; y++)
            {
                for (int z = -radiusInVoxels; z <= radiusInVoxels; z++)
                {
                    currentWorldPosition = centerWorldPosition + new Vector3(x * VoxelDefines.Instance.VoxelSize, y * VoxelDefines.Instance.VoxelSize, z * VoxelDefines.Instance.VoxelSize);

                    if (Vector3.Distance(centerWorldPosition, currentWorldPosition) <= radius)
                    {
                        int3 voxelChunkKey = VoxelMethods.GetChunkKeyFromWorldPosition(currentWorldPosition);

                        if (WorldManager.Instance.GetChunk(voxelChunkKey, out Chunk chunk))
                        {
                            localPosition = (float3)currentWorldPosition - (float3)voxelChunkKey * VoxelDefines.Instance.ChunkSize * VoxelDefines.Instance.VoxelSize;
                            localPosition /= VoxelDefines.Instance.VoxelSize;

                            int3 voxelPositionInt = new int3(
                                Mathf.FloorToInt(localPosition.x),
                                Mathf.FloorToInt(localPosition.y),
                                Mathf.FloorToInt(localPosition.z)
                            );

                            index = VoxelMethods.GetIndexFromPosition(voxelPositionInt);

                            if (index >= 0 && index < chunk.VoxelArray.Length)
                            {
                                voxel = chunk.VoxelArray[index];
                                foreach (VoxelInteraction interaction in SpellData.VoxelInteraction)
                                {
                                    if (voxel.ID == interaction.TargetVoxelToReplace.ID)
                                    {
                                        value += chunk.VoxelArray[index].Value;
                                        if (interaction.ParticleSystemPrefab != null)
                                        {
                                            if (interaction.useReplacementVoxelMaterial)
                                            {
                                                chunk.VoxelArray[index] = interaction.ReplacementVoxel.Clone();
                                                VoxelParticleSystem system = Instantiate(interaction.ParticleSystemPrefab, currentWorldPosition, Quaternion.identity).GetComponent<VoxelParticleSystem>();
                                                system.SetParticleMaterial(VoxelDefines.Instance.GetVoxelObject(chunk.VoxelArray[index].ID).Material);
                                            }
                                            else
                                            {
                                                VoxelParticleSystem system = Instantiate(interaction.ParticleSystemPrefab, currentWorldPosition, Quaternion.identity).GetComponent<VoxelParticleSystem>();
                                                system.SetParticleMaterial(VoxelDefines.Instance.GetVoxelObject(chunk.VoxelArray[index].ID).Material);
                                                chunk.VoxelArray[index] = interaction.ReplacementVoxel.Clone();
                                            }
                                        }
                                        else
                                        {
                                            chunk.VoxelArray[index] = interaction.ReplacementVoxel.Clone();
                                        }
                                        if (!updatedChunks.Contains(chunk))
                                        {
                                            updatedChunks.Add(chunk);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (updatedChunks.Count > 0)
        {
            foreach (Chunk chunk in updatedChunks)
            {
                chunk.ForceGenerateMesh();
            }
        }
        InGameUI.Instance.AddScore(value);
    }
}
