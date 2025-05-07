using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class VoxelChunk : MonoBehaviour
{
    [HideInInspector] public int sizeXZ;
    [HideInInspector] public int sizeY;
    [HideInInspector] public float noiseScale;
    [HideInInspector] public float heightMult;

    private int[,,] voxels;  // [x, y, z]
    private MeshFilter mf;

    /// <summary>
    /// Call once after setting sizeXZ and sizeY
    /// </summary>
    public void Initialize()
    {
        mf = GetComponent<MeshFilter>();
        voxels = new int[sizeXZ, sizeY, sizeXZ];
    }

    public void GenerateMesh()
    {
        PopulateVoxels();
        BuildMesh();
    }

    void PopulateVoxels()
    {
        for (int x = 0; x < sizeXZ; x++)
            for (int z = 0; z < sizeXZ; z++)
            {
                float worldX = transform.position.x + x;
                float worldZ = transform.position.z + z;
                float h = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale)
                          * heightMult;
                int hInt = Mathf.Clamp(Mathf.FloorToInt(h), 0, sizeY - 1);

                for (int y = 0; y <= hInt; y++)
                    voxels[x, y, z] = 1;
            }
    }

    void BuildMesh()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        int vCount = 0;

        Vector3[] faceDirs = {
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
            Vector3.right, Vector3.left
        };

        Vector3[][] faceVerts = new Vector3[][] {
            // Up
            new[]{ new Vector3(0,1,0), new Vector3(1,1,0),
                   new Vector3(1,1,1), new Vector3(0,1,1) },
            // Down
            new[]{ new Vector3(0,0,1), new Vector3(1,0,1),
                   new Vector3(1,0,0), new Vector3(0,0,0) },
            // Front (+Z)
            new[]{ new Vector3(1,0,1), new Vector3(0,0,1),
                   new Vector3(0,1,1), new Vector3(1,1,1) },
            // Back (-Z)
            new[]{ new Vector3(0,0,0), new Vector3(1,0,0),
                   new Vector3(1,1,0), new Vector3(0,1,0) },
            // Right (+X)
            new[]{ new Vector3(1,0,0), new Vector3(1,0,1),
                   new Vector3(1,1,1), new Vector3(1,1,0) },
            // Left (-X)
            new[]{ new Vector3(0,0,1), new Vector3(0,0,0),
                   new Vector3(0,1,0), new Vector3(0,1,1) }
        };

        Vector2[] faceUVs = {
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(1,1), new Vector2(0,1)
        };

        for (int x = 0; x < sizeXZ; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeXZ; z++)
                {
                    if (voxels[x, y, z] == 0) continue;

                    for (int f = 0; f < 6; f++)
                    {
                        var dir = faceDirs[f];
                        int nx = x + (int)dir.x,
                            ny = y + (int)dir.y,
                            nz = z + (int)dir.z;

                        bool draw =
                            nx < 0 || nx >= sizeXZ ||
                            ny < 0 || ny >= sizeY ||
                            nz < 0 || nz >= sizeXZ ||
                            voxels[nx, ny, nz] == 0;
                        if (!draw) continue;

                        // add verts & UVs
                        for (int i = 0; i < 4; i++)
                            verts.Add(new Vector3(x, y, z) + faceVerts[f][i]);
                        uvs.AddRange(faceUVs);

                        // inverted winding for all faces (CCW from outside)
                        tris.Add(vCount + 0);
                        tris.Add(vCount + 2);
                        tris.Add(vCount + 1);

                        tris.Add(vCount + 0);
                        tris.Add(vCount + 3);
                        tris.Add(vCount + 2);

                        vCount += 4;
                    }
                }

        var mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = verts.ToArray(),
            triangles = tris.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }
}
