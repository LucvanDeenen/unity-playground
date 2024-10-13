using UnityEngine;
using World.Shared;
using World.NoiseGeneration;

namespace World.Spawners
{
    /// <summary>
    /// Base class for all spawner types with generalized spawning logic.
    /// </summary>
    public abstract class Spawner : MonoBehaviour
    {
        protected ObjectPlacementManager placementManager;
        protected NoiseGenerator noiseGenerator;

        public void SetPlacementManager(ObjectPlacementManager manager)
        {
            placementManager = manager;
        }

        public void SetNoiseGenerator(NoiseGenerator generator)
        {
            noiseGenerator = generator;
        }

        /// <summary>
        /// The prefabs to spawn.
        /// </summary>
        protected abstract GameObject[] Prefabs { get; }

        /// <summary>
        /// The minimum and maximum height range where objects can spawn.
        /// </summary>
        protected abstract Vector2 HeightRange { get; }

        /// <summary>
        /// The minimum distance between spawned objects.
        /// </summary>
        protected abstract float MinDistanceBetweenObjects { get; }

        /// <summary>
        /// The chance for an object to spawn at a given position (0 to 1).
        /// </summary>
        protected abstract float SpawnChance { get; }

        /// <summary>
        /// Optional offset to apply to the noise coordinates.
        /// </summary>
        protected virtual Vector2 NoiseOffset => Vector2.zero;

        /// <summary>
        /// Overrides the noise scale used for spawning objects.
        /// </summary>
        protected virtual float NoiseScale => -1f; // Use default unless overridden

        /// <summary>
        /// The step size for sampling positions, based on minimum distance.
        /// </summary>
        protected virtual int StepSize(float voxelScale) => 1; // Default to checking every position

        /// <summary>
        /// Threshold for spawning based on noise value.
        /// </summary>
        protected virtual float SpawnThreshold => SpawnChance; // Default behavior

        /// <summary>
        /// Spawns objects on a terrain chunk.
        /// </summary>
        public virtual void Spawn(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord)
        {
            if (Prefabs == null || Prefabs.Length == 0)
            {
                Debug.LogError($"Prefabs are not assigned in {GetType().Name}.");
                return;
            }

            if (placementManager == null)
            {
                Debug.LogError("ObjectPlacementManager reference is not set in Spawner.");
                return;
            }

            if (noiseGenerator == null)
            {
                Debug.LogError("NoiseGenerator reference is not set in Spawner.");
                return;
            }

            // Get the chunk position
            Vector3 chunkPosition = chunkObject.transform.position;

            int stepSize = StepSize(voxelScale);

            // Loop through positions within the chunk
            for (int x = 0; x < chunkSize; x += stepSize)
            {
                for (int z = 0; z < chunkSize; z += stepSize)
                {
                    // Grid coordinates
                    int xCoord = chunkCoord.x * chunkSize + x;
                    int zCoord = chunkCoord.y * chunkSize + z;

                    // World position coordinates
                    float worldX = chunkPosition.x + (x * voxelScale);
                    float worldZ = chunkPosition.z + (z * voxelScale);

                    // Use the noise generator to decide whether to spawn an object
                    float noiseValue = noiseGenerator.GetNormalizedNoiseValue(
                        worldX + NoiseOffset.x,
                        worldZ + NoiseOffset.y,
                        NoiseScale
                    );

                    if (noiseValue < SpawnThreshold)
                    {
                        int height = heightMap[x, z];
                        float worldY = height * voxelScale;

                        // Check if the height is within the desired range
                        if (worldY >= HeightRange.x && worldY <= HeightRange.y)
                        {
                            // Position in world space
                            Vector3 position = new Vector3(worldX, worldY, worldZ);

                            // Check if position is available
                            if (placementManager.IsPositionAvailable(position, MinDistanceBetweenObjects))
                            {
                                // Create a deterministic random number generator
                                int seed = noiseGenerator.Seed;
                                int hash = seed + xCoord * 73856093 ^ zCoord * 19349663;
                                System.Random prng = new System.Random(hash);

                                // Randomly select a prefab
                                GameObject prefab = Prefabs[prng.Next(Prefabs.Length)];

                                // Instantiate prefab with constrained rotation
                                GameObject instance = Instantiate(prefab, position, GetConstrainedRotation(prng), chunkObject.transform);

                                // Optionally, adjust scale for variation
                                float scaleVariation = (float)(prng.NextDouble() * 0.2 + 0.9); // Range between 0.9 and 1.1
                                instance.transform.localScale *= scaleVariation;

                                // Register the position
                                placementManager.RegisterObjectPosition(position);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the constrained rotation with z-axis set to 90 degrees.
        /// </summary>
        protected virtual Quaternion GetConstrainedRotation(System.Random prng)
        {
            // Randomize the y-axis rotation and snap it to the closest multiple of 90 degrees
            float randomYRotation = (float)prng.NextDouble() * 360f;
            float snappedYRotation = Mathf.Round(randomYRotation / 90f) * 90f;

            // Set z-axis rotation to 90 degrees
            float zRotation = 90f;

            // Return the constrained rotation
            return Quaternion.Euler(-90f, snappedYRotation, zRotation);
        }
    }
}
