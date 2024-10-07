using UnityEngine;

/// <summary>
/// Handles spawning of boulders on the voxel terrain.
/// </summary>
public class BoulderSpawner : MonoBehaviour
{
    [Header("Boulder Settings")]
    [Tooltip("The boulder prefab to spawn on the terrain.")]
    public GameObject boulderPrefab;

    [Tooltip("The chance to spawn a boulder at a potential location (0 to 1).")]
    [Range(0f, 1f)]
    public float spawnChance = 0.05f;

    [Tooltip("The height range for spawning boulders.")]
    public Vector2 heightRange = new Vector2(10f, 50f);

    [Tooltip("Spacing between boulders (in world units).")]
    public float boulderSpacing = 10f;

    [Tooltip("Minimum distance between boulders and other objects.")]
    public float minDistanceBetweenBoulders = 5f;

    [Tooltip("Reference to the ObjectPlacementManager.")]
    public ObjectPlacementManager placementManager;

    /// <summary>
    /// Spawns boulders on the given chunk.
    /// </summary>
    public void SpawnBoulders(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord)
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
                                // Instantiate boulder prefab
                                GameObject boulderInstance = Instantiate(boulderPrefab, position, Quaternion.identity, chunkObject.transform);

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
