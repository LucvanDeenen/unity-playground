using System.Collections.Generic;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    public Transform player;                        // Reference to the player's Transform
    public int chunkSize = 32;                      // Size of each chunk
    public int renderDistance = 20;                 // Number of chunks to generate around the player
    public float voxelScale = 0.75f;                // Scale of each voxel
    public float noiseScale = 0.005f;               // Adjusted scale of the Perlin noise
    public float heightMultiplier = 10f;            // Height multiplier for the terrain
    public int octaves = 6;                         // Number of noise layers
    public float persistence = 0.5f;                // Controls amplitude of each octave
    public float lacunarity = 2f;                   // Controls frequency of each octave
    public int seed = 42;                           // Seed for randomizing noise
    public Material voxelMaterial;                  // Material to apply to the voxels

    private Dictionary<Vector2Int, GameObject> chunkDictionary = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        UpdateChunks();  // Generate initial chunks around the player
    }

    void Update()
    {
        UpdateChunks();  // Update chunks as the player moves
    }

    void UpdateChunks()
    {
        Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

        // Determine which chunks should be active
        List<Vector2Int> activeChunks = new List<Vector2Int>();

        for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
        {
            for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + xOffset, playerChunkCoord.y + zOffset); // Use y for z coordinate
                activeChunks.Add(chunkCoord);

                if (!chunkDictionary.ContainsKey(chunkCoord))
                {
                    // Generate and store new chunk
                    GameObject chunkObject = GenerateChunk(chunkCoord);
                    chunkDictionary.Add(chunkCoord, chunkObject);
                }
            }
        }

        // Remove chunks that are no longer within the render distance
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

    GameObject GenerateChunk(Vector2Int chunkCoord)
    {
        GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObject.transform.parent = transform;
        Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkSize * voxelScale, 0, chunkCoord.y * chunkSize * voxelScale);
        chunkObject.transform.position = chunkPosition;

        // Generate mesh data for the chunk
        MeshData meshData = GenerateChunkMesh(chunkCoord);

        // Create mesh from mesh data
        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh
        {
            vertices = meshData.vertices.ToArray(),
            triangles = meshData.triangles.ToArray()
        };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = voxelMaterial;  // Assign the voxel material

        // Optional: Add Mesh Collider
        MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        return chunkObject;
    }

    MeshData GenerateChunkMesh(Vector2Int chunkCoord)
    {
        MeshData meshData = new MeshData();
        int[,] heightMap = new int[chunkSize + 1, chunkSize + 1];

        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000);
        float offsetZ = prng.Next(-100000, 100000);

        // Generate height map for the chunk
        for (int x = 0; x <= chunkSize; x++)
        {
            for (int z = 0; z <= chunkSize; z++)
            {
                int worldX = chunkCoord.x * chunkSize + x;
                int worldZ = chunkCoord.y * chunkSize + z;

                // Calculate height using fractal noise (multiple octaves)
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (worldX + offsetX) * noiseScale * frequency;
                    float sampleZ = (worldZ + offsetZ) * noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // Adjusted to range [-1, 1]
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Apply height multiplier and store in height map
                float height = noiseHeight * heightMultiplier;

                heightMap[x, z] = Mathf.RoundToInt(height);
            }
        }

        // Generate mesh data based on the height map
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int columnHeight = heightMap[x, z];

                int startY = Mathf.Min(0, columnHeight);
                int endY = Mathf.Max(0, columnHeight);

                for (int y = startY; y <= endY; y++)
                {
                    Vector3 blockPosition = new Vector3(x * voxelScale, y * voxelScale, z * voxelScale);

                    // Add faces for the block at this position and height
                    // Top face (only for the topmost block)
                    if (y == columnHeight)
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.up);
                    }

                    // Bottom face (only for the bottommost block)
                    if (y == startY)
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.down);
                    }

                    // Side faces
                    // Left face
                    if (x == 0 || IsFaceVisible(heightMap, x - 1, z, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.left);
                    }
                    // Right face
                    if (x == chunkSize - 1 || IsFaceVisible(heightMap, x + 1, z, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.right);
                    }
                    // Back face
                    if (z == 0 || IsFaceVisible(heightMap, x, z - 1, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.back);
                    }
                    // Front face
                    if (z == chunkSize - 1 || IsFaceVisible(heightMap, x, z + 1, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.forward);
                    }
                }
            }
        }

        return meshData;
    }

    bool IsFaceVisible(int[,] heightMap, int x, int z, int y)
    {
        if (x < 0 || x >= heightMap.GetLength(0) || z < 0 || z >= heightMap.GetLength(1))
        {
            // Neighbor is outside the chunk; face is visible
            return true;
        }

        int neighborColumnHeight = heightMap[x, z];
        int neighborStartY = Mathf.Min(0, neighborColumnHeight);
        int neighborEndY = Mathf.Max(0, neighborColumnHeight);

        return y > neighborEndY || y < neighborStartY;
    }

    void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction)
    {
        Vector3[] faceVertices = GetFaceVertices(position, direction);
        int vertexIndex = meshData.vertices.Count;

        meshData.vertices.AddRange(faceVertices);

        // Define triangles in clockwise order
        meshData.triangles.Add(vertexIndex + 0);
        meshData.triangles.Add(vertexIndex + 1);
        meshData.triangles.Add(vertexIndex + 2);

        meshData.triangles.Add(vertexIndex + 2);
        meshData.triangles.Add(vertexIndex + 3);
        meshData.triangles.Add(vertexIndex + 0);
    }

    Vector3[] GetFaceVertices(Vector3 position, Vector3 direction)
    {
        Vector3[] faceVertices = new Vector3[4];
        float s = voxelScale;

        if (direction == Vector3.up)
        {
            // Top face
            faceVertices[0] = position + new Vector3(0, s, 0);
            faceVertices[1] = position + new Vector3(0, s, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }
        else if (direction == Vector3.down)
        {
            // Bottom face
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(s, 0, 0);
            faceVertices[3] = position + new Vector3(s, 0, s);
        }
        else if (direction == Vector3.left)
        {
            // Left face
            faceVertices[0] = position + new Vector3(0, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, s);
            faceVertices[2] = position + new Vector3(0, s, s);
            faceVertices[3] = position + new Vector3(0, s, 0);
        }
        else if (direction == Vector3.right)
        {
            // Right face
            faceVertices[0] = position + new Vector3(s, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, 0);
            faceVertices[2] = position + new Vector3(s, s, 0);
            faceVertices[3] = position + new Vector3(s, s, s);
        }
        else if (direction == Vector3.forward)
        {
            // Front face
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(0, s, s);
        }
        else if (direction == Vector3.back)
        {
            // Back face
            faceVertices[0] = position + new Vector3(s, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(0, s, 0);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }

        return faceVertices;
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
        int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
        return new Vector2Int(x, z); // Return (x, z) as (x, y) in Vector2Int
    }
}

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
}
