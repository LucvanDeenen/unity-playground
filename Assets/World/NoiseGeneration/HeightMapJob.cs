using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

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

            float worldX = x + offsetX;
            float worldZ = z + offsetZ;

            // float amplitude = 1f;
            // float frequency = 1f;
            float noiseHeight = 0f;

            // FastNoiseLite noise = new FastNoiseLite(42);
            // noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            // noise.SetFrequency(noiseScale);

            // for (int i = 0; i < octaves; i++)
            // {
            //     float sampleX = worldX * frequency;
            //     float sampleZ = worldZ * frequency;

            //     float perlinValue = noise.GetNoise(sampleX, sampleZ);
            //     perlinValue = (perlinValue + 1f) / 2f; // Normalize to 0-1

            //     noiseHeight += perlinValue * amplitude;

            //     amplitude *= persistence;
            //     frequency *= lacunarity;
            // }

            float heightValue = noiseHeight * heightMultiplier;
            heightMap[index] = heightValue;
        }

        public NativeArray<float> GenerateHeightMapMainThread()
        {
            NativeArray<float> heightMap = new NativeArray<float>(chunkSizeX * chunkSizeZ, Allocator.TempJob);

            FastNoiseLite noise = new FastNoiseLite();
            noise.SetSeed(seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            noise.SetFrequency(noiseScale);

            for (int i = 0; i < chunkSizeX; i++)
            {
                for (int j = 0; j < chunkSizeZ; j++)
                {
                    float sampleX = (i + offsetX) * noiseScale;
                    float sampleZ = (j + offsetZ) * noiseScale;
                    float perlinValue = noise.GetNoise(sampleX, sampleZ);
                    perlinValue = (perlinValue + 1f) / 2f; // Normalize to 0-1
                    heightMap[i * chunkSizeZ + j] = perlinValue * heightMultiplier;
                }
            }

            return heightMap;
        }
    }
}
