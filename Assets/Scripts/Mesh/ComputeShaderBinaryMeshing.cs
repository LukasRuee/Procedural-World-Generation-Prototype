using UnityEngine;

public class ComputeShaderBinaryMeshing : MonoBehaviour
{
    [SerializeField] private ComputeShader MeshShader;
    private ComputeBuffer voxelBuff;
    private ComputeBuffer facesBuff;
    private ComputeBuffer chunkSizeBuffer;
    private int[] faces;

    public static ComputeShaderBinaryMeshing Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }
    private void Start()
    {
        voxelBuff = new ComputeBuffer(VoxelDefines.Instance.TotalVoxelsPerChunk, sizeof(int));
        facesBuff = new ComputeBuffer(VoxelDefines.Instance.TotalVoxelsPerChunk, sizeof(int));
        chunkSizeBuffer = new ComputeBuffer(1, sizeof(float));

        faces = new int[VoxelDefines.Instance.TotalVoxelsPerChunk];

        int[] chunkSizeArray = new int[1];
        chunkSizeArray[0] = VoxelDefines.Instance.ChunkSize;
        chunkSizeBuffer.SetData(chunkSizeArray);
        MeshShader.SetBuffer(0, "chunkSize", chunkSizeBuffer);
    }
    /// <summary>
    /// Updates the data of a mesh
    /// </summary>
    /// <param name="dataArray"></param>
    /// <param name="mesh"></param>
    public void UpdateMesh(int[] dataArray, Mesh mesh)
    {
        voxelBuff.SetData(dataArray);

        int GetFacesKer = MeshShader.FindKernel("GetFacesList");
        int GetVertsKer = MeshShader.FindKernel("GetVerts");

        MeshShader.SetBuffer(GetFacesKer, "voxels", voxelBuff);
        MeshShader.SetBuffer(GetFacesKer, "faces", facesBuff);
        MeshShader.Dispatch(GetFacesKer, VoxelDefines.Instance.ChunkSize / 8, VoxelDefines.Instance.ChunkSize / 8, VoxelDefines.Instance.ChunkSize / 8);

        facesBuff.GetData(faces);
        for (int i = 1; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            faces[i] += faces[i - 1];
        }
        if (faces[VoxelDefines.Instance.TotalVoxelsPerChunk - 1] != 0)
        {
            facesBuff.SetData(faces);

            ComputeBuffer vertsBuffer = new ComputeBuffer(faces[VoxelDefines.Instance.TotalVoxelsPerChunk - 1] * 4, sizeof(float) * 3);
            ComputeBuffer normalBuffer = new ComputeBuffer(faces[VoxelDefines.Instance.TotalVoxelsPerChunk - 1] * 4, sizeof(float) * 3);
            ComputeBuffer uvBuffer = new ComputeBuffer(faces[VoxelDefines.Instance.TotalVoxelsPerChunk - 1] * 4, sizeof(float) * 2);

            MeshShader.SetBuffer(GetVertsKer, "voxels", voxelBuff);
            MeshShader.SetBuffer(GetVertsKer, "faces", facesBuff);
            MeshShader.SetBuffer(GetVertsKer, "verts", vertsBuffer);
            MeshShader.SetBuffer(GetVertsKer, "normals", normalBuffer);
            MeshShader.SetBuffer(GetVertsKer, "uvs", uvBuffer);
            MeshShader.SetBuffer(GetVertsKer, "chunkSize", chunkSizeBuffer);

            MeshShader.Dispatch(GetVertsKer, VoxelDefines.Instance.ChunkSize / 8, VoxelDefines.Instance.ChunkSize / 8, VoxelDefines.Instance.ChunkSize / 8);

            int maxFacesCount = faces[VoxelDefines.Instance.TotalVoxelsPerChunk - 1];

            Vector3[] verts = new Vector3[maxFacesCount * 4];
            Vector3[] normals = new Vector3[maxFacesCount * 4];
            Vector2[] uvs = new Vector2[maxFacesCount * 4];
            int[] tris = new int[maxFacesCount * 6];
            int trisCount = 0;

            for (int i = 0; i < maxFacesCount * 6; i += 6)
            {
                tris[i + 0] = 0 + trisCount;
                tris[i + 1] = 1 + trisCount;
                tris[i + 2] = 2 + trisCount;
                tris[i + 3] = 0 + trisCount;
                tris[i + 4] = 2 + trisCount;
                tris[i + 5] = 3 + trisCount;
                trisCount += 4;
            }

            vertsBuffer.GetData(verts);
            normalBuffer.GetData(normals);
            uvBuffer.GetData(uvs);

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] *= VoxelDefines.Instance.VoxelSize;
            }

            mesh.Clear(false);
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            mesh.Optimize();

            vertsBuffer.Release();
            normalBuffer.Release();
            uvBuffer.Release();
        }
        else
        {
            mesh.Clear(false);
        }
    }
    private void OnDestroy()
    {
        if (chunkSizeBuffer != null)
        {
            chunkSizeBuffer.Release();
        }
        if (facesBuff != null)
        {
            facesBuff.Release();
        }
        if (voxelBuff != null)
        {
            voxelBuff.Release();
        }
    }
}
