using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace World.NoiseGeneration
{
    [BurstCompile]
    public struct HeightMapJob : IJobParallelFor
    {
        [ReadOnly] public int chunkSizeX;
        [ReadOnly] public int chunkSizeZ;
        [ReadOnly] public float noiseScale;
        [ReadOnly] public float persistence;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public int octaves;
        [ReadOnly] public float heightMultiplier;
        [ReadOnly] public float offsetX;
        [ReadOnly] public float offsetZ;
        [ReadOnly] public int seed;

        [WriteOnly] public NativeArray<float> heightMap;

        public void Execute(int index)
        {
            int x = index / chunkSizeZ;
            int z = index % chunkSizeZ;

            int worldX = x;
            int worldZ = z;

            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (worldX + offsetX) * noiseScale * frequency;
                float sampleZ = (worldZ + offsetZ) * noiseScale * frequency;

                // To simulate Mathf.PerlinNoise, use math.sin and math.cos as a placeholder
                // since Burst does not support Mathf.PerlinNoise. For actual Perlin noise,
                // consider using a Burst-compatible noise library or implement Perlin noise manually.

                // Placeholder noise function
                float perlinValue = (math.sin(sampleX) + math.cos(sampleZ)) * 0.5f + 0.5f;
                perlinValue *= 2f;

                noiseHeight += perlinValue * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            // Apply height multiplier
            float heightValue = noiseHeight * heightMultiplier;

            heightMap[index] = heightValue;
        }
    }
}
