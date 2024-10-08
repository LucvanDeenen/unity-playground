using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// Generates noise values for terrain generation.
    /// </summary>
    public class NoiseGenerator
    {
        // Noise settings for regular terrain
        private float noiseScale = 0.005f;
        private float persistence = 0.5f;
        private float lacunarity = 2f;
        private int octaves = 4;
        private float heightMultiplier = 15f;
        private float baseHeight = 20f;
    
        // Noise settings for valley terrain
        private float valleyNoiseScale = 0.005f;
        private float valleyThreshold = 0.7f;         
    
        private System.Random prng;
        private float offsetX;
        private float offsetZ;
        private int seed;
    
        // Valley offsets to ensure consistency across chunks
        private float valleyOffsetZ;
    
        /// <summary>
        /// Initializes the NoiseGenerator with a specific seed.
        /// </summary>
        /// <param name="seed">Seed for noise generation.</param>
        public NoiseGenerator(int seed)
        {
            this.seed = seed;
            InitializeRandomOffsets();
            InitializeValleyOffsets();
        }
    
        /// <summary>
        /// Initializes random offsets based on the seed to ensure unique terrain generation.
        /// </summary>
        private void InitializeRandomOffsets()
        {
            // Initialize PRNG for regular terrain
            prng = new System.Random(seed);
            offsetX = prng.Next(-100000, 100000);
            offsetZ = prng.Next(-100000, 100000);
        }
    
        /// <summary>
        /// Initializes random offsets for the valley to ensure a consistent valley path across all chunks.
        /// </summary>
        private void InitializeValleyOffsets()
        {
            // Initialize a separate PRNG for valley to ensure different patterns from regular terrain
            System.Random valleyPrng = new System.Random(seed + 1); // Different seed for valley
            valleyOffsetZ = valleyPrng.Next(-100000, 100000);
        }
    
        /// <summary>
        /// Generates a height map using multi-octave Perlin noise for regular terrain.
        /// </summary>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="chunkCoord">Chunk coordinates.</param>
        /// <param name="chunkSize">Size of the chunk.</param>
        /// <returns>2D array representing the height map.</returns>
        public float[,] GenerateHeightMap(int width, int height, Vector2Int chunkCoord, int chunkSize)
        {
            float[,] heightMap = new float[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
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
    
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1; // Range [-1, 1]
                        noiseHeight += perlinValue * amplitude;
    
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }
    
                    // Apply height multiplier and add baseHeight
                    float heightValue = noiseHeight * heightMultiplier + baseHeight;
                    heightMap[x, z] = heightValue;
                }
            }
    
            return heightMap;
        }
    
        /// <summary>
        /// Generates a valley mask using a Gaussian function to define a single, continuous valley.
        /// </summary>
        /// <param name="width">Width of the mask.</param>
        /// <param name="height">Height of the mask.</param>
        /// <param name="chunkCoord">Chunk coordinates.</param>
        /// <param name="chunkSize">Size of the chunk.</param>
        /// <returns>2D array representing the valley mask.</returns>
        public float[,] GenerateValleyMask(int width, int height, Vector2Int chunkCoord, int chunkSize)
        {
            float[,] valleyMask = new float[width, height];
            float twoSigmaSquare = 2 * Mathf.Pow(valleyThreshold * width / 2, 2); // Precompute denominator
    
            for (int z = 0; z < height; z++)
            {
                // Calculate the center x position of the valley for this z using global coordinates
                float globalZ = z + chunkCoord.y * chunkSize + valleyOffsetZ;
                float sampleZ = globalZ * valleyNoiseScale;
                float valleyCenterX = Mathf.PerlinNoise(sampleZ, 0.0f) * (width - 1); // Ensure within [0, width-1]
    
                for (int x = 0; x < width; x++)
                {
                    float distance = Mathf.Abs(x - valleyCenterX);
                    float maskValue = Mathf.Exp(- (distance * distance) / twoSigmaSquare);
                    valleyMask[x, z] = maskValue;
                }
            }
    
            return valleyMask;
        }
    
        /// <summary>
        /// Determines if a specific point is part of the valley based on the valley mask.
        /// </summary>
        /// <param name="valleyMask">2D array representing the valley mask.</param>
        /// <param name="x">X-coordinate within the chunk.</param>
        /// <param name="z">Z-coordinate within the chunk.</param>
        /// <returns>True if the point is part of the valley; otherwise, false.</returns>
        public bool IsValley(int x, int z, float[,] valleyMask)
        {
            if (x < 0 || x >= valleyMask.GetLength(0) || z < 0 || z >= valleyMask.GetLength(1))
                return false;
    
            return valleyMask[x, z] > 0.5f; // Threshold can be adjusted if needed
        }
    
        /// <summary>
        /// Generates smooth noise for valley areas.
        /// </summary>
        /// <param name="x">World X coordinate.</param>
        /// <param name="z">World Z coordinate.</param>
        /// <returns>Smooth noise value for valleys.</returns>
        public float GenerateSmoothNoise(float x, float z)
        {
            // Generate Perlin noise with low frequency for smooth valleys
            float noiseValue = Mathf.PerlinNoise(x * valleyNoiseScale, z * valleyNoiseScale) * 0.5f + 0.5f; // Normalize to [0,1]
            return noiseValue;
        }
    }
}
