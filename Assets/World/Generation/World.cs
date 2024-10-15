using UnityEngine;

namespace World.Generation
{
    public class World : MonoBehaviour
    {
        public GameObject chunkPrefab;
        public int worldSizeInChunks = 2;

        void Start()
        {
            for (int x = 0; x < worldSizeInChunks; x++)
            {
                for (int z = 0; z < worldSizeInChunks; z++)
                {
                    Vector3 chunkPosition = new Vector3(x * 32, 0, z * 32);
                    GameObject chunk = Instantiate(chunkPrefab, chunkPosition, Quaternion.identity, transform);
                    chunk.name = $"Chunk_{x}_{z}";
                }
            }
        }
    }
}