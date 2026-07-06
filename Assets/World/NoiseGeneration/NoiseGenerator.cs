using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// Provides seeded, decorrelated noise channels for terrain and climate generation.
    /// </summary>
    public class NoiseGenerator
    {
        /// <summary>
        /// A fractal noise field with its own seed-derived offsets, so channels
        /// sampled at the same coordinates stay independent of each other.
        /// </summary>
        public class Channel
        {
            private readonly float scale;
            private readonly int octaves;
            private readonly float persistence;
            private readonly float lacunarity;
            private readonly float offsetX;
            private readonly float offsetZ;

            internal Channel(System.Random prng, float scale, int octaves, float persistence = 0.5f, float lacunarity = 2f)
            {
                this.scale = scale;
                this.octaves = octaves;
                this.persistence = persistence;
                this.lacunarity = lacunarity;
                offsetX = prng.Next(-100000, 100000);
                offsetZ = prng.Next(-100000, 100000);
            }

            /// <summary>
            /// Samples the fractal noise, normalized to 0..1.
            /// </summary>
            public float Sample01(float x, float z)
            {
                return Sample01(x, z, scale);
            }

            /// <summary>
            /// Samples the fractal noise at an overridden scale, normalized to 0..1.
            /// </summary>
            public float Sample01(float x, float z, float scaleOverride)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float total = 0f;
                float maxTotal = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + offsetX) * scaleOverride * frequency;
                    float sampleZ = (z + offsetZ) * scaleOverride * frequency;

                    total += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                    maxTotal += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                return total / maxTotal;
            }

            /// <summary>
            /// Samples smooth and ridged variants of the same fractal in one pass.
            /// The ridged variant peaks where the noise crosses its midline, which
            /// produces sharp crests when used for mountainous terrain.
            /// </summary>
            public void SampleSmoothAndRidged(float x, float z, out float smooth01, out float ridged01)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float smoothTotal = 0f;
                float ridgedTotal = 0f;
                float maxTotal = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + offsetX) * scale * frequency;
                    float sampleZ = (z + offsetZ) * scale * frequency;

                    float value = Mathf.PerlinNoise(sampleX, sampleZ);
                    smoothTotal += value * amplitude;
                    ridgedTotal += (1f - Mathf.Abs(2f * value - 1f)) * amplitude;
                    maxTotal += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                smooth01 = smoothTotal / maxTotal;
                ridged01 = ridgedTotal / maxTotal;
            }
        }

        public Channel Height { get; }
        public Channel Temperature { get; }
        public Channel Moisture { get; }
        public Channel Relief { get; }
        public Channel Paths { get; }

        public int Seed { get; }

        public NoiseGenerator(int seed)
        {
            Seed = seed;
            System.Random prng = new System.Random(seed);
            // Broad, low-persistence height noise gives gradual Cube World-style
            // slopes that quantize into terraces instead of noisy bumps.
            Height = new Channel(prng, 0.0035f, 4, 0.48f);
            // Climate varies over very large distances so single biomes span
            // whole regions instead of patchworking within one view.
            Temperature = new Channel(prng, 0.0008f, 2);
            Moisture = new Channel(prng, 0.0008f, 2);
            Relief = new Channel(prng, 0.0012f, 3);
            Paths = new Channel(prng, 0.004f, 2);
        }

        /// <summary>
        /// Gets a normalized noise value between 0 and 1. Used by spawners for
        /// clustering; samples the height channel, optionally at another scale.
        /// </summary>
        public float GetNormalizedNoiseValue(float x, float z, float noiseScaleOverride = -1f)
        {
            return noiseScaleOverride > 0f
                ? Height.Sample01(x, z, noiseScaleOverride)
                : Height.Sample01(x, z);
        }
    }
}
