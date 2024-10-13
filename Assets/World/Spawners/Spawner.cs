using UnityEngine;
using World.Shared;
using World.NoiseGeneration;

namespace World.Spawners
{
    /// <summary>
    /// Abstract base class for all spawner types.
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
        /// Spawns objects on a terrain chunk.
        /// </summary>
        public abstract void Spawn(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord);

        /// <summary>
        /// Gets the constrained rotation with z-axis set to 90 degrees.
        /// </summary>
        protected Quaternion GetConstrainedRotation(System.Random prng)
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
