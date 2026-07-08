using UnityEngine;
using World.Biomes;
using World.Chunks;
using World.Spawners;

namespace World.Managers
{
    /// <summary>
    /// Manages world terrain chunks around the player.
    /// </summary>
    public class WorldTerrainManager : TerrainManager
    {
        void Update()
        {
            UpdateChunks();
        }

        protected override void GenerateChunk(TerrainChunk chunk)
        {
            // Generate biome-blended heights and colors
            ChunkData chunkData = biomeGenerator.GenerateChunkData(chunk.chunkCoord, chunk.chunkSize);

            // Build and apply the chunk mesh
            chunk.UpdateChunkMesh(meshGenerator.GenerateMeshData(chunkData));

            // Spawn objects
            if (!spawnVegetation)
            {
                return;
            }

            foreach (Spawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.Spawn(chunk.chunkObject, chunkData, voxelScale, chunk.chunkCoord);
                }
            }
        }
    }
}
