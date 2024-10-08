using UnityEngine;

namespace Terrain.Spawners
{
    /// <summary>
    /// Handles spawning of boulders on the voxel terrain.
    /// </summary>
    public class BoulderSpawner : Spawner
    {
        [Header("Boulder Settings")]
        [Tooltip("The boulder prefab to spawn on the terrain.")]
        public GameObject boulderPrefab;

        private Vector2 heightRange = new Vector2(110f, 250f);
        private float spawnChance = 0.05f;
        private float boulderSpacing = 20f;
        private float minDistanceBetweenBoulders = 5f;

        /// <summary>
        /// Spawns boulders on the given chunk.
        /// </summary>
        public override void Spawn(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord)
        {
            if (boulderPrefab == null)
            {
                Debug.LogError("Boulder prefab is not assigned in BoulderSpawner.");
                return;
            }

            if (placementManager == null)
            {
                Debug.LogError("ObjectPlacementManager reference is not set in BoulderSpawner.");
                return;
            }

            // Get the chunk position
            Vector3 chunkPosition = chunkObject.transform.position;

            // Determine boulder grid spacing
            float worldChunkSize = chunkSize * voxelScale;
            int boulderGridSize = Mathf.CeilToInt(worldChunkSize / boulderSpacing);

            // Loop through positions within the chunk based on boulder spacing
            for (int x = 0; x < boulderGridSize; x++)
            {
                for (int z = 0; z < boulderGridSize; z++)
                {
                    // World position coordinates
                    float worldX = chunkPosition.x + (x * boulderSpacing);
                    float worldZ = chunkPosition.z + (z * boulderSpacing);

                    // Use a noise function to decide whether to spawn a boulder
                    float noiseValue = Mathf.PerlinNoise((worldX + 1000f) * 0.1f, (worldZ + 1000f) * 0.1f);
                    if (noiseValue < spawnChance)
                    {
                        // Convert world coordinates to chunk-local coordinates
                        float localX = (worldX - chunkPosition.x) / voxelScale;
                        float localZ = (worldZ - chunkPosition.z) / voxelScale;

                        int ix = Mathf.FloorToInt(localX);
                        int iz = Mathf.FloorToInt(localZ);

                        // Get the height at this position from the height map
                        if (ix >= 0 && ix < heightMap.GetLength(0) && iz >= 0 && iz < heightMap.GetLength(1))
                        {
                            int height = heightMap[ix, iz];
                            float worldY = height * voxelScale;

                            // Check if the height is within the desired range
                            if (worldY >= heightRange.x && worldY <= heightRange.y)
                            {
                                // Position in world space
                                Vector3 position = new Vector3(localX * voxelScale, worldY, localZ * voxelScale) + chunkPosition;

                                // Check if position is available
                                if (placementManager.IsPositionAvailable(position, minDistanceBetweenBoulders))
                                {
                                    // Get the constrained rotation
                                    Quaternion rotation = GetConstrainedRotation();

                                    // Instantiate boulder prefab
                                    GameObject boulderInstance = Instantiate(boulderPrefab, position, rotation, chunkObject.transform);

                                    // Adjust scale for variation
                                    float scaleVariation = Random.Range(0.9f, 1.1f);
                                    boulderInstance.transform.localScale *= scaleVariation;

                                    // Register the position
                                    placementManager.RegisterObjectPosition(position);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}