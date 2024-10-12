// using UnityEngine;
// using World.MeshGeneration;
// using World.Chunks;
// using World.Spawners;

// namespace World.Managers
// {
//     /// <summary>
//     /// Manages the starting terrain with a single, continuous valley around the player.
//     /// </summary>
//     public class StartingTerrainManager : TerrainManager
//     {
//         protected override void GenerateChunk(TerrainChunk chunk)
//         {
//             // Generate isValleyArea as bool[,] and height map as float[,]
//             float[,] heightMapFloat = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);
//             bool[,] isValleyArea = new bool[chunk.chunkSize + 1, chunk.chunkSize + 1];
    
//             // Adjust heights to create valleys and smooth terrain at the bottom
//             for (int x = 0; x <= chunk.chunkSize; x++)
//             {
//                 for (int z = 0; z <= chunk.chunkSize; z++)
//                 {
//                     // Compute world position
//                     float worldX = (chunk.chunkCoord.x * chunk.chunkSize + x) * voxelScale;
//                     float worldZ = (chunk.chunkCoord.y * chunk.chunkSize + z) * voxelScale;
    
//                     float dx = worldX - player.position.x;
//                     float dz = worldZ - player.position.z;
//                     float distance = Mathf.Sqrt(dx * dx + dz * dz);
    
//                     {
//                         float t = (distance - lowerRadius) / valleyWidth;
//                         float loweredHeight = noiseGenerator.GenerateSmoothNoise(worldX, worldZ) + heightOffset;
//                         heightMapFloat[x, z] = Mathf.Lerp(loweredHeight, heightMapFloat[x, z], t);
//                         isValleyArea[x, z] = true; 
//                     }
//                     else
//                     {
//                         isValleyArea[x, z] = false;
//                         // Keep the original height
//                     }
//                 }
//             }
    
//             // Convert float[,] heightMap to int[,]
//             int[,] heightMapInt = new int[chunk.chunkSize + 1, chunk.chunkSize + 1];
//             for (int x = 0; x <= chunk.chunkSize; x++)
//             {
//                 for (int z = 0; z <= chunk.chunkSize; z++)
//                 {
//                     heightMapInt[x, z] = Mathf.RoundToInt(heightMapFloat[x, z]);
//                 }
//             }
    
//             // Generate mesh data using the adjusted heightMap and isValleyArea
//             MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat, isValleyArea);
    
//             // Update chunk mesh
//             chunk.UpdateChunkMesh(meshData);
    
//             // Spawn objects using the int[,] heightMap
//             foreach (Spawner spawner in spawners)
//             {
//                 if (spawner != null)
//                 {
//                     spawner.Spawn(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
//                 }
//             }
//         }
//     }
// }
