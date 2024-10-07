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

        // Modify the height map to increase terrain height around the player's spawn point
        CarveValleyInHeightMap(heightMapFloat, chunk);

        // Convert float[,] heightMap to int[,] for spawners
        int[,] heightMapInt = new int[chunk.chunkSize + 1, chunk.chunkSize + 1];
        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                heightMapInt[x, z] = Mathf.RoundToInt(heightMapFloat[x, z]);
            }
        }

        // Generate mesh data using the modified float[,] heightMap
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
    private void CarveValleyInHeightMap(float[,] heightMap, TerrainChunk chunk)
    {
        // Define the radius of the valley
        float valleyRadius = 30f; // Adjust as needed
        float valleyDepth = 20f; // Depth of the valley

        // Get the center position (player's spawn point projected onto XZ plane)
        Vector2 centerPosition = new Vector2(spawnPoint.x, spawnPoint.z);

        // Get the chunk's world position
        Vector3 chunkPosition = chunk.chunkObject.transform.position;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Calculate the world position of the current point
                float worldX = chunkPosition.x + (x * voxelScale);
                float worldZ = chunkPosition.z + (z * voxelScale);

                Vector2 pointPosition = new Vector2(worldX, worldZ);

                // Calculate the distance from the point to the center position
                float distance = Vector2.Distance(pointPosition, centerPosition);

                if (distance <= valleyRadius)
                {
                    // Calculate normalized distance from the center
                    float t = distance / valleyRadius;

                    // Use a quadratic function for steep walls
                    float depthFactor = Mathf.Pow(t, 2f); // Adjust exponent for steepness

                    // Lower the height to create the valley
                    heightMap[x, z] -= valleyDepth * (1f - depthFactor);
                }
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
