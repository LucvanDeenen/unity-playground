using UnityEngine;
using System.Collections.Generic;

public class Chunk
{
    public Vector3Int ChunkPosition { get; private set; }
    public Mesh Mesh { get; private set; }

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public Chunk(Vector3Int chunkPosition, MeshFilter filter, MeshRenderer renderer)
    {
        ChunkPosition = chunkPosition;
        meshFilter = filter;
        meshRenderer = renderer;
        Mesh = new Mesh();
    }

    /// <summary>
    /// Generates the mesh for the chunk based on block positions.
    /// </summary>
    public void GenerateMesh(List<Vector3Int> blockPositions, float noiseScale, float threshold)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        foreach (var pos in blockPositions)
        {
            // Determine which faces are visible
            // For simplicity, assume all faces are visible
            // Implement face culling based on neighboring blocks for optimization

            AddCube(new Vector3(pos.x, pos.y, pos.z));
        }

        Mesh.Clear();
        Mesh.vertices = vertices.ToArray();
        Mesh.triangles = triangles.ToArray();
        Mesh.uv = uvs.ToArray();
        Mesh.RecalculateNormals();

        meshFilter.mesh = Mesh;
    }

    /// <summary>
    /// Adds a cube to the mesh at the specified position.
    /// </summary>
    private void AddCube(Vector3 position)
    {
        int vertexIndex = vertices.Count;

        // Define cube vertices
        vertices.Add(position + new Vector3(0, 0, 0));
        vertices.Add(position + new Vector3(1, 0, 0));
        vertices.Add(position + new Vector3(1, 1, 0));
        vertices.Add(position + new Vector3(0, 1, 0));
        vertices.Add(position + new Vector3(0, 0, 1));
        vertices.Add(position + new Vector3(1, 0, 1));
        vertices.Add(position + new Vector3(1, 1, 1));
        vertices.Add(position + new Vector3(0, 1, 1));

        // Define cube triangles
        // Front
        triangles.Add(vertexIndex + 0);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 0);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 2);

        // Back
        triangles.Add(vertexIndex + 5);
        triangles.Add(vertexIndex + 6);
        triangles.Add(vertexIndex + 4);
        triangles.Add(vertexIndex + 6);
        triangles.Add(vertexIndex + 7);
        triangles.Add(vertexIndex + 4);

        // Left
        triangles.Add(vertexIndex + 4);
        triangles.Add(vertexIndex + 7);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 4);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 0);

        // Right
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 6);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 6);
        triangles.Add(vertexIndex + 5);

        // Top
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 7);
        triangles.Add(vertexIndex + 6);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 6);
        triangles.Add(vertexIndex + 2);

        // Bottom
        triangles.Add(vertexIndex + 4);
        triangles.Add(vertexIndex + 0);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 4);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 5);

        // Define UVs (simple placeholder, can be customized)
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));
    }

    /// <summary>
    /// Assigns the generated mesh to the MeshFilter and MeshRenderer.
    /// </summary>
    public void ApplyMesh()
    {
        meshFilter.mesh = Mesh;
        // Assign materials if necessary
    }
}
