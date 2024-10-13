// using UnityEngine;

// namespace World.Spawners
// {
//     /// <summary>
//     /// Handles spawning of foliage (e.g., grass) on the voxel terrain.
//     /// </summary>
//     public class FoliageSpawner : Spawner
//     {
//         [Header("Foliage Settings")]
//         [Tooltip("The grass prefab to spawn on the terrain.")]
//         public GameObject grassPrefab;

//         private Vector2 heightRange = new Vector2(5f, 40f);
//         private float minDistanceBetweenGrass = 2f;
//         private int grassDensity = 2;


//         /// <summary>
//         /// Spawns grass on the given chunk.
//         /// </summary>
//         public override void Spawn(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord)
//         {
//             if (grassPrefab == null)
//             {
//                 Debug.LogError("Grass prefab is not assigned in FoliageSpawner.");
//                 return;
//             }

//             if (placementManager == null)
//             {
//                 Debug.LogError("ObjectPlacementManager reference is not set in FoliageSpawner.");
//                 return;
//             }

//             // Get the chunk position
//             Vector3 chunkPosition = chunkObject.transform.position;

//             // Loop through positions within the chunk
//             for (int i = 0; i < grassDensity; i++)
//             {
//                 // Random position within the chunk
//                 float x = Random.Range(0, chunkSize);
//                 float z = Random.Range(0, chunkSize);

//                 int ix = Mathf.FloorToInt(x);
//                 int iz = Mathf.FloorToInt(z);

//                 // Get the height at this position from the height map
//                 if (ix >= 0 && ix < heightMap.GetLength(0) && iz >= 0 && iz < heightMap.GetLength(1))
//                 {
//                     int height = heightMap[ix, iz];
//                     float worldY = height * voxelScale;

//                     // Check if the height is within the desired range
//                     if (worldY >= heightRange.x && worldY <= heightRange.y)
//                     {
//                         // Position in world space
//                         Vector3 position = new Vector3(x * voxelScale, worldY, z * voxelScale) + chunkPosition;

//                         // Check if position is available
//                         if (placementManager.IsPositionAvailable(position, minDistanceBetweenGrass))
//                         {
//                             // Instantiate grass prefab
//                             GameObject grassInstance = Instantiate(grassPrefab, position, Quaternion.identity, chunkObject.transform);

//                             // Adjust rotation and scale for variation
//                             grassInstance.transform.Rotate(-90f, Random.Range(0f, 360f), 0f);
//                             float scaleVariation = Random.Range(0.8f, 1.2f);
//                             grassInstance.transform.localScale *= scaleVariation;

//                             // Register the position
//                             placementManager.RegisterObjectPosition(position);
//                         }
//                     }
//                 }
//             }
//         }
//     }
// }