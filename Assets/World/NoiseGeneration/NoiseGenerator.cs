using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// Generates noise values for terrain generation.
    /// </summary>
    public class NoiseGenerator
    {
        // Noise settings for regular terrain
        private float noiseScale = 0.005f;
        private float persistence = 0.5f;
        private float lacunarity = 2f;
        private int octaves = 4;
        private float heightMultiplier = 15f;

        private System.Random prng;
        private float offsetX;
        private float offsetZ;
        private int seed;

        public int Seed => seed; // Add a public property to get the seed

        /// <summary>
        /// Initializes the NoiseGenerator with a specific seed.
        /// </summary>
        /// <param name="seed">Seed for noise generation.</param>
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

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    // Apply height multiplier
                    float heightValue = noiseHeight * heightMultiplier;
                    heightMap[x, z] = heightValue;
                }
            }

            return heightMap;
        }

        /// <summary>
        /// Gets a normalized noise value between 0 and 1.
        /// </summary>
        public float GetNormalizedNoiseValue(float x, float z, float noiseScaleOverride = -1f)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;
            float maxPossibleHeight = 0f;

            float usedNoiseScale = (noiseScaleOverride > 0f) ? noiseScaleOverride : this.noiseScale;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + offsetX) * usedNoiseScale * frequency;
                float sampleZ = (z + offsetZ) * usedNoiseScale * frequency;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
                noiseHeight += perlinValue * amplitude;

                maxPossibleHeight += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return noiseHeight / maxPossibleHeight;
        }

    }
}
