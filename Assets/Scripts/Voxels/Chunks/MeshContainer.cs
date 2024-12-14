using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshContainer : MonoBehaviour
{
    [SerializeField] private MeshCollider meshCollider;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    /// <summary>
    /// Deactivates the visuals and collisions
    /// </summary>
    public void Deactivate()
    {
        meshRenderer.enabled = false;
        meshCollider.enabled = false;
        gameObject.SetActive(false);
    }
    /// <summary>
    /// Sets the visibiliry
    /// </summary>
    /// <param name="state"></param>
    public void SetVisibility(bool state)
    {
        meshRenderer.enabled = state;
    }
    /// <summary>
    /// Overwrite the meshrenderer material
    /// </summary>
    /// <param name="material"></param>
    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }
    /// <summary>
    /// Overwrite the meshfilters mesh
    /// </summary>
    /// <param name="material"></param>
    public void SetMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;
    }
    /// <summary>
    /// Overwrite the meshColliders shaders mesh
    /// </summary>
    /// <param name="material"></param>
    public void SetCollider(Mesh mesh)
    {
        meshCollider.sharedMesh = mesh;
    }
    /// <summary>
    /// sets meshes to null
    /// </summary>
    /// <param name="material"></param>
    public void Clear()
    {
        meshFilter.mesh = null;
        meshCollider.sharedMesh = null;
    }
}
