using UnityEngine;
using UnityEngine.Rendering;
using World.MeshGeneration;

namespace World.Chunks
{
    /// <summary>
    /// Represents a single terrain chunk. Culling is left to Unity's built-in
    /// per-renderer frustum culling.
    /// </summary>
    public class TerrainChunk
    {
        public Vector2Int chunkCoord;
        public int chunkSize;
        public float voxelScale;
        public GameObject chunkObject;
        public Material voxelMaterial;

        public TerrainChunk(Vector2Int coord, int size, float scale, Transform parent, Material material)
        {
            chunkCoord = coord;
            chunkSize = size;
            voxelScale = scale;
            voxelMaterial = material;

            chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            // The player's ground check only detects the "Ground" layer.
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                chunkObject.layer = groundLayer;
            }
            chunkObject.transform.parent = parent;
            chunkObject.transform.position = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize) * voxelScale;
        }

        /// <summary>
        /// Updates the mesh of the terrain chunk.
        /// </summary>
        /// <param name="meshData">The mesh data to apply.</param>
        public void UpdateChunkMesh(MeshData meshData)
        {
            MeshFilter meshFilter = chunkObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = chunkObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = chunkObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                meshRenderer.material = voxelMaterial;
            }

            Mesh mesh = new Mesh
            {
                // Steep chunks (mountains) can exceed the default 16-bit vertex limit.
                indexFormat = IndexFormat.UInt32
            };
            mesh.SetVertices(meshData.vertices);
            mesh.SetUVs(0, meshData.uvs);
            mesh.SetColors(meshData.colors);
            mesh.SetTriangles(meshData.triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            MeshCollider meshCollider = chunkObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = chunkObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = mesh;
        }

        /// <summary>
        /// Destroys the terrain chunk.
        /// </summary>
        public void DestroyChunk()
        {
            GameObject.Destroy(chunkObject);
        }
    }
}
