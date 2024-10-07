using UnityEngine;

/// <summary>
/// Generates noise values for terrain generation.
/// </summary>
public class NoiseGenerator
{
    // Noise settings
    private int seed;
    private float noiseScale;
    private int octaves;
    private float persistence;
    private float lacunarity;
    private float baseHeight;
    private float heightMultiplier;

    private float maxPossibleHeight;

    private System.Random prng;
    private float offsetX;
    private float offsetZ;

    public NoiseGenerator(int seed, float noiseScale, int octaves, float persistence, float lacunarity, float baseHeight, float heightMultiplier)
    {
        this.seed = seed;
        this.noiseScale = noiseScale;
        this.octaves = octaves;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
        this.baseHeight = baseHeight;
        this.heightMultiplier = heightMultiplier;

        CalculateGlobalHeights();
        InitializeRandomOffsets();
    }

    private void CalculateGlobalHeights()
    {
        maxPossibleHeight = 0f;
        float amplitude = 1f;

        for (int i = 0; i < octaves; i++)
        {
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }
    }

    private void InitializeRandomOffsets()
    {
        prng = new System.Random(seed);
        offsetX = prng.Next(-100000, 100000);
        offsetZ = prng.Next(-100000, 100000);
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
