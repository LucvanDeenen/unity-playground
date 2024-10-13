using UnityEngine;
using System.Collections.Generic;
using World.MeshGeneration;
using World.NoiseGeneration;
using World.Chunks;
using World.Spawners;
using World.Shared;

namespace World.Managers
{
    /// <summary>
    /// Abstract base class for managing terrain chunks around the player.
    /// </summary>
    public abstract class TerrainManager : MonoBehaviour
    {
        [Header("Player Settings")]
        public Transform player;

        [Header("Terrain Settings")]
        public int seed = 42;
        public Material voxelMaterial;
        public Gradient terrainGradient;
        public Color wallColor = new Color(0.5f, 0.3f, 0.2f);

        [Header("Spawners")]
        public List<Spawner> spawners = new List<Spawner>();

        protected Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
        protected Vector2Int lastPlayerChunkCoord;

        protected float voxelScale = 0.75f;
        protected int renderDistance = 2;
        protected int chunkSize = 32;

        protected ObjectPlacementManager placementManager;
        protected NoiseGenerator noiseGenerator;
        protected MeshGenerator meshGenerator;

        protected virtual void Start()
        {
            if (player == null)
            {
                Debug.LogError("Player reference is not set in TerrainManager.");
                enabled = false;
                return;
            }

            placementManager = GetComponent<ObjectPlacementManager>();

            // Initialize NoiseGenerator and MeshGenerator with parameters
            noiseGenerator = new NoiseGenerator(seed);
            meshGenerator = new MeshGenerator(voxelScale, terrainGradient, wallColor);

            foreach (Spawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.SetPlacementManager(placementManager);
                    spawner.SetNoiseGenerator(noiseGenerator);
                }
            }

            lastPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
            UpdateChunks();
        }

        /// <summary>
        /// Updates the active terrain chunks based on the player's current position.
        /// </summary>
        protected void UpdateChunks()
        {
            Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

            HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

            for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
            {
                for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + xOffset, playerChunkCoord.y + zOffset);
                    activeChunks.Add(chunkCoord);

                    if (!chunkDictionary.ContainsKey(chunkCoord))
                    {
                        // Generate and store new chunk.
                        TerrainChunk chunk = new TerrainChunk(chunkCoord, chunkSize, voxelScale, transform, voxelMaterial);
                        GenerateChunk(chunk);
                        chunkDictionary.Add(chunkCoord, chunk);
                    }
                }
            }

            // Remove chunks that are no longer within the render distance.
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var chunk in chunkDictionary)
            {
                if (!activeChunks.Contains(chunk.Key))
                {
                    UnloadChunk(chunk.Value);
                    chunksToRemove.Add(chunk.Key);
                }
            }
            foreach (var coord in chunksToRemove)
            {
                chunkDictionary.Remove(coord);
            }
        }

        /// <summary>
        /// Abstract method to generate a terrain chunk. Must be implemented by derived classes.
        /// </summary>
        /// <param name="chunk">The terrain chunk to generate.</param>
        protected abstract void GenerateChunk(TerrainChunk chunk);

        /// <summary>
        /// Converts a world position to chunk coordinates.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <returns>The corresponding chunk coordinates.</returns>
        protected Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
            int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Unloads a terrain chunk and cleans up associated objects.
        /// </summary>
        /// <param name="chunk">The terrain chunk to unload.</param>
        protected void UnloadChunk(TerrainChunk chunk)
        {
            // Deregister object positions in the chunk
            foreach (Transform child in chunk.chunkObject.transform)
            {
                placementManager.DeregisterObjectPosition(child.position);
            }

            // Destroy the chunk object
            chunk.DestroyChunk();
        }
    }
}
