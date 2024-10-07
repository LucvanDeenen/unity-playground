using UnityEngine;

/// <summary>
/// Handles spawning of trees on the voxel terrain.
/// </summary>
public class TreeSpawner : Spawner
{
    [Header("Tree Settings")]
    [Tooltip("The list of tree prefabs to spawn on the terrain.")]
    public GameObject[] treePrefabs;

    private Vector2 heightRange = new Vector2(10f, 35f);
    private float minDistanceBetweenTrees = 5f;
    private float spawnChance = 0.1f;

    /// <summary>
    /// Spawns trees on the given chunk.
    /// </summary>
    public override void Spawn(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord)
    {
        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("Tree prefabs are not assigned in TreeSpawner.");
            return;
        }

        if (placementManager == null)
        {
            Debug.LogError("ObjectPlacementManager reference is not set in TreeSpawner.");
            return;
        }

        // Get the chunk position
        Vector3 chunkPosition = chunkObject.transform.position;

        // Loop through positions within the chunk
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // World position coordinates
                float worldX = chunkPosition.x + (x * voxelScale);
                float worldZ = chunkPosition.z + (z * voxelScale);

                // Use a noise function to decide whether to spawn a tree
                float noiseValue = Mathf.PerlinNoise((worldX + 500f) * 0.05f, (worldZ + 500f) * 0.05f);
                if (noiseValue < spawnChance)
                {
                    int height = heightMap[x, z];
                    float worldY = height * voxelScale;

                    // Check if the height is within the desired range
                    if (worldY >= heightRange.x && worldY <= heightRange.y)
                    {
                        // Position in world space
                        Vector3 position = new Vector3(worldX, worldY, worldZ);

                        // Check if position is available
                        if (placementManager.IsPositionAvailable(position, minDistanceBetweenTrees))
                        {
                            // Randomly select a tree prefab
                            GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];

                            // Instantiate tree prefab with constrained rotation
                            GameObject treeInstance = Instantiate(treePrefab, position, GetConstrainedRotation(), chunkObject.transform);

                            // Optionally, adjust scale for variation
                            float scaleVariation = Random.Range(0.9f, 1.1f);
                            treeInstance.transform.localScale *= scaleVariation;

                            // Register the position
                            placementManager.RegisterObjectPosition(position);
                        }
                    }
                }
            }
        }
    }
}
