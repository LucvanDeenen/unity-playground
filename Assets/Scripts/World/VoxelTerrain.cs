using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates voxel-based terrain using Perlin noise and manages chunk loading around the player.
/// </summary>
public class VoxelTerrain : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("The player Transform reference. This is used to track the player's position and load chunks around the player.")]
    [SerializeField] private Transform player;

    [Header("Chunk Settings")]
    [Tooltip("Defines the size of each chunk in terms of the number of voxels per edge. Larger values create bigger chunks with more voxels.")]
    [SerializeField] private int chunkSize = 32;

    [Tooltip("The radius around the player within which chunks are generated. This is measured in chunks. Higher values increase the render distance but may affect performance.")]
    [SerializeField] private int renderDistance = 5;

    [Tooltip("The physical size of each voxel block. Smaller values create more detailed terrain, while larger values result in larger blocks.")]
    [SerializeField] private float voxelScale = 0.75f;

    [Header("Terrain Noise Settings")]
    [Tooltip("The seed value for generating consistent terrain noise. Changing the seed will result in different terrain being generated.")]
    [SerializeField] private int seed = 42;

    [Tooltip("Multiplier applied to the height of the terrain. Higher values result in taller mountains and deeper valleys.")]
    [Range(10, 100f)]
    [SerializeField] private float heightMultiplier = 15f;

    [Tooltip("Controls the 'zoom' level of the Perlin noise. Smaller values create more gradual terrain variation, while larger values create sharper terrain features.")]
    [SerializeField] private float noiseScale = 0.005f;

    [Tooltip("The number of noise layers used to add detail to the terrain. More octaves add more fine details but can increase computation time.")]
    [SerializeField] private int octaves = 6;

    [Tooltip("Determines how much the amplitude decreases for each successive octave. A lower value means less variation in higher octaves, resulting in smoother terrain.")]
    [Range(0f, 0.1f)]
    [SerializeField] private float persistence = 0.1f;

    [Tooltip("Determines how much the frequency increases for each successive octave. Higher values result in more frequent variations, creating rougher terrain.")]
    [SerializeField] private float lacunarity = 5f;

    [Header("Visual Settings")]
    [Tooltip("The material applied to voxel blocks to define their appearance. This could include textures, colors, or shader properties.")]
    [SerializeField] private Material voxelMaterial;

    // Dictionary to keep track of generated chunks using their coordinates as keys.
    private Dictionary<Vector2Int, GameObject> chunkDictionary = new Dictionary<Vector2Int, GameObject>();

    // Workaround for preventing issues with rendering
    private float baseHeight = 20f;

    // Stores the last player chunk position to determine when to update chunks.
    private Vector2Int lastPlayerChunkCoord;

    /// <summary>
    /// Initializes the terrain by generating initial chunks around the player.
    /// </summary>
    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player reference is not set in VoxelTerrain.");
            enabled = false;
            return;
        }

        lastPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
        UpdateChunks();
    }

    /// <summary>
    /// Updates the terrain chunks based on the player's position when the player moves to a new chunk.
    /// </summary>
    void Update()
    {
        Vector2Int currentChunkCoord = GetChunkCoordFromPosition(player.position);
        if (currentChunkCoord != lastPlayerChunkCoord)
        {
            UpdateChunks();
            lastPlayerChunkCoord = currentChunkCoord;
        }
    }

    /// <summary>
    /// Updates the chunks around the player by generating new ones and removing distant ones.
    /// </summary>
    void UpdateChunks()
    {
        Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

        // HashSet for efficient lookup of active chunks.
        HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        // Determine the range of chunks to be generated around the player.
        for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
        {
            for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + xOffset, playerChunkCoord.y + zOffset);
                activeChunks.Add(chunkCoord);

                if (!chunkDictionary.ContainsKey(chunkCoord))
                {
                    // Generate and store new chunk.
                    GameObject chunkObject = GenerateChunk(chunkCoord);
                    chunkDictionary.Add(chunkCoord, chunkObject);
                }
            }
        }

        // Remove chunks that are no longer within the render distance.
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in chunkDictionary)
        {
            if (!activeChunks.Contains(chunk.Key))
            {
                Destroy(chunk.Value);
                chunksToRemove.Add(chunk.Key);
            }
        }
        foreach (var coord in chunksToRemove)
        {
            chunkDictionary.Remove(coord);
        }
    }

    /// <summary>
    /// Generates a chunk at the specified coordinate.
    /// </summary>
    /// <param name="chunkCoord">The coordinate of the chunk to generate.</param>
    /// <returns>The generated chunk GameObject.</returns>
    GameObject GenerateChunk(Vector2Int chunkCoord)
    {
        // Create a new GameObject for the chunk.
        GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObject.transform.parent = transform;
        Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize) * voxelScale;
        chunkObject.transform.position = chunkPosition;

        // Set the ground tag and layer (ensure "Ground" layer exists in your project).
        chunkObject.tag = "Ground";
        chunkObject.layer = LayerMask.NameToLayer("Ground");

        // Generate mesh data for the chunk.
        MeshData meshData = GenerateChunkMesh(chunkCoord);

        // Create mesh from mesh data.
        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh
        {
            vertices = meshData.vertices.ToArray(),
            triangles = meshData.triangles.ToArray(),
            uv = meshData.uvs.ToArray()
        };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = voxelMaterial;  // Assign the voxel material.

        // Optional: Add Mesh Collider.
        MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        return chunkObject;
    }

    /// <summary>
    /// Generates the mesh data for a chunk at the specified coordinate.
    /// </summary>
    /// <param name="chunkCoord">The coordinate of the chunk.</param>
    /// <returns>The generated mesh data.</returns>
    MeshData GenerateChunkMesh(Vector2Int chunkCoord)
    {
        MeshData meshData = new MeshData();
        int[,] heightMap = new int[chunkSize + 1, chunkSize + 1];

        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000);
        float offsetZ = prng.Next(-100000, 100000);

        // Generate height map for the chunk.
        for (int x = 0; x <= chunkSize; x++)
        {
            for (int z = 0; z <= chunkSize; z++)
            {
                int worldX = chunkCoord.x * chunkSize + x;
                int worldZ = chunkCoord.y * chunkSize + z;

                // Calculate height using fractal noise (multiple octaves).
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (worldX + offsetX) * noiseScale * frequency;
                    float sampleZ = (worldZ + offsetZ) * noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // Adjusted to range [-1, 1].
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Apply height multiplier and add baseHeight.
                float height = noiseHeight * heightMultiplier + baseHeight;

                heightMap[x, z] = Mathf.RoundToInt(height);
            }
        }

        // Generate mesh data based on the height map.
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int columnHeight = heightMap[x, z];

                int startY = Mathf.Min(0, columnHeight);
                int endY = Mathf.Max(0, columnHeight);

                for (int y = startY; y <= endY; y++)
                {
                    Vector3 blockPosition = new Vector3(x, y, z) * voxelScale;

                    // Add faces for the block at this position and height.

                    // Top face (only for the topmost block).
                    if (y == columnHeight)
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.up);
                    }

                    // Bottom face (only for the bottommost block).
                    if (y == startY)
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.down);
                    }

                    // Side faces.

                    // Left face.
                    if (x == 0 || IsFaceVisible(heightMap, x - 1, z, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.left);
                    }

                    // Right face.
                    if (x == chunkSize - 1 || IsFaceVisible(heightMap, x + 1, z, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.right);
                    }

                    // Back face.
                    if (z == 0 || IsFaceVisible(heightMap, x, z - 1, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.back);
                    }

                    // Front face.
                    if (z == chunkSize - 1 || IsFaceVisible(heightMap, x, z + 1, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.forward);
                    }
                }
            }
        }

        return meshData;
    }

    /// <summary>
    /// Determines if a face should be visible based on the neighboring block's height.
    /// </summary>
    /// <param name="heightMap">The height map of the chunk.</param>
    /// <param name="x">X-coordinate in the height map.</param>
    /// <param name="z">Z-coordinate in the height map.</param>
    /// <param="y">Current Y-level being evaluated.</param>
    /// <returns>True if the face should be visible; otherwise, false.</returns>
    bool IsFaceVisible(int[,] heightMap, int x, int z, int y)
    {
        if (x < 0 || x >= heightMap.GetLength(0) || z < 0 || z >= heightMap.GetLength(1))
        {
            // Neighbor is outside the chunk; face is visible.
            return true;
        }

        int neighborColumnHeight = heightMap[x, z];
        int neighborStartY = Mathf.Min(0, neighborColumnHeight); // Consider negative y-values.
        int neighborEndY = Mathf.Max(0, neighborColumnHeight);   // Consider positive y-values.

        // Adjust the logic for visibility when y < 0.
        if (y >= 0)
        {
            // Standard case when y is above or equal to 0.
            return y > neighborEndY || y < neighborStartY;
        }
        else
        {
            // When y is negative, invert the logic to handle face visibility.
            return y < neighborStartY || y > neighborEndY;
        }
    }

    /// <summary>
    /// Adds a face to the mesh data in the specified direction at the given position.
    /// </summary>
    /// <param name="meshData">The mesh data to add the face to.</param>
    /// <param name="position">The position of the block.</param>
    /// <param name="direction">The direction the face is facing.</param>
    void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction)
    {
        Vector3[] faceVertices = GetFaceVertices(position, direction);
        int vertexIndex = meshData.vertices.Count;

        meshData.vertices.AddRange(faceVertices);

        // Define triangles in clockwise order.
        meshData.triangles.Add(vertexIndex + 0);
        meshData.triangles.Add(vertexIndex + 1);
        meshData.triangles.Add(vertexIndex + 2);

        meshData.triangles.Add(vertexIndex + 2);
        meshData.triangles.Add(vertexIndex + 3);
        meshData.triangles.Add(vertexIndex + 0);

        // Add UVs for texturing.
        meshData.uvs.AddRange(new Vector2[]
        {
            new Vector2(0, 0), // Bottom-left
            new Vector2(0, 1), // Top-left
            new Vector2(1, 1), // Top-right
            new Vector2(1, 0)  // Bottom-right
        });
    }

    /// <summary>
    /// Gets the vertices for a face in a specific direction at the given position.
    /// </summary>
    /// <param name="position">The position of the block.</param>
    /// <param name="direction">The direction the face is facing.</param>
    /// <returns>An array of vertices for the face.</returns>
    Vector3[] GetFaceVertices(Vector3 position, Vector3 direction)
    {
        Vector3[] faceVertices = new Vector3[4];
        float s = voxelScale;

        if (direction == Vector3.up)
        {
            // Top face.
            faceVertices[0] = position + new Vector3(0, s, 0);
            faceVertices[1] = position + new Vector3(0, s, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }
        else if (direction == Vector3.down)
        {
            // Bottom face.
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(s, 0, 0);
            faceVertices[3] = position + new Vector3(s, 0, s);
        }
        else if (direction == Vector3.left)
        {
            // Left face.
            faceVertices[0] = position + new Vector3(0, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, s);
            faceVertices[2] = position + new Vector3(0, s, s);
            faceVertices[3] = position + new Vector3(0, s, 0);
        }
        else if (direction == Vector3.right)
        {
            // Right face.
            faceVertices[0] = position + new Vector3(s, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, 0);
            faceVertices[2] = position + new Vector3(s, s, 0);
            faceVertices[3] = position + new Vector3(s, s, s);
        }
        else if (direction == Vector3.forward)
        {
            // Front face.
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(0, s, s);
        }
        else if (direction == Vector3.back)
        {
            // Back face.
            faceVertices[0] = position + new Vector3(s, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(0, s, 0);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }

        return faceVertices;
    }

    /// <summary>
    /// Converts a world position to chunk coordinates.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <returns>The chunk coordinates as a Vector2Int.</returns>
    Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
        int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
        return new Vector2Int(x, z);
    }
}

/// <summary>
/// Represents mesh data containing vertices, triangles, and UVs.
/// </summary>
public class MeshData
{
    /// <summary>
    /// The list of vertices in the mesh.
    /// </summary>
    public List<Vector3> vertices = new List<Vector3>();

    /// <summary>
    /// The list of triangle indices in the mesh.
    /// </summary>
    public List<int> triangles = new List<int>();

    /// <summary>
    /// The list of UV coordinates for texturing.
    /// </summary>
    public List<Vector2> uvs = new List<Vector2>();
}
