using UnityEngine;

public class PerlinBlockTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 100;          // Number of blocks along the X-axis
    public int terrainDepth = 100;          // Number of blocks along the Z-axis

    [Header("Perlin Noise Settings")]
    [Range(1f, 100f)]
    public float scale = 20f;
    public float heightMultiplier = 5f;

    [Header("Block Settings")]
    public GameObject blockPrefab;           // Prefab to use for blocks

    [Header("Seed Settings")]
    public int seed = 42;
    public Vector2 randomOffsetRange = new Vector2(0f, 1000f);

    float offsetX, offsetZ;
    public void GenerateTerrain()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("Block Prefab is not assigned!");
            return;
        }

        Random.InitState(seed);
        offsetX = Random.Range(randomOffsetRange.x, randomOffsetRange.y);
        offsetZ = Random.Range(randomOffsetRange.x, randomOffsetRange.y);

        // Iterate through the terrain grid
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int z = 0; z < terrainDepth; z++)
            {
                for (int y = 0; y < 20; y++)
                {
                    float sampleX = (x + offsetX) / scale;
                    float sampleZ = (z + offsetZ) / scale;

                    float perlinY = Mathf.PerlinNoise(sampleX, sampleZ);
                    if (perlinY >= 0.5f)
                        Instantiate(blockPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
                }
                // int y = Mathf.RoundToInt(perlinValue * heightMultiplier);
                // for (int currentY = 0; currentY < y; currentY++)
                // {
                //     Vector3 blockPosition = new Vector3(x, currentY, z);
                //     Instantiate(blockPrefab, blockPosition, Quaternion.identity, transform);
                // }
            }
        }

        Debug.Log("Terrain Generation Complete!");
    }

    void Start()
    {
        GenerateTerrain();
    }
}
