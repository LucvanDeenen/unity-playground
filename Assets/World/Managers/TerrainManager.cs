using UnityEngine;
using System.Collections.Generic;
using World.MeshGeneration;
using World.NoiseGeneration;
using World.Spawners;
using World.Shared;
using World.Chunks;

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
        [SerializeField] private float voxelScale = 0.75f;
        [SerializeField] private int chunkSize = 32;
        [SerializeField] protected int seed = 42;
        [SerializeField] protected int renderDistance = 12;

        [Header("Materials")]
        public Material voxelMaterial;
        public Gradient gradient;
        public Color wall;

        [Header("Spawners")]
        public List<Spawner> spawners = new List<Spawner>();

        protected ObjectPlacementManager placementManager;
        protected NoiseGenerator noiseGenerator;
        protected MeshGenerator meshGenerator;
        protected Terrain terrain;

        protected virtual void Start()
        {
            if (player == null)
            {
                enabled = false;
                return;
            }

            placementManager = GetComponent<ObjectPlacementManager>();
            meshGenerator = new MeshGenerator(voxelScale, gradient, wall);
            noiseGenerator = new NoiseGenerator(seed);
            terrain = new Terrain(placementManager, chunkSize, voxelScale);
            foreach (Spawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.SetPlacementManager(placementManager);
                    spawner.SetNoiseGenerator(noiseGenerator);
                }
            }

            UpdateChunks();
        }

        protected virtual void Update()
        {
            UpdateChunks();
        }

        /// <summary>
        /// Updates the active terrain chunks based on the player's current position.
        /// </summary>
        protected void UpdateChunks()
        {
            Vector2Int playerCoord = terrain.GetChunkCoordFromPosition(player.position);

            // Validate chunks that need to be added.
            HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
            for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
            {
                for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerCoord.x + xOffset, playerCoord.y + zOffset);
                    activeChunks.Add(chunkCoord);
                    if (!terrain.chunks.ContainsKey(chunkCoord))
                    {
                        // Generate and store new chunk.
                        TerrainChunk chunk = new TerrainChunk(chunkCoord, chunkSize, voxelScale, transform, voxelMaterial);
                        GenerateTerrain(chunk);
                        terrain.chunks.Add(chunkCoord, chunk);
                    }
                }
            }

            terrain.ValidateChunks(activeChunks, player);
        }

        /// <summary>
        /// Abstract method to generate a terrain chunk. Must be implemented by derived classes.
        /// </summary>
        /// <param name="chunk">The terrain chunk to generate.</param>
        protected virtual void GenerateTerrain(TerrainChunk chunk)
        {
            // Generate height map
            float[,] heightMapFloat = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);

            // Generate mesh data
            MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat);

            // Update chunk mesh
            chunk.UpdateChunkMesh(meshData);
        }
    }
}
