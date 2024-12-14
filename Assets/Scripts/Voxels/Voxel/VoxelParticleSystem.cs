using UnityEngine;
using Unity.Mathematics;
public class VoxelParticleSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem system;
    [SerializeField] private ParticleSystemRenderer particleRenderer; //Must be Serialized?
    [SerializeField] private float mass;
    [SerializeField] private bool interactWithForce;
    [SerializeField] private bool emitAtStart;
    [SerializeField] private bool canOverwriteMaterial;
    private void Awake()
    {
        particleRenderer = system.GetComponent<ParticleSystemRenderer>();
        if (emitAtStart)
        {
            Emit();
        }
    }
    public void Emit()
    {
        system.Emit(1);
        system.Play();
    }
    void Update()
    {
        if (WorldManager.Instance != null) return;
        InteractWithForce();
    }
    public void SetParticleMaterial(Material material)
    {
        if(canOverwriteMaterial)
        {
            particleRenderer.material = material;
        }
    }
    /// <summary>
    /// Lets the particles interact with a voxels force field
    /// </summary>
    private void InteractWithForce()
    {
        if (interactWithForce)
        {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[system.main.maxParticles];
            int numParticlesAlive = system.GetParticles(particles);

            for (int i = 0; i < numParticlesAlive; i++)
            {
                Vector3 particlePosition = particles[i].position;

                int3 chunkKey = VoxelMethods.GetChunkKeyFromWorldPosition(particlePosition);
                int3 voxelPosition = VoxelMethods.GetLocalVoxelPosition(particlePosition, chunkKey);

                int index = VoxelMethods.GetIndexFromPosition(voxelPosition);

                if (VoxelMethods.IsPositionInsideChunkBounds(index))
                {
                    if (WorldManager.Instance.GetChunk(chunkKey, out Chunk chunk))
                    {
                        Voxel voxel = chunk.VoxelArray[index];

                        if (mass < voxel.Mass)
                        {
                            particles[i].velocity = Vector3.zero;
                        }
                        else if (mass > voxel.Mass)
                        {
                            particles[i].velocity = Vector3.down;
                        }
                        particles[i].velocity += (Vector3)voxel.ForceDirection;
                    }
                }
            }

            system.SetParticles(particles, numParticlesAlive);
        }
    }
}
