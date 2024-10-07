using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages terrain chunks around the player.
/// </summary>
public class TerrainManager : MonoBehaviour
{

    [Header("Player Settings")]
    public Transform player;

    [Header("Terrain Settings")]
    public int chunkSize = 32;
    public int seed = 42;
    public int renderDistance = 5;
    public Material voxelMaterial;
    public Gradient terrainGradient;

    [Header("Spawners")]
    public List<Spawner> spawners = new List<Spawner>();

    private Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
    private Vector2Int lastPlayerChunkCoord;
    private float voxelScale;

    private ObjectPlacementManager placementManager;
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
        placementManager = transform.GetComponent<ObjectPlacementManager>();
        noiseGenerator = new NoiseGenerator(seed);
        meshGenerator = new MeshGenerator(voxelScale, terrainGradient);
        foreach (Spawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.SetPlacementManager(placementManager);
            }
        }

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
        foreach (Spawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.Spawn(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
            }
        }
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
        int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
        return new Vector2Int(x, z);
    }
}
