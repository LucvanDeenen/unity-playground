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

        /// <summary>Broad shape used for mountain massifs.</summary>
        public Channel Height { get; }
        /// <summary>Local knolls and hillocks.</summary>
        public Channel Hills { get; }
        /// <summary>Regional hilliness: some areas are flat meadows, others clustered hills.</summary>
        public Channel HillMask { get; }
        /// <summary>Gentle large-scale elevation swell.</summary>
        public Channel BaseSwell { get; }
        /// <summary>Continental elevation: sweeps whole regions from lowlands to highlands.</summary>
        public Channel Elevation { get; }
        public Channel Temperature { get; }
        public Channel Moisture { get; }
        public Channel Relief { get; }
        public Channel Paths { get; }
        /// <summary>Rare set-piece landmarks: boulder mountains at its peaks, valley bowls at its lows.</summary>
        public Channel Landmark { get; }

        public int Seed { get; }

        private readonly float offset3DX;
        private readonly float offset3DY;
        private readonly float offset3DZ;

        /// <param name="landscapeScale">Multiplies the size of all landforms; higher = broader, more sweeping terrain.</param>
        public NoiseGenerator(int seed, float landscapeScale = 1f)
        {
            Seed = seed;
            float scale = Mathf.Max(0.1f, landscapeScale);
            System.Random prng = new System.Random(seed);
            offset3DX = prng.Next(-100000, 100000);
            offset3DY = prng.Next(-100000, 100000);
            offset3DZ = prng.Next(-100000, 100000);
            // Few octaves and low persistence keep slopes smooth: every extra
            // detail octave quantizes into distracting one-block ripples.
            // Wavelengths are stretched wide so single hills and massifs span
            // hundreds of blocks — big gradual landmarks instead of dense lumps.
            Height = new Channel(prng, 0.0028f / scale, 4, 0.4f);
            Hills = new Channel(prng, 0.0065f / scale, 2, 0.35f);
            HillMask = new Channel(prng, 0.0025f / scale, 2);
            BaseSwell = new Channel(prng, 0.0012f / scale, 2);
            // Fast enough that flat plains and towering hill clusters both
            // appear within one vista, slow enough that each swell spans many
            // hills.
            Elevation = new Channel(prng, 0.001f / scale, 2);
            // Climate varies over very large distances so single biomes span
            // whole regions instead of patchworking within one view.
            Temperature = new Channel(prng, 0.0008f, 2);
            Moisture = new Channel(prng, 0.0008f, 2);
            Relief = new Channel(prng, 0.0012f, 3);
            Paths = new Channel(prng, 0.004f, 2);
            // Constructed last so adding this channel left every earlier
            // channel's seed offsets — and the existing world layout — intact.
            Landmark = new Channel(prng, 0.0035f / scale, 2);
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

        /// <summary>
        /// Volumetric noise (0..1) approximated from three 2D samples. The
        /// vertical axis runs at a higher frequency so undercuts stay chunky
        /// rather than stretched.
        /// </summary>
        public float Sample3D01(float x, float y, float z)
        {
            const float horizontalScale = 0.055f;
            const float verticalScale = 0.07f;

            float sx = (x + offset3DX) * horizontalScale;
            float sy = (y + offset3DY) * verticalScale;
            float sz = (z + offset3DZ) * horizontalScale;

            float value = (Mathf.PerlinNoise(sx, sy) + Mathf.PerlinNoise(sy, sz) + Mathf.PerlinNoise(sz, sx)) / 3f;

            // Averaging three samples squeezes the range toward 0.5; stretch it
            // back out so the carve term actually reaches +-strength.
            return Mathf.Clamp01((value - 0.5f) * 3f + 0.5f);
        }
    }
}
