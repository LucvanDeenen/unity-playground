using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages terrain chunks around the starting player.
/// </summary>
public class StartingTerrainManager : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;

    [Header("Terrain Settings")]
    public int seed = 42;
    public Material voxelMaterial;
    public Gradient terrainGradient;

    [Header("Spawners")]
    public List<Spawner> spawners = new List<Spawner>();

    private Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
    private Vector3 spawnPoint;

    private float voxelScale = 0.75f;
    private int renderDistance = 2;
    private int chunkSize = 32;

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

        // Set placement manager
        placementManager = GetComponent<ObjectPlacementManager>();

        // Store the player's initial spawn position
        spawnPoint = player.position;

        // Initialize NoiseGenerator and MeshGenerator with parameters
        noiseGenerator = new NoiseGenerator(seed);
        meshGenerator = new MeshGenerator(voxelScale, terrainGradient);

        foreach (Spawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.SetPlacementManager(placementManager);
            }
        }
        UpdateChunks();
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
                    TerrainChunk chunk = new TerrainChunk(chunkCoord, chunkSize, voxelScale, transform, voxelMaterial);
                    GenerateChunk(chunk);
                    chunkDictionary.Add(chunkCoord, chunk);
                }
            }
        }

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
        float[,] heightMap = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);
        float valleyRadius = 30f;
        float valleyDepth = 20f;

        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                Vector3 worldPos = new Vector3(chunk.chunkCoord.x * chunk.chunkSize + x, 0, chunk.chunkCoord.y * chunk.chunkSize + z) * voxelScale;
                float distanceToPlayer = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(player.position.x, player.position.z));

                if (distanceToPlayer <= valleyRadius)
                {
                    heightMap[x, z] = Mathf.Lerp(heightMap[x, z], valleyDepth, 1f - (distanceToPlayer / valleyRadius));
                }
                else if (distanceToPlayer <= valleyRadius + 3)
                {
                    heightMap[x, z] = Mathf.Lerp(heightMap[x, z], valleyDepth, 1f - ((distanceToPlayer - valleyRadius) / 3f));
                }
            }
        }

        MeshData meshData = meshGenerator.GenerateMeshData(heightMap);
        chunk.UpdateChunkMesh(meshData);
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
        int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
        return new Vector2Int(x, z);
    }
}