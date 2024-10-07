using UnityEngine;

/// <summary>
/// Handles spawning of foliage (e.g., grass) on the voxel terrain.
/// </summary>
public class FoliageSpawner : MonoBehaviour
{
    [Header("Foliage Settings")]
    [Tooltip("The grass prefab to spawn on the terrain.")]
    public GameObject grassPrefab;

    [Tooltip("The density of grass per chunk.")]
    [Range(0, 500)]
    public int grassDensity = 5;

    [Tooltip("The height range for spawning grass (e.g., plains).")]
    public Vector2 heightRange = new Vector2(5f, 40f);

    [Tooltip("Minimum distance between grass and other objects.")]
    public float minDistanceBetweenGrass = 1f;

    [Tooltip("Reference to the ObjectPlacementManager.")]
    public ObjectPlacementManager placementManager;

    /// <summary>
    /// Spawns grass on the given chunk.
    /// </summary>
    public void SpawnGrass(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize)
    {
        if (grassPrefab == null)
        {
            Debug.LogError("Grass prefab is not assigned in FoliageSpawner.");
            return;
        }

        if (placementManager == null)
        {
            Debug.LogError("ObjectPlacementManager reference is not set in FoliageSpawner.");
            return;
        }

        // Get the chunk position
        Vector3 chunkPosition = chunkObject.transform.position;

        // Loop through positions within the chunk
        for (int i = 0; i < grassDensity; i++)
        {
            // Random position within the chunk
            float x = Random.Range(0, chunkSize);
            float z = Random.Range(0, chunkSize);

            int ix = Mathf.FloorToInt(x);
            int iz = Mathf.FloorToInt(z);

            // Get the height at this position from the height map
            if (ix >= 0 && ix < heightMap.GetLength(0) && iz >= 0 && iz < heightMap.GetLength(1))
            {
                int height = heightMap[ix, iz];
                float worldY = height * voxelScale;

                // Check if the height is within the desired range
                if (worldY >= heightRange.x && worldY <= heightRange.y)
                {
                    // Position in world space
                    Vector3 position = new Vector3(x * voxelScale, worldY, z * voxelScale) + chunkPosition;

                    // Check if position is available
                    if (placementManager.IsPositionAvailable(position, minDistanceBetweenGrass))
                    {
                        // Instantiate grass prefab
                        GameObject grassInstance = Instantiate(grassPrefab, position, Quaternion.identity, chunkObject.transform);

                        // Adjust rotation and scale for variation
                        grassInstance.transform.Rotate(-90f, Random.Range(0f, 360f), 0f);
                        float scaleVariation = Random.Range(0.8f, 1.2f);
                        grassInstance.transform.localScale *= scaleVariation;

                        // Register the position
                        placementManager.RegisterObjectPosition(position);
                    }
                }
            }
        }
    }
}
