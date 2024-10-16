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
        public Vector2Int currentChunkCoord;


        [Header("Terrain Settings")]
        public GameObject chunkPrefab;
        public float voxelScale = 0.75f;
        public int seed = 42;
        public int renderDistance = 12;
        public float heightMultiplier = 25f;
        public int chunkSize = 32;
        public int maxChunkHeight = 250;

        [Header("Materials")]
        public Material voxelMaterial;
        public Gradient gradient;
        public Color wall;

        private Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();
        private Queue<GameObject> chunkPool = new Queue<GameObject>();
        private NoiseGenerator noiseGenerator;

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
                    Vector3Int chunkCoord = new Vector3Int(currentChunkCoord.x + xOffset, 0, currentChunkCoord.y + zOffset);
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
            List<Vector3Int> chunksToUnload = new List<Vector3Int>();

            foreach (var chunk in chunks)
            {
                int distanceX = Mathf.Abs(chunk.Key.x - currentChunkCoord.x);
                int distanceZ = Mathf.Abs(chunk.Key.z - currentChunkCoord.y);

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
        IEnumerator LoadChunkAsync(Vector3Int chunkCoord)
        {
            Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkSize * voxelScale, chunkCoord.y * chunkSize * voxelScale, chunkCoord.z * chunkSize * voxelScale);
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
            chunkBehaviour.SetChunkSize(chunkSize, maxChunkHeight);
            chunkBehaviour.SetChunkCoord(new Vector2Int(chunkCoord.x, chunkCoord.z));
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
        void UnloadChunk(Vector3Int chunkCoord)
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