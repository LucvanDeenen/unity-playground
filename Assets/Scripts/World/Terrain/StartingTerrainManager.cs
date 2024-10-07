using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages terrain chunks around the player.
/// </summary>
public class StartingTerrainManager : MonoBehaviour
{

    [Header("Player Settings")]
    public Transform player;

    [Header("Terrain Settings")]
    public int seed = 42;
    public Material voxelMaterial;
    public Gradient terrainGradient;
    public Color wallColor = new Color(0.5f, 0.3f, 0.2f); // Grey/Brownish color for walls

    [Header("Spawners")]
    public List<Spawner> spawners = new List<Spawner>();

    private Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
    private Vector2Int lastPlayerChunkCoord;

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

        placementManager = GetComponent<ObjectPlacementManager>();

        // Initialize NoiseGenerator and MeshGenerator with parameters
        noiseGenerator = new NoiseGenerator(seed);
        meshGenerator = new MeshGenerator(voxelScale, terrainGradient, wallColor);

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

        // Initialize the isCliffArea array
        bool[,] isCliffArea = new bool[chunk.chunkSize + 1, chunk.chunkSize + 1];

        // Adjust heights to create cliffs around lowered area and smooth terrain at the bottom
        float lowerRadius = 35f;      // Radius of lowered area
        float cliffWidth = 2f;        // Width of the cliff (transition area)
        float heightOffset = 20f;     // Height offset for the lowered area

        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                // Compute world position
                float worldX = (chunk.chunkCoord.x * chunk.chunkSize + x) * voxelScale;
                float worldZ = (chunk.chunkCoord.y * chunk.chunkSize + z) * voxelScale;

                float dx = worldX - player.position.x;
                float dz = worldZ - player.position.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance <= lowerRadius)
                {
                    float smoothNoise = noiseGenerator.GenerateSmoothNoise(worldX, worldZ);
                    heightMapFloat[x, z] = smoothNoise + heightOffset;
                    isCliffArea[x, z] = false; // Not a cliff area
                }
                else if (distance <= lowerRadius + cliffWidth)
                {
                    // Create a steep transition (cliff)
                    float t = (distance - lowerRadius) / cliffWidth;
                    float loweredHeight = noiseGenerator.GenerateSmoothNoise(worldX, worldZ) + heightOffset;
                    heightMapFloat[x, z] = Mathf.Lerp(loweredHeight, heightMapFloat[x, z], t);
                    isCliffArea[x, z] = true; // Mark as cliff area
                }
                else
                {
                    isCliffArea[x, z] = false; // Not a cliff area
                    // Keep the original height
                }
            }
        }

        // Convert float[,] heightMap to int[,]
        int[,] heightMapInt = new int[chunk.chunkSize + 1, chunk.chunkSize + 1];
        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                heightMapInt[x, z] = Mathf.RoundToInt(heightMapFloat[x, z]);
            }
        }

        // Generate mesh data using the adjusted heightMap and isCliffArea
        MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat, isCliffArea);

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
