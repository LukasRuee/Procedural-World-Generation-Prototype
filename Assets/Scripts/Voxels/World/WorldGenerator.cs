using MyBox;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Serializable]
    public struct NoiseSettings
    {
        [field: SerializeField] public float Frequency { get; private set; }
        [field: SerializeField] public float Threshold { get; private set; }
        public int ID { get; private set; }
        /// <summary>
        /// Sets the ID
        /// </summary>
        /// <param name="ID"></param>
        public void SetID(int ID)
        {
            this.ID = ID;
        }
    }

    [Serializable]
    public struct GenerationSetting
    {
        [SerializeField, TextArea] private string description;
        [field: SerializeField] public NoiseSettings NoiseSettings;
        [field: SerializeField] public VoxelObject Voxel { get; private set; }
        public int[] Random { get; private set; }
        [field: SerializeField] public bool GenerateThisSetting { get; private set; }
        /// <summary>
        /// Generates a random IDs for generation
        /// </summary>
        /// <param name="size"></param>
        public void Randomize(int size)
        {
            Random = new int[size];

            List<int> randIds = new List<int>();
            for (int i = 0; i < size / 2; i++)
            {
                randIds.Add(i);
            }
            for (int i = 0; i < size / 2; i++)
            {
                int id = UnityEngine.Random.Range(0, randIds.Count);
                Random[randIds[id]] = i;
                Random[randIds[id] + size / 2] = i;
                randIds.RemoveAt(id);
            }
        }
    }

    [SerializeField] private VoxelObject baseVoxel;
    [SerializeField] private VoxelObject emptyVoxel;
    [SerializeField] private GenerationSetting[] settings;
    [SerializeField] private int playerStartCaveSize;
    private GenerationSetting[] settingsToGenerate;

    [SerializeField, Min(1)] private int seed = 1;
    private int randomizeSize = 512;

    [SerializeField] private ComputeShader PerlinShader;
    private ComputeBuffer randomBuffer;
    private ComputeBuffer chunkSizeBuffer;
    private ComputeBuffer startXYZBuffer;
    private ComputeBuffer voxelMapBuffer;
    private ComputeBuffer settingBuffer;

    private Voxel[] prefilledArray;
    public static WorldGenerator Instance { get; private set; }
    private void Awake()
    {
        UnityEngine.Random.InitState(seed);
        if (Instance == null)
        {
            Instance = this;
        }
    }
    private void Start()
    {
        for (int i = 0; i < settings.Length; i++)
        {
            NoiseSettings temp = settings[i].NoiseSettings;
            temp.SetID(settings[i].Voxel.ID);
            settings[i].NoiseSettings = temp;
        }
        int counter = 0;
        foreach (GenerationSetting setting in settings)
        {
            if (setting.GenerateThisSetting)
            {
                counter++;
            }
        }
        settingsToGenerate = new GenerationSetting[counter];
        counter = 0;
        for(int i = 0; i < settings.Length; i++)
        {
            if (settings[i].GenerateThisSetting)
            {
                settingsToGenerate[counter] = settings[i];
                counter++;
            }
        }
        PrefillArrayPrefab();
        SetUpBuffers();
        SetRamdomData();
        SetSettingData();
    }
    /// <summary>
    /// Prefills a array with the base voxel
    /// </summary>
    private void PrefillArrayPrefab()
    {
        prefilledArray = new Voxel[VoxelDefines.Instance.TotalVoxelsPerChunk];
        Voxel voxel = VoxelDefines.Instance.GetVoxel(baseVoxel.ID);
        for (int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            prefilledArray[i] = voxel;
        }
    }
    /// <summary>
    /// Sets up the ComputeBuffer
    /// </summary>
    private void SetUpBuffers()
    {
        randomBuffer = new ComputeBuffer(settingsToGenerate.Length * randomizeSize, sizeof(int));
        startXYZBuffer = new ComputeBuffer(3, sizeof(float));
        voxelMapBuffer = new ComputeBuffer(VoxelDefines.Instance.TotalVoxelsPerChunk, sizeof(int));
        settingBuffer = new ComputeBuffer(settingsToGenerate.Length, 2 * sizeof(float) + sizeof(int));
        chunkSizeBuffer = new ComputeBuffer(1, sizeof(int));

        int[] chunkSizeArray = new int[1];
        chunkSizeArray[0] = VoxelDefines.Instance.ChunkSize;
        chunkSizeBuffer.SetData(chunkSizeArray);
        PerlinShader.SetBuffer(0, "chunkSize", chunkSizeBuffer);
    }
    private void SetSettingData()
    {
        NoiseSettings[] settingArray = new NoiseSettings[settingsToGenerate.Length];
        for (int i = 0; i < settingsToGenerate.Length; i++)
        {
            settingArray[i] = settingsToGenerate[i].NoiseSettings;
        }
        settingBuffer.SetData(settingArray);
        PerlinShader.SetBuffer(0, "settings", settingBuffer);
    }
    /// <summary>
    /// Initialize the random data
    /// </summary>
    private void SetRamdomData()
    {
        int[] randoms = new int[settingsToGenerate.Length * randomizeSize];
        for (int settingCount = 0; settingCount < settingsToGenerate.Length; settingCount++)
        {
            settingsToGenerate[settingCount].Randomize(randomizeSize);
            for (int i = 0; i < randomizeSize; i++)
            {
                randoms[settingCount * randomizeSize + i] = settingsToGenerate[settingCount].Random[i];
            }
        }
        randomBuffer.SetData(randoms);
        PerlinShader.SetBuffer(0, "random", randomBuffer);
    }
    /// <summary>
    /// Spawn a new chunk
    /// </summary>
    /// <param name="key"></param>
    /// <param name="newChunk"></param>
    public void SpawnChunk(int3 key, Chunk newChunk)
    {
        int[] data = new int[VoxelDefines.Instance.TotalVoxelsPerChunk];
        for(int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            data[i] = baseVoxel.ID;
        }
        voxelMapBuffer.SetData(data);

        int3 offset = key * VoxelDefines.Instance.ChunkSize;

        int[] xyz = new int[3];
        xyz[0] = offset.x;
        xyz[1] = offset.y;
        xyz[2] = offset.z;
        startXYZBuffer.SetData(xyz);

        PerlinShader.SetBuffer(0, "voxelMap", voxelMapBuffer);
        PerlinShader.SetBuffer(0, "startXYZ", startXYZBuffer);

        int threadGroupSize = 8;
        int dataSize = VoxelDefines.Instance.ChunkSize;

        int threadGroup = (dataSize + threadGroupSize - 1) / threadGroupSize;

        PerlinShader.Dispatch(0, threadGroup, threadGroup, threadGroup);
        voxelMapBuffer.GetData(data);

        Voxel[] voxelArray = new Voxel[VoxelDefines.Instance.TotalVoxelsPerChunk];
        prefilledArray.CopyTo(voxelArray, 0);


        for(int i = 0; i < VoxelDefines.Instance.TotalVoxelsPerChunk; i++)
        {
            foreach (GenerationSetting setting in settingsToGenerate)
            {
                if (data[i] == setting.Voxel.ID)
                {
                    VoxelObject newVoxel = setting.Voxel;
                    voxelArray[i] = setting.Voxel.Clone();
                }
            }
        }

        if(key.Equals(new int3(0,0,0)))
        {
            CarveHoleInMiddle(voxelArray, key, playerStartCaveSize);
        }

        newChunk.SetUpMeshes();
        newChunk.Initialize(key, voxelArray);
    }
    /// <summary>
    /// Generate a hole where the player spawns
    /// </summary>
    /// <param name="array"></param>
    /// <param name="coord"></param>
    /// <param name="radius"></param>
    public void CarveHoleInMiddle(Voxel[] array, int3 coord, int radius)
    {
        int3 center = new int3(VoxelDefines.Instance.ChunkSize / 2, VoxelDefines.Instance.ChunkSize / 2, VoxelDefines.Instance.ChunkSize / 2);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    int3 carvePosition = center + new int3(x, y, z);
                    int distanceSquared = x * x + y * y + z * z;
                    if (distanceSquared <= radius * radius)
                    {
                        int index = VoxelMethods.GetIndexFromPosition(carvePosition);
                        if (VoxelMethods.IsIndexInsideChunkBounds(index))
                        {
                            array[index] = emptyVoxel.Clone();
                        }
                    }
                }
            }
        }
    }
    private void OnDestroy()
    {
        if (randomBuffer != null)
        {
            randomBuffer.Release();
        }
        if (startXYZBuffer != null)
        {
            startXYZBuffer.Release();
        }
        if (settingBuffer != null)
        {
            settingBuffer.Release();
        }
        if (chunkSizeBuffer != null)
        {
            chunkSizeBuffer.Release();
        }
        if (voxelMapBuffer != null)
        {
            voxelMapBuffer.Release();
        }
    }
}