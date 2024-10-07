using UnityEngine;

/// <summary>
/// Generates noise values for terrain generation.
/// </summary>
public class NoiseGenerator
{
    // Noise settings
    private float noiseScale = 0.005f;
    private float persistence = 0.1f;
    private float lacunarity = 5f;
    private int octaves = 6;
    private float heightMultiplier = 15f;
    private float baseHeight = 100f;

    private System.Random prng;
    private float offsetX;
    private float offsetZ;
    private int seed;

    public NoiseGenerator(int seed)
    {
        this.seed = seed;

        InitializeRandomOffsets();
    }

    private void InitializeRandomOffsets()
    {
        prng = new System.Random(seed);
        offsetX = prng.Next(-100000, 100000);
        offsetZ = prng.Next(-100000, 100000);
    }

    public float GenerateSmoothNoise(float x, float z)
    {
        // Use different noise parameters for smoother terrain
        float frequency = 0.01f; 
        float amplitude = 20f;   

        // Implement your noise function here
        float noiseValue = Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;

        return noiseValue;
    }
    
    public float[,] GenerateHeightMap(int width, int height, Vector2Int chunkCoord, int chunkSize)
    {
        float[,] heightMap = new float[width, height];

        for (int x = 0; x <= chunkSize; x++)
        {
            for (int z = 0; z <= chunkSize; z++)
            {
                int worldX = chunkCoord.x * chunkSize + x;
                int worldZ = chunkCoord.y * chunkSize + z;

                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (worldX + offsetX) * noiseScale * frequency;
                    float sampleZ = (worldZ + offsetZ) * noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Apply height multiplier and add baseHeight.
                float heightValue = noiseHeight * heightMultiplier + baseHeight;
                heightMap[x, z] = heightValue;
            }
        }

        return heightMap;
    }
}
