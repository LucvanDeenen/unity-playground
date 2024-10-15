using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

namespace World.NoiseGeneration
{
    [BurstCompile]
    public struct HeightMapJob : IJobParallelFor
    {
        // Noise parameters
        public float noiseScale;
        public float persistence;
        public float lacunarity;
        public int octaves;
        public float heightMultiplier;

        // Chunk information
        public int mapSize;
        public Vector2Int chunkCoord;
        public int chunkSize;
        public int seed;

        // Noise offsets
        public float offsetX;
        public float offsetZ;

        [WriteOnly]
        public NativeArray<float> heightMap;

        public void Execute(int index)
        {
            int x = index % mapSize;
            int z = index / mapSize;

            // Convert chunk coordinates to world coordinates
            float worldX = (chunkCoord.x * chunkSize + x) * noiseScale + offsetX;
            float worldZ = (chunkCoord.y * chunkSize + z) * noiseScale + offsetZ;

            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = worldX * frequency;
                float sampleZ = worldZ * frequency;

                // 2D Perlin-like noise using math.cnoise
                // float perlinValue = math.cnoise(new float2(sampleX, sampleZ)) * 0.5f + 0.5f; // Normalize to [0,1]
                // noiseHeight += perlinValue * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            // Apply height multiplier
            heightMap[index] = noiseHeight * heightMultiplier;
        }
    }
}
