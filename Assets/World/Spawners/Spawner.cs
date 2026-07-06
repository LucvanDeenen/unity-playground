using UnityEngine;
using World.Biomes;
using World.NoiseGeneration;
using World.Shared;

namespace World.Spawners
{
    /// <summary>
    /// Base class for spawners that populate freshly generated chunks.
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
        /// Populates a freshly generated chunk.
        /// </summary>
        public abstract void Spawn(GameObject chunkObject, ChunkData chunkData, float voxelScale, Vector2Int chunkCoord);

        /// <summary>
        /// Random y rotation snapped to 90-degree steps, combined with the x/z
        /// rotation the imported FBX props need to stand upright.
        /// </summary>
        protected static Quaternion GetConstrainedRotation(System.Random prng)
        {
            float randomYRotation = (float)prng.NextDouble() * 360f;
            float snappedYRotation = Mathf.Round(randomYRotation / 90f) * 90f;
            return Quaternion.Euler(-90f, snappedYRotation, 90f);
        }
    }
}
