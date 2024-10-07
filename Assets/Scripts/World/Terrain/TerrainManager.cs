using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages terrain chunks around the player.
/// </summary>
public class TerrainManager : MonoBehaviour
{
    [Header("Gradient Height Range")]
    [Tooltip("The minimum height that corresponds to the start of the gradient.")]
    public float gradientMinHeight = 0f;

    [Tooltip("The maximum height that corresponds to the end of the gradient.")]
    public float gradientMaxHeight = 100f;

    [Header("Player Settings")]
    public Transform player;

    [Header("Chunk Settings")]
    public int chunkSize = 32;
    public int renderDistance = 5;
    public float voxelScale = 0.75f;

    [Header("Terrain Noise Settings")]
    public int seed = 42;
    [Range(10, 100f)]
    public float heightMultiplier = 15f;
    public float noiseScale = 0.005f;
    public int octaves = 6;
    [Range(0f, 0.1f)]
    public float persistence = 0.1f;
    public float lacunarity = 5f;

    [Header("Visual Settings")]
    public Material voxelMaterial;
    public Gradient terrainGradient;

    // References
    public FoliageSpawner foliageSpawner;
    public BoulderSpawner boulderSpawner;
    public TreeSpawner treeSpawner;
    public ObjectPlacementManager placementManager;

    // Private variables
    private Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
    private Vector2Int lastPlayerChunkCoord;

    private NoiseGenerator noiseGenerator;
    private MeshGenerator meshGenerator;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player reference is not set in TerrainManager.");
            enabled = false;
            return;
        }

        // Initialize NoiseGenerator (unchanged) && MeshGenerator with static gradient heights
        noiseGenerator = new NoiseGenerator(seed, noiseScale, octaves, persistence, lacunarity, 20f, heightMultiplier);
        meshGenerator = new MeshGenerator(voxelScale, terrainGradient, gradientMinHeight, gradientMaxHeight);

        lastPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
        UpdateChunks();
    }

    void Update()
    {
        Vector2Int currentChunkCoord = GetChunkCoordFromPosition(player.position);
        if (currentChunkCoord != lastPlayerChunkCoord)
        {
            UpdateChunks();
            lastPlayerChunkCoord = currentChunkCoord;
        }
    }

    void UpdateChunks()
    {
        Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

        HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
        {
            for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + xOffset, playerChunkCoord.y + zOffset);
                activeChunks.Add(chunkCoord);

                if (!chunkDictionary.ContainsKey(chunkCoord))
                {
                    // Generate and store new chunk.
                    TerrainChunk chunk = new TerrainChunk(chunkCoord, chunkSize, voxelScale, transform, voxelMaterial);
                    GenerateChunk(chunk);
                    chunkDictionary.Add(chunkCoord, chunk);
                }
            }
        }

        // Remove chunks that are no longer within the render distance.
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in chunkDictionary)
        {
            if (!activeChunks.Contains(chunk.Key))
            {
                chunk.Value.DestroyChunk();
                chunksToRemove.Add(chunk.Key);
            }
        }
        foreach (var coord in chunksToRemove)
        {
            chunkDictionary.Remove(coord);
        }
    }

    void GenerateChunk(TerrainChunk chunk)
    {
        // Generate height map as float[,]
        float[,] heightMapFloat = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);

        // Convert float[,] heightMap to int[,]
        int[,] heightMapInt = new int[chunk.chunkSize + 1, chunk.chunkSize + 1];
        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                heightMapInt[x, z] = Mathf.RoundToInt(heightMapFloat[x, z]);
            }
        }

        // Generate mesh data using the float[,] heightMap
        MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat);

        // Update chunk mesh
        chunk.UpdateChunkMesh(meshData);

        // Spawn objects using the int[,] heightMap
        if (foliageSpawner != null)
        {
            foliageSpawner.SpawnGrass(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize);
        }

        if (boulderSpawner != null)
        {
            boulderSpawner.SpawnBoulders(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
        }

        if (treeSpawner != null)
        {
            treeSpawner.SpawnTrees(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
        }
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
        int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
        return new Vector2Int(x, z);
    }
}
