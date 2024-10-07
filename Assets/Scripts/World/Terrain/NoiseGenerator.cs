using UnityEngine;

/// <summary>
/// Generates noise values for terrain generation.
/// </summary>
public class NoiseGenerator
{
    // Noise settings for regular terrain
    private float noiseScale = 0.005f;
    private float persistence = 0.1f;
    private float lacunarity = 5f;
    private int octaves = 6;
    private float heightMultiplier = 15f;
    private float baseHeight = 100f;

    // Noise settings for cliff terrain
    private float smoothNoiseScale = 0.02f; // Lower frequency for smoother cliffs
    private float smoothNoiseAmplitude = 10f; // Controlled amplitude for consistent cliff heights

    private System.Random prng;
    private float offsetX;
    private float offsetZ;
    private int seed;

    /// <summary>
    /// Initializes the NoiseGenerator with a specific seed.
    /// </summary>
    /// <param name="seed">Seed for random offset generation.</param>
    public NoiseGenerator(int seed)
    {
        this.seed = seed;
        InitializeRandomOffsets();
    }

    /// <summary>
    /// Initializes random offsets based on the seed to ensure unique terrain generation.
    /// </summary>
    private void InitializeRandomOffsets()
    {
        prng = new System.Random(seed);
        offsetX = prng.Next(-100000, 100000);
        offsetZ = prng.Next(-100000, 100000);
    }

    /// <summary>
    /// Generates smooth noise for cliff areas.
    /// </summary>
    /// <param name="x">World X coordinate.</param>
    /// <param name="z">World Z coordinate.</param>
    /// <returns>Smooth noise value for cliffs.</returns>
    public float GenerateSmoothNoise(float x, float z)
    {
        // Generate Perlin noise with low frequency for smooth cliffs
        float noiseValue = Mathf.PerlinNoise(x * smoothNoiseScale, z * smoothNoiseScale) * smoothNoiseAmplitude;
        return noiseValue;
    }

    /// <summary>
    /// Generates a height map using multi-octave Perlin noise for regular terrain.
    /// </summary>
    /// <param name="width">Width of the height map.</param>
    /// <param name="height">Height of the height map.</param>
    /// <param name="chunkCoord">Chunk coordinates.</param>
    /// <param name="chunkSize">Size of the chunk.</param>
    /// <returns>2D array representing the height map.</returns>
    public float[,] GenerateHeightMap(int width, int height, Vector2Int chunkCoord, int chunkSize)
    {
        float[,] heightMap = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
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

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // Range [-1, 1]
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Apply height multiplier and add baseHeight
                float heightValue = noiseHeight * heightMultiplier + baseHeight;
                heightMap[x, z] = heightValue;
            }
        }

        return heightMap;
    }
}
