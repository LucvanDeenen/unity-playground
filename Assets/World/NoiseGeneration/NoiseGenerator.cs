using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// Generates noise values for terrain generation using Unity's Job System.
    /// </summary>
    public class NoiseGenerator
    {
        // Noise settings for regular terrain
        private float noiseScale = 0.005f;
        private float persistence = 0.5f;
        private float lacunarity = 2f;
        private int octaves = 4;
        private float heightMultiplier = 15f;
        public float GetHeightMultiplier() => this.heightMultiplier;
        public void SetHeightMultiplier(float heightMultiplier)
        {
            this.heightMultiplier = heightMultiplier;
        }

        private System.Random prng;
        private float offsetX;
        private float offsetZ;
        private int seed;
        public int Seed => seed;

        private Dictionary<Vector2Int, float[,]> heightMapCache = new Dictionary<Vector2Int, float[,]>();

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
        /// Generates a height map for the specified chunk coordinates and size.
        /// Utilizes caching and parallel processing to optimize performance.
        /// Returns a JobHandle that represents the height map generation job.
        /// </summary>
        public JobHandle GenerateHeightMapAsync(Vector2Int chunkCoord, int chunkSize, NativeArray<float> heightMapFlat, HeightMapJob heightMapJob)
        {
            // Check if the height map is already cached
            if (heightMapCache.TryGetValue(chunkCoord, out float[,] cachedHeightMap))
            {
                // Populate the NativeArray with cached data
                for (int i = 0; i < chunkSize; i++)
                {
                    for (int j = 0; j < chunkSize; j++)
                    {
                        heightMapFlat[i * chunkSize + j] = cachedHeightMap[i, j];
                    }
                }

                // Return a default JobHandle since no job was scheduled
                return default;
            }

            // Initialize the HeightMapJob
            heightMapJob = new HeightMapJob
            {
                chunkSizeX = chunkSize,
                chunkSizeZ = chunkSize,
                noiseScale = this.noiseScale,
                persistence = this.persistence,
                lacunarity = this.lacunarity,
                octaves = this.octaves,
                heightMultiplier = this.heightMultiplier,
                offsetX = this.offsetX,
                offsetZ = this.offsetZ,
                seed = this.seed,
                heightMap = heightMapFlat
            };

            // Schedule the job
            JobHandle jobHandle = heightMapJob.Schedule(chunkSize * chunkSize, 64);

            return jobHandle;
        }

        /// <summary>
        /// After the HeightMapJob is complete, cache the height map.
        /// This should be called after job completion.
        /// </summary>
        public void CacheHeightMap(Vector2Int chunkCoord, int chunkSizeX, int chunkSizeZ, NativeArray<float> heightMapFlat)
        {
            if (heightMapCache.ContainsKey(chunkCoord))
                return;

            float[,] heightMap = new float[chunkSizeX, chunkSizeZ];
            for (int i = 0; i < chunkSizeX; i++)
            {
                for (int j = 0; j < chunkSizeZ; j++)
                {
                    heightMap[i, j] = heightMapFlat[i * chunkSizeZ + j];
                }
            }

            heightMapCache.Add(chunkCoord, heightMap);
        }

        /// <summary>
        /// After the HeightMapJob is complete, cache the height map.
        /// This should be called after job completion.
        /// </summary>
        public float[,] GenerateHeightMap(Vector2Int chunkCoord, int chunkSize)
        {
            if (heightMapCache.TryGetValue(chunkCoord, out float[,] cachedHeightMap))
            {
                return cachedHeightMap;
            }

            float[,] heightMap = new float[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
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

            heightMapCache.Add(chunkCoord, heightMap);
            return heightMap;
        }

        /// <summary>
        /// Gets a normalized noise value between 0 and 1.
        /// </summary>
        public float GetNormalizedNoiseValue(float x, float z, float noiseScaleOverride = -1f)
        {
            int octaves = 2;

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

        /// <summary>
        /// Clears the height map cache. Call this method to free memory if necessary.
        /// </summary>
        public void ClearCache()
        {
            heightMapCache.Clear();
        }
    }
}
