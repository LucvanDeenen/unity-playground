using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the starting terrain with cliffs around the player.
/// </summary>
public class StartingTerrainManager : TerrainManager
{
    [Header("Cliff Settings")]
    public float lowerRadius = 35f;      
    public float cliffWidth = 2f;        
    public float heightOffset = 15f;     

    protected override void GenerateChunk(TerrainChunk chunk)
    {
        // Generate isCliffArea as bool[,] and height map as float[,]
        float[,] heightMapFloat = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);
        bool[,] isCliffArea = new bool[chunk.chunkSize + 1, chunk.chunkSize + 1];

        // Adjust heights to create cliffs around lowered area and smooth terrain at the bottom
        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                // Compute world position
                float worldX = (chunk.chunkCoord.x * chunk.chunkSize + x) * voxelScale;
                float worldZ = (chunk.chunkCoord.y * chunk.chunkSize + z) * voxelScale;

                float dx = worldX - player.position.x;
                float dz = worldZ - player.position.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance <= lowerRadius)
                {
                    float smoothNoise = noiseGenerator.GenerateSmoothNoise(worldX, worldZ);
                    heightMapFloat[x, z] = smoothNoise + heightOffset;
                    isCliffArea[x, z] = false;
                }
                else if (distance <= lowerRadius + cliffWidth)
                {
                    float t = (distance - lowerRadius) / cliffWidth;
                    float loweredHeight = noiseGenerator.GenerateSmoothNoise(worldX, worldZ) + heightOffset;
                    heightMapFloat[x, z] = Mathf.Lerp(loweredHeight, heightMapFloat[x, z], t);
                    isCliffArea[x, z] = true; 
                }
                else
                {
                    isCliffArea[x, z] = false;
                    // Keep the original height
                }
            }
        }

        // Convert float[,] heightMap to int[,]
        int[,] heightMapInt = new int[chunk.chunkSize + 1, chunk.chunkSize + 1];
        for (int x = 0; x <= chunk.chunkSize; x++)
        {
            for (int z = 0; z <= chunk.chunkSize; z++)
            {
                heightMapInt[x, z] = Mathf.RoundToInt(heightMapFloat[x, z]);
            }
        }

        // Generate mesh data using the adjusted heightMap and isCliffArea
        MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat, isCliffArea);

        // Update chunk mesh
        chunk.UpdateChunkMesh(meshData);

        // Spawn objects using the int[,] heightMap
        foreach (Spawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.Spawn(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
            }
        }
    }
}
