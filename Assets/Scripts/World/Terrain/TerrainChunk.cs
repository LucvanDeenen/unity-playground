using UnityEngine;

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

    public TerrainChunk(Vector2Int coord, int size, float scale, Transform parent, Material material)
    {
        chunkCoord = coord;
        chunkSize = size;
        voxelScale = scale;
        voxelMaterial = material;

        chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObject.transform.parent = parent;
        chunkObject.transform.position = new Vector3(chunkCoord.x * chunkSize, 0, chunkCoord.y * chunkSize) * voxelScale;
    }

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

    public void DestroyChunk()
    {
        GameObject.Destroy(chunkObject);
    }
}
