using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World.NoiseGeneration;

namespace World.Generation
{
    public class WorldGeneration : MonoBehaviour
    {
        [Header("Player Settings")]
        public Transform player;


        [Header("Terrain Settings")]
        public GameObject chunkPrefab;
        [SerializeField] private float voxelScale = 0.75f;
        [SerializeField] protected int seed = 42;
        [SerializeField] protected int renderDistance = 12;
        [SerializeField] protected float heightMultiplier = 15f;
        [SerializeField] private int chunkSize = 32;

        [Header("Materials")]
        public Material voxelMaterial;
        public Gradient gradient;
        public Color wall;

        protected NoiseGenerator noiseGenerator;

        [Header("Debugging")]
        public Vector2Int currentChunkCoord;
        public Queue<GameObject> chunkPool = new Queue<GameObject>();
        public Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();

        void Start()
        {
            if (player == null)
            {
                enabled = false;
                return;
            }

            noiseGenerator = new NoiseGenerator(seed);
            noiseGenerator.SetHeightMultiplier(heightMultiplier);

            currentChunkCoord = GetChunkCoordFromPosition(player.position);
            LoadChunks();
            UnloadChunks();

            StartCoroutine(UpdateChunks());
        }

        IEnumerator UpdateChunks()
        {
            while (true)
            {
                Vector2Int newChunkCoord = GetChunkCoordFromPosition(player.position);

                if (!newChunkCoord.Equals(currentChunkCoord))
                {
                    currentChunkCoord = newChunkCoord;
                    LoadChunks();
                    UnloadChunks();
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / (chunkSize * voxelScale));
            int z = Mathf.FloorToInt(position.z / (chunkSize * voxelScale));
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Loads chunks around the current chunk within the view distance.
        /// </summary>
        void LoadChunks()
        {
            for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
            {
                for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
                {
                    Vector2Int chunkCoord = new Vector2Int(currentChunkCoord.x + xOffset, currentChunkCoord.y + zOffset);

                    if (!chunks.ContainsKey(chunkCoord))
                    {
                        StartCoroutine(LoadChunkAsync(chunkCoord));
                    }
                }
            }
        }

        /// <summary>
        /// Unloads chunks that are outside the view distance.
        /// </summary>
        void UnloadChunks()
        {
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();

            foreach (var chunk in chunks)
            {
                int distanceX = Mathf.Abs(chunk.Key.x - currentChunkCoord.x);
                int distanceZ = Mathf.Abs(chunk.Key.y - currentChunkCoord.y);

                if (distanceX > renderDistance || distanceZ > renderDistance)
                {
                    chunksToUnload.Add(chunk.Key);
                }
            }

            foreach (var chunkCoord in chunksToUnload)
            {
                UnloadChunk(chunkCoord);
            }
        }

        /// <summary>
        /// Asynchronously loads a single chunk at the specified coordinates.
        /// </summary>
        /// <param name="chunkCoord">Chunk coordinates</param>
        IEnumerator LoadChunkAsync(Vector2Int chunkCoord)
        {
            Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkSize * voxelScale, 0, chunkCoord.y * chunkSize * voxelScale);
            GameObject newChunk;
            if (chunkPool.Count > 0)
            {
                newChunk = chunkPool.Dequeue();
                newChunk.SetActive(true);
                newChunk.transform.position = chunkPosition;
            }
            else
            {
                newChunk = Instantiate(chunkPrefab, chunkPosition, Quaternion.identity, this.transform);
            }

            newChunk.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";

            Chunk chunkBehaviour = newChunk.GetComponent<Chunk>();
            chunkBehaviour.SetNoiseGenerator(noiseGenerator);
            chunkBehaviour.SetChunkSize(chunkSize);
            chunkBehaviour.SetChunkCoord(chunkCoord);
            chunkBehaviour.SetVoxelScale(voxelScale);

            chunkBehaviour.ResetMesh();

            // Start chunk generation without blocking
            yield return StartCoroutine(chunkBehaviour.GenerateChunk());

            chunks.Add(chunkCoord, newChunk);
        }

        /// <summary>
        /// Unloads (destroys) a single chunk at the specified coordinates.
        /// </summary>
        /// <param name="chunkCoord">Chunk coordinates</param>
        void UnloadChunk(Vector2Int chunkCoord)
        {
            if (chunks.ContainsKey(chunkCoord))
            {
                GameObject chunk = chunks[chunkCoord];
                chunk.SetActive(false);
                chunkPool.Enqueue(chunk);
                chunks.Remove(chunkCoord);
            }
        }
    }
}