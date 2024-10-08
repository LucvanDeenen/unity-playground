using UnityEngine;

public class PerlinBlockTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 100;          // Number of blocks along the X-axis
    public int terrainDepth = 100;          // Number of blocks along the Z-axis
    public int maxHeight = 20;              // Maximum height (Y-axis) of the terrain

    [Header("Perlin Noise Settings")]
    [Range(1f, 100f)]
    public float scale = 20f;                // Scale of the Perlin noise
    public float heightMultiplier = 5f;      // Multiplier for height variation

    [Header("Offset Settings")]
    public float offsetX = 100f;             // X offset for the noise
    public float offsetZ = 100f;             // Z offset for the noise

    [Header("Block Settings")]
    public GameObject blockPrefab;           // Prefab to use for blocks
    public Vector3 blockScale = Vector3.one; // Scale of each block

    [Header("Seed Settings")]
    public int seed = 42;                    // Seed for randomizing offsets
    public Vector2 randomOffsetRange = new Vector2(0f, 1000f);
    public bool useSeed = true;              // Toggle to use seed

    [Header("Generation Settings")]
    public bool generateOnStart = true;      // Generate terrain on Start
    public bool clearExisting = true;        // Clear existing blocks before generating

    // Parent object to hold all blocks (for organization)
    private GameObject terrainParent;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateTerrain();
        }
    }

    /// <summary>
    /// Generates the block-based terrain using Perlin noise.
    /// </summary>
    public void GenerateTerrain()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("Block Prefab is not assigned!");
            return;
        }

        // Initialize random seed if using seed-based offsets
        if (useSeed)
        {
            Random.InitState(seed);
            offsetX = Random.Range(randomOffsetRange.x, randomOffsetRange.y);
            offsetZ = Random.Range(randomOffsetRange.x, randomOffsetRange.y);
        }

        // Create a parent object to organize blocks
        terrainParent = new GameObject("TerrainBlocks");

        // Optional: Clear existing blocks
        if (clearExisting && terrainParent.transform.childCount > 0)
        {
            foreach (Transform child in terrainParent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Iterate through the terrain grid
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int z = 0; z < terrainDepth; z++)
            {
                // Calculate Perlin noise value
                float sampleX = (x + offsetX) / scale;
                float sampleZ = (z + offsetZ) / scale;
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

                // Determine the height based on Perlin noise
                int y = Mathf.RoundToInt(perlinValue * heightMultiplier);
                y = Mathf.Clamp(y, 1, maxHeight); // Ensure y is within bounds

                // Instantiate blocks up to the calculated height
                for (int currentY = 0; currentY < y; currentY++)
                {
                    Vector3 blockPosition = new Vector3(x, currentY, z);
                    GameObject block = Instantiate(blockPrefab, blockPosition, Quaternion.identity, terrainParent.transform);
                    block.transform.localScale = blockScale;
                }
            }
        }

        Debug.Log("Terrain Generation Complete!");
    }

    /// <summary>
    /// Regenerates the terrain. Useful for runtime adjustments.
    /// </summary>
    [ContextMenu("Regenerate Terrain")]
    public void RegenerateTerrain()
    {
        GenerateTerrain();
    }

    /// <summary>
    /// Clears all generated blocks.
    /// </summary>
    [ContextMenu("Clear Terrain")]
    public void ClearTerrain()
    {
        if (terrainParent != null)
        {
            DestroyImmediate(terrainParent);
        }
    }

    // Optional: Regenerate terrain when parameters are changed in the Inspector
    void OnValidate()
    {
        if (generateOnStart && Application.isPlaying)
        {
            GenerateTerrain();
        }
    }
}
