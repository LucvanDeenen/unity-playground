using UnityEngine;
using World.MeshGeneration;

namespace World.Chunks
{
    /// <summary>
    /// Represents a single terrain chunk.
    /// </summary>
    public class TerrainChunk
    {
        public Vector2Int chunkCoord;
        public int chunkSize;
        public float voxelScale;
        public GameObject chunkObject;
        public Material voxelMaterial;

        private Renderer[] renderers;
        private bool isVisible = true;
        private const float maxViewDistance = 500f;

        public TerrainChunk(Vector2Int coord, int size, float scale, Transform parent, Material material)
        {
            chunkCoord = coord;
            chunkSize = size;
            voxelScale = scale;
            voxelMaterial = material;

            chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
            chunkObject.transform.parent = parent;
            chunkObject.transform.position = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize) * voxelScale;

            renderers = chunkObject.GetComponentsInChildren<Renderer>();
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
                vertices = meshData.vertices.ToArray(),
                triangles = meshData.triangles.ToArray(),
                uv = meshData.uvs.ToArray(),
                colors = meshData.colors.ToArray()
            };
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
        /// Updates the visibility of the chunk based on the camera's position and view frustum.
        /// </summary>
        /// <param name="cameraTransform">The transform of the player's camera.</param>
        public void UpdateVisibility(Transform cameraTransform)
        {
            float distance = Vector3.Distance(cameraTransform.position, chunkObject.transform.position);

            if (distance > maxViewDistance)
            {
                if (isVisible)
                {
                    isVisible = false;
                    chunkObject.SetActive(false);
                }
                return;
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            Bounds combinedBounds = new Bounds(chunkObject.transform.position, Vector3.zero);

            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            bool currentlyVisible = GeometryUtility.TestPlanesAABB(planes, combinedBounds);

            if (currentlyVisible != isVisible)
            {
                isVisible = currentlyVisible;
                chunkObject.SetActive(isVisible);
            }
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

