using UnityEngine;
using World.MeshGeneration;

namespace World.Chunks
{
    /// <summary>
    /// Represents a single terrain chunk.
    /// Owns the chunk's <see cref="GameObject"/>, mesh, and visibility state.
    /// </summary>
    public class TerrainChunk
    {
        public readonly Vector2Int chunkCoord;
        public readonly int        chunkSize;
        public readonly float      voxelScale;
        public readonly GameObject chunkObject;
        public readonly Material   voxelMaterial;

        private Renderer[] renderers;
        private Collider[] colliders;
        private bool isVisible = true;

        private const float MaxViewDistance = 500f;

        public TerrainChunk(Vector2Int coord, int size, float scale, Transform parent, Material material)
        {
            chunkCoord    = coord;
            chunkSize     = size;
            voxelScale    = scale;
            voxelMaterial = material;

            chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
            chunkObject.transform.parent   = parent;
            chunkObject.transform.position = new Vector3(coord.x * size, 0, coord.y * size) * scale;
        }

        /// <summary>
        /// Applies mesh data to the chunk's MeshFilter, MeshRenderer, and MeshCollider.
        /// Adds the necessary components if they don't yet exist.
        /// </summary>
        public void UpdateChunkMesh(MeshData meshData)
        {
            MeshFilter   meshFilter   = GetOrAdd<MeshFilter>();
            MeshRenderer meshRenderer = GetOrAdd<MeshRenderer>();
            meshRenderer.material = voxelMaterial;

            Mesh mesh = new Mesh
            {
                name      = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_Mesh",
                vertices  = meshData.vertices.ToArray(),
                triangles = meshData.triangles.ToArray(),
                uv        = meshData.uvs.ToArray(),
                colors    = meshData.colors.ToArray()
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

            GetOrAdd<MeshCollider>().sharedMesh = mesh;

            // Cache for fast visibility toggling
            renderers = chunkObject.GetComponentsInChildren<Renderer>();
            colliders = chunkObject.GetComponentsInChildren<Collider>();
        }

        /// <summary>
        /// Enables or disables renderers and colliders based on player distance and camera frustum.
        /// </summary>
        /// <param name="player">Used for distance culling.</param>
        /// <param name="camera">Used for frustum culling. May be null (skips frustum check).</param>
        public void UpdateVisibility(Transform player, Camera camera)
        {
            // Distance cull
            float distance = Vector3.Distance(player.position, chunkObject.transform.position);
            if (distance > MaxViewDistance)
            {
                SetVisible(false);
                return;
            }

            // Frustum cull
            bool inFrustum = true;
            if (camera != null && renderers != null && renderers.Length > 0)
            {
                Plane[] planes         = GeometryUtility.CalculateFrustumPlanes(camera);
                Bounds  combinedBounds = new Bounds(chunkObject.transform.position, Vector3.zero);
                foreach (var r in renderers)
                    combinedBounds.Encapsulate(r.bounds);
                inFrustum = GeometryUtility.TestPlanesAABB(planes, combinedBounds);
            }

            SetVisible(inFrustum);
        }

        /// <summary>Destroys the chunk's GameObject and releases resources.</summary>
        public void DestroyChunk() => Object.Destroy(chunkObject);

        // ─── Helpers ────────────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (visible == isVisible) return;
            isVisible = visible;

            if (renderers != null)
                foreach (var r in renderers) r.enabled = visible;

            if (colliders != null)
                foreach (var c in colliders) c.enabled = visible;
        }

        private T GetOrAdd<T>() where T : Component
        {
            T component = chunkObject.GetComponent<T>();
            return component != null ? component : chunkObject.AddComponent<T>();
        }
    }
}
