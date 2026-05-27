using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// Generates deterministic Perlin-noise values for terrain and object placement.
    /// Parameterized via a <see cref="NoiseSettings"/> asset so tuning never requires
    /// touching this class.
    /// </summary>
    public class NoiseGenerator
    {
        private readonly NoiseSettings settings;
        private readonly float offsetX;
        private readonly float offsetZ;
        private readonly int seed;

        /// <summary>The seed used to initialize this generator.</summary>
        public int Seed => seed;

        /// <summary>
        /// Creates a NoiseGenerator with a fixed seed and the supplied settings.
        /// </summary>
        /// <param name="seed">Seed for reproducible terrain.</param>
        /// <param name="settings">Noise parameters. Must not be null.</param>
        public NoiseGenerator(int seed, NoiseSettings settings)
        {
            this.seed = seed;
            this.settings = settings;

            System.Random prng = new System.Random(seed);
            offsetX = prng.Next(-100000, 100000);
            offsetZ = prng.Next(-100000, 100000);
        }

        /// <summary>
        /// Generates a 2D height map for the given chunk, using multi-octave Perlin noise.
        /// </summary>
        /// <param name="width">Number of columns (usually chunkSize + 1).</param>
        /// <param name="height">Number of rows (usually chunkSize + 1).</param>
        /// <param name="chunkCoord">Chunk grid coordinate — used to offset world positions.</param>
        /// <param name="chunkSize">Voxel size of a single chunk side.</param>
        /// <returns>A [width × height] array of raw height values.</returns>
        public float[,] GenerateHeightMap(int width, int height, Vector2Int chunkCoord, int chunkSize)
        {
            float[,] heightMap = new float[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldZ = chunkCoord.y * chunkSize + z;
                    heightMap[x, z] = SampleHeight(worldX, worldZ);
                }
            }
            return heightMap;
        }

        /// <summary>
        /// Returns the raw (unnormalized) terrain height at the given world coordinates.
        /// </summary>
        public float SampleHeight(float worldX, float worldZ)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;

            for (int i = 0; i < settings.octaves; i++)
            {
                float sampleX = (worldX + offsetX) * settings.noiseScale * frequency;
                float sampleZ = (worldZ + offsetZ) * settings.noiseScale * frequency;
                noiseHeight += Mathf.PerlinNoise(sampleX, sampleZ) * 2f * amplitude;
                amplitude  *= settings.persistence;
                frequency  *= settings.lacunarity;
            }

            return noiseHeight * settings.heightMultiplier;
        }

        /// <summary>
        /// Returns a noise value in [0, 1] at the given world coordinates.
        /// Useful for probabilistic spawn decisions and biome masking.
        /// </summary>
        /// <param name="x">World X.</param>
        /// <param name="z">World Z.</param>
        /// <param name="noiseScaleOverride">
        /// If positive, overrides the scale from settings — useful when a spawner
        /// wants tighter or looser clustering than the terrain noise.
        /// </param>
        public float GetNormalizedNoiseValue(float x, float z, float noiseScaleOverride = -1f)
        {
            float amplitude        = 1f;
            float frequency        = 1f;
            float noiseHeight      = 0f;
            float maxPossibleHeight = 0f;

            float scale = noiseScaleOverride > 0f ? noiseScaleOverride : settings.noiseScale;

            for (int i = 0; i < settings.octaves; i++)
            {
                float sampleX = (x + offsetX) * scale * frequency;
                float sampleZ = (z + offsetZ) * scale * frequency;
                noiseHeight      += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                maxPossibleHeight += amplitude;
                amplitude  *= settings.persistence;
                frequency  *= settings.lacunarity;
            }

            return noiseHeight / maxPossibleHeight;
        }
    }
}
