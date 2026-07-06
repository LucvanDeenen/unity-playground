using UnityEngine;
using UnityEngine.Rendering;
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
        [Tooltip("World size of one block; around half the player's height gives the Cube World feel.")]
        public float voxelScale = 1f;
        [Tooltip("Terrain snaps to flat treads with risers of this many blocks; 0 disables terracing.")]
        [Range(0f, 8f)] public float terraceHeight = 4f;
        [Tooltip("Color of tall exposed walls; shallow steps keep the surface color.")]
        public Color rockColor = new Color(0.55f, 0.55f, 0.58f);

        [Header("Paths")]
        [Tooltip("Weave winding trails through the terrain colors.")]
        public bool generatePaths = true;
        public Color pathColor = new Color(0.93f, 0.8f, 0.5f);
        [Tooltip("Approximate trail width in blocks.")]
        [Range(1f, 8f)] public float pathWidth = 5f;
        [Tooltip("How many blocks trails sink below the surrounding terrain.")]
        [Range(0f, 3f)] public float pathDepth = 1f;

        [Header("Biome Settings")]
        [Tooltip("Leave empty to use the built-in defaults. Right-click the component header and choose 'Populate Default Biomes' to edit them here.")]
        public List<BiomeDefinition> biomes = new List<BiomeDefinition>();
        [Tooltip("Height in blocks reserved as the future water level; unused until water is added.")]
        public float seaLevel = 8f;

        [Header("Spawners")]
        public List<Spawner> spawners = new List<Spawner>();

        [Header("Atmosphere")]
        [Tooltip("Configure linear distance fog matched to the loaded chunk radius, hiding chunk and vegetation pop-in.")]
        public bool configureFog = true;
        public Color fogColor = new Color(0.55f, 0.7f, 0.97f);
        [Range(0f, 1f)] public float fogStartFraction = 0.45f;
        [Range(0f, 1f)] public float fogEndFraction = 0.92f;
        [Tooltip("Set a sky/ground gradient ambient so block faces get colored bounce light.")]
        public bool configureAmbientLight = true;

        protected Dictionary<Vector2Int, TerrainChunk> chunkDictionary = new Dictionary<Vector2Int, TerrainChunk>();
        protected Vector2Int lastPlayerChunkCoord;

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
            biomeGenerator = new BiomeGenerator(noiseGenerator, biomes, seaLevel, generatePaths, pathColor, pathWidth, pathDepth, terraceHeight);
            meshGenerator = new MeshGenerator(voxelScale, rockColor);

            foreach (Spawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.SetPlacementManager(placementManager);
                    spawner.SetNoiseGenerator(noiseGenerator);
                }
            }

            ApplyAtmosphere();

            lastPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
            RefreshPendingChunks(lastPlayerChunkCoord);

            // Generate the closest ring synchronously so the player starts on solid ground.
            GeneratePendingChunks(9);
        }

        /// <summary>
        /// Sets up distance fog so terrain fades out just inside the loaded chunk
        /// radius instead of popping in at the edge.
        /// </summary>
        private void ApplyAtmosphere()
        {
            if (configureFog)
            {
                float loadedRadius = renderDistance * chunkSize * voxelScale;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogStartDistance = loadedRadius * fogStartFraction;
                RenderSettings.fogEndDistance = loadedRadius * fogEndFraction;

                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    // Everything past the loaded radius is solid fog; no need to render further.
                    mainCamera.farClipPlane = loadedRadius * 1.15f;
                }
            }

            if (configureAmbientLight)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.52f, 0.62f, 0.8f);
                RenderSettings.ambientEquatorColor = new Color(0.46f, 0.44f, 0.38f);
                RenderSettings.ambientGroundColor = new Color(0.27f, 0.24f, 0.2f);
            }
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
