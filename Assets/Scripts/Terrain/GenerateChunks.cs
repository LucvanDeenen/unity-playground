using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GenerateChunks : MonoBehaviour
{
    // Chunk parameters
    public int chunkSize = 32; // Size of each chunk (32x32 units)
    public int chunksPerAxis = 4; // Number of chunks per axis (e.g., 4x4 grid)
    public float spacing = 0f; // Spacing between chunks

    // Material for the chunks
    public Material chunkMaterial;

    void Start()
    {
        GenerateAllChunks();
    }

    void GenerateAllChunks()
    {

        for (int x = 0; x < chunksPerAxis; x++)
        {
            for (int z = 0; z < chunksPerAxis; z++)
            {
                // Calculate chunk position
                Vector3 chunkPosition = new Vector3(
                    x * (chunkSize + spacing),
                    0,
                    z * (chunkSize + spacing)
                );

                // Create a new GameObject for the chunk
                GameObject chunk = new GameObject($"Chunk_{x}_{z}");
                chunk.transform.parent = this.transform;
                chunk.transform.position = chunkPosition;

                // Add necessary components
                MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();

                // Assign material
                if (chunkMaterial != null)
                {
                    meshRenderer.material = chunkMaterial;
                }
                else
                {
                    // Create a default material if none is assigned
                    meshRenderer.material = new Material(Shader.Find("Standard"));
                    meshRenderer.material.color = Color.green;
                }

                // Generate mesh
                Mesh mesh = GenerateFlatChunkMesh();
                meshFilter.mesh = mesh;
            }
        }
    }

    Mesh GenerateFlatChunkMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FlatChunk";

        // Define vertices
        Vector3[] vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];
        for (int i = 0, z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++, i++)
            {
                // Inside GenerateFlatChunkMesh()
                float height = Mathf.PerlinNoise((float)x / chunkSize, (float)z / chunkSize) * 35f;
                vertices[i] = new Vector3(x, height, z);
            }
        }

        // Define triangles
        int[] triangles = new int[chunkSize * chunkSize * 6];
        int tri = 0;
        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int current = z * (chunkSize + 1) + x;
                int next = current + chunkSize + 1;

                // First triangle (current, next, current + 1)
                triangles[tri++] = current;
                triangles[tri++] = next;
                triangles[tri++] = current + 1;

                // Second triangle (current + 1, next, next + 1)
                triangles[tri++] = current + 1;
                triangles[tri++] = next;
                triangles[tri++] = next + 1;
            }
        }

        // Define UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / chunkSize, vertices[i].z / chunkSize);
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        // Recalculate normals for lighting
        mesh.RecalculateNormals();

        return mesh;
    }
}
