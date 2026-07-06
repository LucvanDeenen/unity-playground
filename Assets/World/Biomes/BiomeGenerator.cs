using System.Collections.Generic;
using UnityEngine;
using World.NoiseGeneration;

namespace World.Biomes
{
    /// <summary>
    /// Samples climate noise per column, blends biome terrain parameters by climate
    /// weight, and produces the per-chunk data consumed by meshing and spawning.
    /// </summary>
    public class BiomeGenerator
    {
        // Fractal noise clusters around 0.5, so the raw climate values would almost
        // never reach the extremes where deserts, tundra, and mountains live.
        private const float ClimateContrast = 1.8f;

        private readonly NoiseGenerator noise;
        private readonly IReadOnlyList<BiomeDefinition> biomes;

        /// <summary>Water level in blocks; reserved for the future water milestone.</summary>
        public float SeaLevel { get; }

        public BiomeGenerator(NoiseGenerator noise, IReadOnlyList<BiomeDefinition> biomes, float seaLevel)
        {
            this.noise = noise;
            this.biomes = biomes;
            SeaLevel = seaLevel;
        }

        /// <summary>
        /// Generates biome data for a chunk, including its one-column border.
        /// All sampling is in world coordinates, so neighboring chunks agree exactly.
        /// </summary>
        public ChunkData GenerateChunkData(Vector2Int chunkCoord, int chunkSize)
        {
            ChunkData data = new ChunkData(chunkSize);
            float[] weights = new float[biomes.Count];

            for (int x = -1; x <= chunkSize; x++)
            {
                for (int z = -1; z <= chunkSize; z++)
                {
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldZ = chunkCoord.y * chunkSize + z;
                    data.SetColumn(x, z, GenerateColumn(worldX, worldZ, weights));
                }
            }

            return data;
        }

        private BiomeColumn GenerateColumn(float worldX, float worldZ, float[] weights)
        {
            float temperature = Spread(noise.Temperature.Sample01(worldX, worldZ));
            float moisture = Spread(noise.Moisture.Sample01(worldX, worldZ));
            float relief = Spread(noise.Relief.Sample01(worldX, worldZ));

            float totalWeight = 0f;
            int dominant = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = biomes[i].ClimateWeight(temperature, moisture, relief);
                totalWeight += weights[i];
                if (weights[i] > weights[dominant])
                {
                    dominant = i;
                }
            }

            if (totalWeight <= 0.0001f)
            {
                // The climate sample fell in a gap between all biome windows.
                weights[0] = 1f;
                totalWeight = 1f;
                dominant = 0;
            }

            noise.Height.SampleSmoothAndRidged(worldX, worldZ, out float smooth, out float ridgedNoise);

            float height = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                BiomeDefinition biome = biomes[i];
                float shape = Mathf.Lerp(smooth, ridgedNoise, biome.ridged);
                height += weights[i] / totalWeight * (biome.baseHeight + biome.amplitude * shape);
            }

            Color surface = Color.clear;
            Color cliff = Color.clear;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                BiomeDefinition biome = biomes[i];
                float weight = weights[i] / totalWeight;
                float gradientTime = Mathf.Clamp01(Mathf.InverseLerp(biome.baseHeight, biome.baseHeight + biome.amplitude, height));
                surface += weight * biome.surfaceGradient.Evaluate(gradientTime);
                cliff += weight * biome.cliffColor;
            }

            surface.a = 1f;
            cliff.a = 1f;

            return new BiomeColumn
            {
                height = height,
                surfaceColor = surface,
                cliffColor = cliff,
                dominantBiome = (byte)dominant,
                dominantWeight = weights[dominant] / totalWeight,
            };
        }

        private static float Spread(float value)
        {
            return Mathf.Clamp01((value - 0.5f) * ClimateContrast + 0.5f);
        }
    }
}
