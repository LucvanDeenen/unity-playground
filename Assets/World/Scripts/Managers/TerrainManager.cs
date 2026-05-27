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
    /// Abstract base class for managing a stream of terrain chunks around the player.
    /// <para>
    /// Subclasses only need to implement <see cref="GenerateChunk"/> to define terrain
    /// shape and object placement.  Everything else — chunk lifecycle, visibility culling,
    /// and distance-based unloading — is handled here.
    /// </para>
    /// <para>
    /// Assign a <see cref="NoiseSettings"/> asset in the Inspector to tune terrain noise
    /// without editing code.  If none is assigned a default instance is created at runtime.
    /// </para>
    /// </summary>
    public abstract class TerrainManager : MonoBehaviour
    {
        // ─── Inspector fields ────────────────────────────────────────────────────

        [Header("Player & Camera")]
        [Tooltip("The player's Transform. Chunk streaming is centered on this position.")]
        public Transform player;

        [Tooltip("Camera used for frustum culling. Leave empty to use Camera.main.")]
        [SerializeField] private Camera mainCamera;

        [Header("Terrain")]
        public int seed = 42;

        [Tooltip("Size of each chunk in voxels (one side). Changing at runtime has no effect.")]
        [SerializeField] protected int chunkSize = 32;

        [Tooltip("World-space size of a single voxel.")]
        [SerializeField] protected float voxelScale = 0.75f;

        [Tooltip("How many chunks to keep loaded in each direction from the player's chunk.")]
        [SerializeField] protected int renderDistance = 8;

        public Material  voxelMaterial;
        public Gradient  terrainGradient;
        public Color     wallColor = new Color(0.5f, 0.3f, 0.2f);

        [Header("Noise")]
        [Tooltip("Noise parameters. Create via Assets > Create > World/Noise Settings.")]
        [SerializeField] protected NoiseSettings noiseSettings;

        [Header("Spawners")]
        public List<Spawner> spawners = new List<Spawner>();

        // ─── Runtime state ───────────────────────────────────────────────────────

        protected Dictionary<Vector2Int, TerrainChunk> chunkDictionary
            = new Dictionary<Vector2Int, TerrainChunk>();

        // Initialized to an impossible value so the first Update always triggers a refresh
        protected Vector2Int lastPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        protected ObjectPlacementManager placementManager;
        protected NoiseGenerator         noiseGenerator;
        protected MeshGenerator          meshGenerator;

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        protected virtual void Start()
        {
            if (player == null)
            {
                Debug.LogError($"[{name}] Player reference is not set in TerrainManager.", this);
                enabled = false;
                return;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (noiseSettings == null)
            {
                Debug.LogWarning($"[{name}] No NoiseSettings assigned — using built-in defaults.", this);
                noiseSettings = ScriptableObject.CreateInstance<NoiseSettings>();
            }

            placementManager = GetComponent<ObjectPlacementManager>();
            noiseGenerator   = new NoiseGenerator(seed, noiseSettings);
            meshGenerator    = new MeshGenerator(voxelScale, terrainGradient, wallColor);

            foreach (Spawner spawner in spawners)
            {
                if (spawner == null) continue;
                spawner.SetPlacementManager(placementManager);
                spawner.SetNoiseGenerator(noiseGenerator);
            }

            // Force immediate chunk generation on the first frame
            lastPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
            RefreshChunks();
        }

        protected virtual void Update()
        {
            Vector2Int currentChunkCoord = GetChunkCoordFromPosition(player.position);

            // Only regenerate/unload chunks when the player crosses a chunk boundary —
            // this avoids the heavy loop running every single frame.
            if (currentChunkCoord != lastPlayerChunkCoord)
            {
                lastPlayerChunkCoord = currentChunkCoord;
                RefreshChunks();
            }

            // Visibility culling still updates every frame (cheap renderer toggle)
            UpdateChunkVisibility();
        }

        // ─── Chunk lifecycle ─────────────────────────────────────────────────────

        /// <summary>
        /// Generates newly in-range chunks and unloads those that have gone out of range.
        /// Called only when the player moves to a new chunk coordinate.
        /// </summary>
        protected void RefreshChunks()
        {
            HashSet<Vector2Int> activeCoords = new HashSet<Vector2Int>();

            for (int xOff = -renderDistance; xOff <= renderDistance; xOff++)
            {
                for (int zOff = -renderDistance; zOff <= renderDistance; zOff++)
                {
                    Vector2Int coord = new Vector2Int(
                        lastPlayerChunkCoord.x + xOff,
                        lastPlayerChunkCoord.y + zOff);

                    activeCoords.Add(coord);

                    if (!chunkDictionary.ContainsKey(coord))
                    {
                        TerrainChunk chunk = new TerrainChunk(
                            coord, chunkSize, voxelScale, transform, voxelMaterial);
                        GenerateChunk(chunk);
                        chunkDictionary.Add(coord, chunk);
                    }
                }
            }

            // Remove chunks that are no longer within render distance
            var toRemove = new List<Vector2Int>();
            foreach (var pair in chunkDictionary)
            {
                if (!activeCoords.Contains(pair.Key))
                {
                    UnloadChunk(pair.Value);
                    toRemove.Add(pair.Key);
                }
            }
            foreach (var coord in toRemove)
                chunkDictionary.Remove(coord);
        }

        /// <summary>
        /// Updates renderer/collider visibility for every active chunk.
        /// Called every frame (cheap compared to mesh generation).
        /// </summary>
        protected void UpdateChunkVisibility()
        {
            foreach (var chunk in chunkDictionary.Values)
                chunk.UpdateVisibility(player, mainCamera);
        }

        // ─── Abstract / virtual interface ────────────────────────────────────────

        /// <summary>
        /// Override to define how a chunk's terrain mesh and spawned objects are created.
        /// Use <see cref="noiseGenerator"/>, <see cref="meshGenerator"/>, and
        /// <see cref="spawners"/> as needed.
        /// </summary>
        protected abstract void GenerateChunk(TerrainChunk chunk);

        // ─── Utility ─────────────────────────────────────────────────────────────

        /// <summary>Converts a world position to chunk grid coordinates.</summary>
        protected Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
            int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Converts a <c>float[,]</c> height map to an <c>int[,]</c> height map.
        /// Spawners work in voxel-integer space, while the mesh generator works in floats.
        /// </summary>
        protected static int[,] ToIntHeightMap(float[,] floatMap)
        {
            int w = floatMap.GetLength(0);
            int h = floatMap.GetLength(1);
            int[,] result = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    result[x, z] = Mathf.RoundToInt(floatMap[x, z]);
            return result;
        }

        /// <summary>
        /// Deregisters all objects placed in the chunk and destroys its GameObject.
        /// </summary>
        protected void UnloadChunk(TerrainChunk chunk)
        {
            if (placementManager != null)
            {
                foreach (Transform child in chunk.chunkObject.transform)
                    placementManager.DeregisterObjectPosition(child.position);
            }
            chunk.DestroyChunk();
        }
    }
}
