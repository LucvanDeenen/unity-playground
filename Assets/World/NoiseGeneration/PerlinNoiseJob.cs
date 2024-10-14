using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// Job for generating noise values for terrain generation.
    /// </summary>
    public struct PerlinNoiseJob : IJob
    {
        // Noise settings
        public float noiseScale;
        public float persistence;
        public float lacunarity;
        public int octaves;
        public float heightMultiplier;

        // Offsets for noise to ensure unique terrain per chunk
        public float offsetX;
        public float offsetZ;

        // Terrain parameters
        public int width;
        public int height;
        public Vector2Int chunkCoord;
        public int chunkSize;

        // Output array to store height values
        public NativeArray<float> heights;

        /// <summary>
        /// Executes the Perlin noise generation job.
        /// </summary>
        public void Execute()
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldZ = chunkCoord.y * chunkSize + z;

                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;
                    float maxPossibleHeight = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (worldX + offsetX) * noiseScale * frequency;
                        float sampleZ = (worldZ + offsetZ) * noiseScale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f;
                        noiseHeight += perlinValue * amplitude;

                        maxPossibleHeight += amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    // Normalize the noise height and apply height multiplier
                    float normalizedHeight = (noiseHeight / maxPossibleHeight) * heightMultiplier;
                    heights[x + z * width] = normalizedHeight;
                }
            }
        }
    }
}
