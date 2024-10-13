using UnityEngine;
using World.MeshGeneration;
using World.Chunks;
using World.Spawners;

namespace World.Managers
{
    /// <summary>
    /// Manages world terrain chunks around the player.
    /// </summary>
    public class WorldTerrainManager : TerrainManager
    {
        protected override void Start()
        {
            renderDistance = 12;
            base.Start();
        }

        void Update()
        {
            UpdateChunks();
        }

        protected override void GenerateChunk(TerrainChunk chunk)
        {
            // Generate height map
            float[,] heightMapFloat = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);

            // Convert float[,] heightMap to int[,]
            int[,] heightMapInt = new int[chunk.chunkSize + 1, chunk.chunkSize + 1];
            for (int x = 0; x <= chunk.chunkSize; x++)
            {
                for (int z = 0; z <= chunk.chunkSize; z++)
                {
                    heightMapInt[x, z] = Mathf.RoundToInt(heightMapFloat[x, z]);
                }
            }

            // Generate mesh data
            MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat);

            // Update chunk mesh
            chunk.UpdateChunkMesh(meshData);

            // Spawn objects
            foreach (Spawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.Spawn(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
                }
            }
        }
    }
}
