using UnityEngine;
using System.Collections.Generic;
using World.Biomes;
using World.MeshGeneration;
using World.NoiseGeneration;
using World.Chunks;
using World.Spawners;
using World.Shared;

namespace World.Managers
{
    /// <summary>
    /// Abstract base class for managing terrain chunks around the player.
    /// Chunk generation is time-sliced: missing chunks are queued nearest-first
    /// and generated a few per frame to avoid hitches when crossing chunk borders.
    /// </summary>
    public abstract class TerrainManager : MonoBehaviour
    {
        [Header("Player Settings")]
        public Transform player;

        [Header("Terrain Settings")]
        public int seed = 42;
        public Material voxelMaterial;
        [Tooltip("How many chunks may be generated per frame.")]
        public int chunksPerFrame = 4;

        [Header("Biome Settings")]
        [Tooltip("Leave empty to use the built-in defaults. Right-click the component header and choose 'Populate Default Biomes' to edit them here.")]
        public List<BiomeDefinition> biomes = new List<BiomeDefinition>();
        [Tooltip("Height in blocks reserved as the future water level; unused until water is added.")]
        public float seaLevel = 8f;

        [Header("Spawners")]
        public List<Spawner> spawners = new List<Spawner>();

        protected Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
        protected Vector2Int lastPlayerChunkCoord;

        protected float voxelScale = 0.75f;
        protected int renderDistance = 2;
        protected int chunkSize = 32;

        protected ObjectPlacementManager placementManager;
        protected NoiseGenerator noiseGenerator;
        protected BiomeGenerator biomeGenerator;
        protected MeshGenerator meshGenerator;

        private readonly Queue<Vector2Int> pendingChunks = new Queue<Vector2Int>();
        private readonly List<Vector2Int> missingChunkBuffer = new List<Vector2Int>();

        protected virtual void Start()
        {
            if (player == null)
            {
                Debug.LogError("Player reference is not set in TerrainManager.");
                enabled = false;
                return;
            }

            placementManager = GetComponent<ObjectPlacementManager>();

            noiseGenerator = new NoiseGenerator(seed);
            if (biomes == null || biomes.Count == 0)
            {
                biomes = BiomeDefaults.CreateDefaults();
            }
            biomeGenerator = new BiomeGenerator(noiseGenerator, biomes, seaLevel);
            meshGenerator = new MeshGenerator(voxelScale);

            foreach (Spawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.SetPlacementManager(placementManager);
                    spawner.SetNoiseGenerator(noiseGenerator);
                }
            }

            lastPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
            RefreshPendingChunks(lastPlayerChunkCoord);

            // Generate the closest ring synchronously so the player starts on solid ground.
            GeneratePendingChunks(9);
        }

        /// <summary>
        /// Updates the active terrain chunks based on the player's current position.
        /// Call once per frame.
        /// </summary>
        protected void UpdateChunks()
        {
            Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

            if (playerChunkCoord != lastPlayerChunkCoord)
            {
                lastPlayerChunkCoord = playerChunkCoord;
                RefreshPendingChunks(playerChunkCoord);
                UnloadDistantChunks(playerChunkCoord);
            }

            GeneratePendingChunks(chunksPerFrame);
        }

        /// <summary>
        /// Rebuilds the queue of chunks that are in range but not yet generated, nearest first.
        /// </summary>
        private void RefreshPendingChunks(Vector2Int center)
        {
            pendingChunks.Clear();
            missingChunkBuffer.Clear();

            for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
            {
                for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
                {
                    Vector2Int chunkCoord = new Vector2Int(center.x + xOffset, center.y + zOffset);
                    if (!chunkDictionary.ContainsKey(chunkCoord))
                    {
                        missingChunkBuffer.Add(chunkCoord);
                    }
                }
            }

            missingChunkBuffer.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));

            foreach (Vector2Int chunkCoord in missingChunkBuffer)
            {
                pendingChunks.Enqueue(chunkCoord);
            }
        }

        /// <summary>
        /// Generates up to the given number of queued chunks.
        /// </summary>
        private void GeneratePendingChunks(int budget)
        {
            while (budget > 0 && pendingChunks.Count > 0)
            {
                Vector2Int chunkCoord = pendingChunks.Dequeue();
                if (chunkDictionary.ContainsKey(chunkCoord))
                {
                    continue;
                }

                TerrainChunk chunk = new TerrainChunk(chunkCoord, chunkSize, voxelScale, transform, voxelMaterial);
                GenerateChunk(chunk);
                chunkDictionary.Add(chunkCoord, chunk);
                budget--;
            }
        }

        /// <summary>
        /// Unloads chunks that are no longer within the render distance.
        /// </summary>
        private void UnloadDistantChunks(Vector2Int center)
        {
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var pair in chunkDictionary)
            {
                Vector2Int offset = pair.Key - center;
                if (Mathf.Abs(offset.x) > renderDistance || Mathf.Abs(offset.y) > renderDistance)
                {
                    UnloadChunk(pair.Value);
                    chunksToRemove.Add(pair.Key);
                }
            }

            foreach (Vector2Int chunkCoord in chunksToRemove)
            {
                chunkDictionary.Remove(chunkCoord);
            }
        }

        [ContextMenu("Populate Default Biomes")]
        private void PopulateDefaultBiomes()
        {
            biomes = BiomeDefaults.CreateDefaults();
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
