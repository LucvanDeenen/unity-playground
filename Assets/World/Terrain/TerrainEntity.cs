using UnityEngine;
using System.Collections.Generic;
using World.Shared;

namespace World.Terrain
{
    /// <summary>
    /// Represents a single terrain entity.
    /// </summary>
    public class TerrainEntity
    {
        public Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        private ObjectPlacementManager placementManager;
        private int chunkSize;
        private float voxelScale;

        public TerrainEntity(ObjectPlacementManager placementManager, int chunkSize = 32, float voxelScale = 0.75f)
        {
            this.placementManager = placementManager;
            this.voxelScale = voxelScale;
            this.chunkSize = chunkSize;
        }

        /// <summary>
        /// Converts a world position to chunk coordinates.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <returns>The corresponding chunk coordinates.</returns>
        public Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
            int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Validates all active terrain chunk and cleans up.
        /// </summary>
        /// <param name="chunk">The terrain chunk to unload.</param>
        public void ValidateChunks(HashSet<Vector2Int> activeChunks, Transform player)
        {
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var chunk in chunks)
            {
                if (!activeChunks.Contains(chunk.Key))
                {
                    UnloadChunk(chunk.Value);
                    chunksToRemove.Add(chunk.Key);
                }
                chunk.Value.UpdateVisibility(player);
            }

            foreach (var coord in chunksToRemove)
            {
                chunks.Remove(coord);
            }
        }

        private void UnloadChunk(Chunk chunk)
        {
            foreach (Transform child in chunk.chunkObject.transform)
            {
                placementManager.DeregisterObjectPosition(child.position);
            }

            chunk.DestroyChunk();
        }
    }
}
