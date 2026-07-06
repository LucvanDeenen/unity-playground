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
        private const float ClimateContrast = 1.6f;

        private readonly NoiseGenerator noise;
        private readonly IReadOnlyList<BiomeDefinition> biomes;
        private readonly bool generatePaths;
        private readonly Color pathColor;
        private readonly float pathWidth;
        private readonly float pathDepth;
        private readonly float terraceHeight;

        /// <summary>Water level in blocks; reserved for the future water milestone.</summary>
        public float SeaLevel { get; }

        public BiomeGenerator(NoiseGenerator noise, IReadOnlyList<BiomeDefinition> biomes, float seaLevel, bool generatePaths, Color pathColor, float pathWidth, float pathDepth, float terraceHeight)
        {
            this.noise = noise;
            this.biomes = biomes;
            this.generatePaths = generatePaths;
            this.pathColor = pathColor;
            this.pathWidth = pathWidth;
            this.pathDepth = pathDepth;
            this.terraceHeight = terraceHeight;
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

            height = ApplyTerracing(height);

            Color surface = Color.clear;
            Color cliff = Color.clear;
            float treeDensity = 0f;
            float grassDensity = 0f;
            float boulderDensity = 0f;
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
                treeDensity += weight * biome.treeDensity;
                grassDensity += weight * biome.grassDensity;
                boulderDensity += weight * biome.boulderDensity;
            }

            surface.a = 1f;
            cliff.a = 1f;

            // Trails follow the midline contour of a broad noise field, which
            // yields long connected winding paths across the world. The distance
            // to the contour is estimated as |n - 0.5| / |gradient| so trails
            // keep a constant width instead of blobbing out where the noise
            // plateaus near its midline.
            float pathMask = 0f;
            if (generatePaths)
            {
                const float gradientStep = 2f;
                float pathNoise = noise.Paths.Sample01(worldX, worldZ);
                float gradX = noise.Paths.Sample01(worldX + gradientStep, worldZ) - pathNoise;
                float gradZ = noise.Paths.Sample01(worldX, worldZ + gradientStep) - pathNoise;
                float gradientPerBlock = Mathf.Sqrt(gradX * gradX + gradZ * gradZ) / gradientStep;
                float distanceBlocks = Mathf.Abs(pathNoise - 0.5f) / Mathf.Max(gradientPerBlock, 0.0004f);
                pathMask = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pathWidth * 0.6f, pathWidth, distanceBlocks));
                // Fade trails out on rugged mountain relief.
                pathMask *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.65f, relief));
                surface = Color.Lerp(surface, pathColor, pathMask);

                // Sink the trail bed below the surrounding terrain.
                if (pathMask > 0.45f)
                {
                    height -= pathDepth;
                }
            }

            return new BiomeColumn
            {
                height = height,
                surfaceColor = surface,
                cliffColor = cliff,
                treeDensity = treeDensity,
                grassDensity = grassDensity,
                boulderDensity = boulderDensity,
                pathMask = pathMask,
                dominantBiome = (byte)dominant,
                dominantWeight = weights[dominant] / totalWeight,
            };
        }

        /// <summary>
        /// Snaps height into flat treads separated by sharp risers, turning smooth
        /// rolling noise into rigid stepped hills.
        /// </summary>
        private float ApplyTerracing(float height)
        {
            if (terraceHeight < 0.01f)
            {
                return height;
            }

            const float riserWidth = 0.2f;
            float steps = height / terraceHeight;
            float tread = Mathf.Floor(steps);
            float riser = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f - riserWidth, 0.5f + riserWidth, steps - tread));
            return (tread + riser) * terraceHeight;
        }

        private static float Spread(float value)
        {
            return Mathf.Clamp01((value - 0.5f) * ClimateContrast + 0.5f);
        }
    }
}
