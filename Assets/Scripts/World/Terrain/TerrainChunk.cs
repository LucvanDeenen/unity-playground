using UnityEngine;

/// <summary>
/// Represents a single terrain chunk.
/// </summary>
public class TerrainChunk
{
    public GameObject chunkObject;
    public Vector2Int chunkCoord;
    public int chunkSize;
    public float voxelScale;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    public TerrainChunk(Vector2Int coord, int size, float scale, Transform parent, Material material)
    {
        chunkCoord = coord;
        chunkSize = size;
        voxelScale = scale;

        CreateChunkObject(parent, material);
    }

    private void CreateChunkObject(Transform parent, Material material)
    {
        chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObject.transform.parent = parent;
        Vector3 chunkPosition = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize) * voxelScale;
        chunkObject.transform.position = chunkPosition;

        // Set the ground tag and layer (ensure "Ground" layer exists in your project).
        chunkObject.tag = "Ground";
        chunkObject.layer = LayerMask.NameToLayer("Ground");

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        meshCollider = chunkObject.AddComponent<MeshCollider>();
    }

    public void UpdateChunkMesh(MeshData meshData)
    {
        Mesh mesh = new Mesh
        {
            vertices = meshData.vertices.ToArray(),
            triangles = meshData.triangles.ToArray(),
            uv = meshData.uvs.ToArray(),
            colors = meshData.colors.ToArray()
        };
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void DestroyChunk()
    {
        Object.Destroy(chunkObject);
    }
}
