using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlane : MonoBehaviour
{
    // Mesh components
    private Mesh mesh;

    // Vertices, uvs and triangles
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        vertices = new Vector3[]
        {
            // Front face
            new Vector3(0, 0, 0), // v0
            new Vector3(1, 0, 0), // v1
            new Vector3(1, 1, 0), // v2
            new Vector3(0, 1, 0), // v3

            // Back face
            new Vector3(1, 0, 1), // v4
            new Vector3(0, 0, 1), // v5
            new Vector3(0, 1, 1), // v6
            new Vector3(1, 1, 1), // v7

            // Left face
            new Vector3(0, 0, 1), // v8
            new Vector3(0, 0, 0), // v9
            new Vector3(0, 1, 0), // v10
            new Vector3(0, 1, 1), // v11

            // Right face
            new Vector3(1, 0, 0), // v12
            new Vector3(1, 0, 1), // v13
            new Vector3(1, 1, 1), // v14
            new Vector3(1, 1, 0), // v15

            // Top face
            new Vector3(0, 1, 0), // v16
            new Vector3(1, 1, 0), // v17
            new Vector3(1, 1, 1), // v18
            new Vector3(0, 1, 1), // v19

            // Bottom face
            new Vector3(1, 0, 0), // v20
            new Vector3(0, 0, 0), // v21
            new Vector3(0, 0, 1), // v22
            new Vector3(1, 0, 1)  // v23
        };

        triangles = new int[]
        {
            // Front face
            0, 2, 1,
            0, 3, 2,

            // Back face
            4, 6, 5,
            4, 7, 6,

            // Left face
            8, 10, 9,
            8, 11, 10,

            // Right face
            12, 14, 13,
            12, 15, 14,

            // Top face
            16, 18, 17,
            16, 19, 18,

            // Bottom face
            20, 22, 21,
            20, 23, 22
        };

        uvs = new Vector2[]
        {
             // Front face
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),

            // Back face
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),

            // Left face
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),

            // Right face
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),

            // Top face
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),

            // Bottom face
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
    }

    void UpdateMesh()
    {
        mesh.Clear();

        // Assign the vertices and triangles to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        // Recalculate normals for lighting
        mesh.RecalculateNormals();
    }

    // Optional: Visualize the mesh in the editor
    void OnDrawGizmos()
    {
        if (vertices == null)
            return;

        Gizmos.color = Color.green;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
    }
}
