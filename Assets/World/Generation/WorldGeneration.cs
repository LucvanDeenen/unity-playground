using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

public class WorldGeneration : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;

    [Header("Noise Settings")]
    [Range(0f, 1f)]
    public float noiseScale = 0.2f;

    [Range(10, 100)]
    public int worldSize = 100; // Total size of the world in blocks

    [Header("Chunk Settings")]
    public int chunkSize = 16; // Size of each chunk in blocks
    public float threshold = 0.5f; // Threshold for block placement

    [Header("Generation Settings")]
    public bool regenerateOnChange = true; // Toggle for regeneration on parameter change
    public int renderDistance = 4; // Number of chunks to load around the player

    [Header("Player Settings")]
    public Transform playerTransform; // Reference to the player

    // Tracking previous values to detect changes
    private float previousNoiseScale;
    private int previousWorldSize;
    private int previousChunkSize;
    private float previousThreshold;
    private int previousRenderDistance;

    // Thread-safe collection for completed chunks
    private ConcurrentQueue<ChunkData> completedChunks = new ConcurrentQueue<ChunkData>();

    // Dictionary to manage active chunks
    private Dictionary<Vector3Int, Chunk> activeChunks = new Dictionary<Vector3Int, Chunk>();

    // Cancellation token to handle task cancellation during regeneration
    private CancellationTokenSource cancellationTokenSource;

    // Object Pool for chunks
    private ObjectPool<Chunk> chunkPool;

    void Start()
    {
        chunkPool = new ObjectPool<Chunk>();
        InitializeGeneration();
    }

    void Update()
    {
        // Detect changes in parameters
        if (regenerateOnChange &&
            (Mathf.Abs(noiseScale - previousNoiseScale) > Mathf.Epsilon ||
             worldSize != previousWorldSize ||
             chunkSize != previousChunkSize ||
             Mathf.Abs(threshold - previousThreshold) > Mathf.Epsilon ||
             renderDistance != previousRenderDistance))
        {
            InitializeGeneration();
        }

        // Load and unload chunks based on player position
        UpdateChunks();

        // Process completed chunks and instantiate their meshes
        ProcessCompletedChunks();
    }

    /// <summary>
    /// Initializes the world generation by cancelling ongoing tasks and starting new ones.
    /// </summary>
    private void InitializeGeneration()
    {
        // Update previous values
        previousNoiseScale = noiseScale;
        previousWorldSize = worldSize;
        previousChunkSize = chunkSize;
        previousThreshold = threshold;
        previousRenderDistance = renderDistance;

        // Cancel any ongoing generation tasks
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = null;
        }

        // Clear existing chunks
        ClearWorld();

        // Initialize new cancellation token
        cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = cancellationTokenSource.Token;

        // Start generating initial chunks around the player
        UpdateChunks();
    }

    /// <summary>
    /// Updates the loaded chunks based on the player's current position.
    /// </summary>
    private void UpdateChunks()
    {
        Vector3 playerPosition = playerTransform.position;
        Vector3Int playerChunkCoord = WorldToChunkCoord(playerPosition);

        // Determine the range of chunks to load
        for (int x = playerChunkCoord.x - renderDistance; x <= playerChunkCoord.x + renderDistance; x++)
        {
            for (int y = playerChunkCoord.y - renderDistance; y <= playerChunkCoord.y + renderDistance; y++)
            {
                for (int z = playerChunkCoord.z - renderDistance; z <= playerChunkCoord.z + renderDistance; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    if (!activeChunks.ContainsKey(chunkCoord) && IsWithinWorldBounds(chunkCoord))
                    {
                        LoadChunkAsync(chunkCoord);
                    }
                }
            }
        }

        // Determine chunks to unload
        List<Vector3Int> chunksToUnload = new List<Vector3Int>();
        foreach (var chunk in activeChunks.Keys)
        {
            if (Mathf.Abs(chunk.x - playerChunkCoord.x) > renderDistance ||
                Mathf.Abs(chunk.y - playerChunkCoord.y) > renderDistance ||
                Mathf.Abs(chunk.z - playerChunkCoord.z) > renderDistance)
            {
                chunksToUnload.Add(chunk);
            }
        }

        // Unload distant chunks
        foreach (var chunkCoord in chunksToUnload)
        {
            UnloadChunk(chunkCoord);
        }
    }

    /// <summary>
    /// Converts world position to chunk coordinates.
    /// </summary>
    private Vector3Int WorldToChunkCoord(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / chunkSize),
            Mathf.FloorToInt(position.y / chunkSize),
            Mathf.FloorToInt(position.z / chunkSize)
        );
    }

    /// <summary>
    /// Determines if a chunk is within the world bounds.
    /// </summary>
    private bool IsWithinWorldBounds(Vector3Int chunkCoord)
    {
        Vector3Int min = new Vector3Int(0, 0, 0);
        Vector3Int max = new Vector3Int(
            Mathf.CeilToInt((float)worldSize / chunkSize),
            Mathf.CeilToInt((float)worldSize / chunkSize),
            Mathf.CeilToInt((float)worldSize / chunkSize)
        );

        return chunkCoord.x >= min.x && chunkCoord.x < max.x &&
               chunkCoord.y >= min.y && chunkCoord.y < max.y &&
               chunkCoord.z >= min.z && chunkCoord.z < max.z;
    }

    /// <summary>
    /// Asynchronously loads a chunk.
    /// </summary>
    private async void LoadChunkAsync(Vector3Int chunkCoord)
    {
        try
        {
            // Retrieve a chunk from the pool or create a new one
            Chunk chunk = chunkPool.GetObject();
            if (chunk == null)
            {
                GameObject chunkGO = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
                chunkGO.transform.parent = this.transform;
                MeshFilter meshFilter = chunkGO.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = chunkGO.AddComponent<MeshRenderer>();
                meshRenderer.material = blockPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                chunk = new Chunk(chunkCoord, meshFilter, meshRenderer);
            }

            activeChunks.Add(chunkCoord, chunk);

            // Generate chunk data on a background thread
            List<Vector3Int> blockPositions = await Task.Run(() => GenerateChunkData(chunkCoord, cancellationTokenSource.Token), cancellationTokenSource.Token);

            if (cancellationTokenSource.Token.IsCancellationRequested)
                return;

            // Enqueue the completed chunk data for mesh generation
            completedChunks.Enqueue(new ChunkData { Chunk = chunk, BlockPositions = blockPositions });
        }
        catch (OperationCanceledException)
        {
            // Task was cancelled, do nothing
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading chunk {chunkCoord}: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates block positions for a chunk based on Perlin noise.
    /// </summary>
    private List<Vector3Int> GenerateChunkData(Vector3Int chunkCoord, CancellationToken token)
    {
        List<Vector3Int> blockPositions = new List<Vector3Int>();

        Vector3 chunkWorldPos = new Vector3(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize, chunkCoord.z * chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (token.IsCancellationRequested)
                        return blockPositions;

                    float noiseValue = Perlin3D((chunkWorldPos.x + x) * noiseScale,
                                               (chunkWorldPos.y + y) * noiseScale,
                                               (chunkWorldPos.z + z) * noiseScale);
                    if (noiseValue >= threshold)
                    {
                        blockPositions.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        return blockPositions;
    }

    /// <summary>
    /// Processes completed chunks by generating their meshes.
    /// </summary>
    private void ProcessCompletedChunks()
    {
        while (completedChunks.TryDequeue(out ChunkData chunkData))
        {
            // Generate mesh on the main thread
            chunkData.Chunk.GenerateMesh(chunkData.BlockPositions, noiseScale, threshold);
            chunkData.Chunk.ApplyMesh();
        }
    }

    /// <summary>
    /// Unloads a chunk and returns it to the pool.
    /// </summary>
    private void UnloadChunk(Vector3Int chunkCoord)
    {
        if (activeChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            activeChunks.Remove(chunkCoord);
            // Destroy the chunk GameObject or return it to the pool
            chunkPool.ReturnObject(chunk);
        }
    }

    /// <summary>
    /// Clears all loaded chunks.
    /// </summary>
    private void ClearWorld()
    {
        foreach (var chunk in activeChunks.Values)
        {
            chunkPool.ReturnObject(chunk);
        }
        activeChunks.Clear();
    }

    /// <summary>
    /// Approximates 3D Perlin Noise by averaging multiple 2D noise samples.
    /// </summary>
    public static float Perlin3D(float x, float y, float z)
    {
        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);

        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        float ABC = AB + BC + AC + BA + CB + CA;
        return ABC / 6f;
    }

    /// <summary>
    /// Draws a wireframe around the entire world for visualization in the Editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (chunkSize <= 0 || worldSize <= 0)
            return;

        Gizmos.color = Color.gray;
        Vector3 worldDimensions = new Vector3(worldSize, worldSize, worldSize);
        Gizmos.DrawWireCube(transform.position + worldDimensions / 2f, worldDimensions);
    }

    /// <summary>
    /// Data structure to hold completed chunk data.
    /// </summary>
    private struct ChunkData
    {
        public Chunk Chunk;
        public List<Vector3Int> BlockPositions;
    }
}
