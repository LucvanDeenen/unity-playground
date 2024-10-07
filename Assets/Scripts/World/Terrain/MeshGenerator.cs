using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator
{
    private float gradientMinHeight = 0f;
    private float gradientMaxHeight = 100f;

    private Gradient terrainGradient;
    private float voxelScale;

    public MeshGenerator(float voxelScale, Gradient terrainGradient)
    {
        this.voxelScale = voxelScale;
        this.terrainGradient = terrainGradient;
    }

    public MeshData GenerateMeshData(float[,] heightMap)
    {
        int chunkSize = heightMap.GetLength(0) - 1;
        MeshData meshData = new MeshData();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                float columnHeight = heightMap[x, z];

                int startY = Mathf.FloorToInt(Mathf.Min(0, columnHeight));
                int endY = Mathf.FloorToInt(Mathf.Max(0, columnHeight));

                for (int y = startY; y <= endY; y++)
                {
                    Vector3 blockPosition = new Vector3(x, y, z) * voxelScale;
                    float blockHeight = y * voxelScale;

                    if (y == Mathf.FloorToInt(columnHeight))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.up, blockHeight + voxelScale);
                    }

                    if (y == startY)
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.down, blockHeight);
                    }

                    if (IsFaceVisible(heightMap, x - 1, z, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.left, blockHeight);
                    }

                    if (IsFaceVisible(heightMap, x + 1, z, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.right, blockHeight);
                    }

                    if (IsFaceVisible(heightMap, x, z - 1, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.back, blockHeight);
                    }

                    if (IsFaceVisible(heightMap, x, z + 1, y))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.forward, blockHeight);
                    }
                }
            }
        }

        return meshData;
    }

    private void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction, float height)
    {
        Vector3[] faceVertices = GetFaceVertices(position, direction);
        int vertexIndex = meshData.vertices.Count;

        meshData.vertices.AddRange(faceVertices);
        bool invertTriangles = direction == Vector3.down || position.y < 0f;

        if (invertTriangles)
        {
            meshData.triangles.Add(vertexIndex + 2);
            meshData.triangles.Add(vertexIndex + 1);
            meshData.triangles.Add(vertexIndex + 0);

            meshData.triangles.Add(vertexIndex + 0);
            meshData.triangles.Add(vertexIndex + 3);
            meshData.triangles.Add(vertexIndex + 2);
        }
        else
        {
            meshData.triangles.Add(vertexIndex + 0);
            meshData.triangles.Add(vertexIndex + 1);
            meshData.triangles.Add(vertexIndex + 2);

            meshData.triangles.Add(vertexIndex + 2);
            meshData.triangles.Add(vertexIndex + 3);
            meshData.triangles.Add(vertexIndex + 0);
        }

        meshData.uvs.AddRange(new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        });

        float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
        normalizedHeight = Mathf.Clamp01(normalizedHeight);

        Color vertexColor = terrainGradient.Evaluate(normalizedHeight);

        meshData.colors.Add(vertexColor);
        meshData.colors.Add(vertexColor);
        meshData.colors.Add(vertexColor);
        meshData.colors.Add(vertexColor);
    }

    private Vector3[] GetFaceVertices(Vector3 position, Vector3 direction)
    {
        Vector3[] faceVertices = new Vector3[4];
        float s = voxelScale;

        if (direction == Vector3.up)
        {
            faceVertices[0] = position + new Vector3(0, s, 0);
            faceVertices[1] = position + new Vector3(0, s, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }
        else if (direction == Vector3.down)
        {
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(s, 0, 0);
            faceVertices[3] = position + new Vector3(s, 0, s);
        }
        else if (direction == Vector3.left)
        {
            faceVertices[0] = position + new Vector3(0, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, s);
            faceVertices[2] = position + new Vector3(0, s, s);
            faceVertices[3] = position + new Vector3(0, s, 0);
        }
        else if (direction == Vector3.right)
        {
            faceVertices[0] = position + new Vector3(s, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, 0);
            faceVertices[2] = position + new Vector3(s, s, 0);
            faceVertices[3] = position + new Vector3(s, s, s);
        }
        else if (direction == Vector3.forward)
        {
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(0, s, s);
        }
        else if (direction == Vector3.back)
        {
            faceVertices[0] = position + new Vector3(s, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(0, s, 0);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }

        return faceVertices;
    }

    private bool IsFaceVisible(float[,] heightMap, int x, int z, int y)
    {
        int chunkSize = heightMap.GetLength(0) - 1;

        if (x < 0 || x >= chunkSize || z < 0 || z >= chunkSize)
        {
            return true;
        }

        float neighborHeight = heightMap[x, z];
        int neighborStartY = Mathf.FloorToInt(Mathf.Min(0, neighborHeight));
        int neighborEndY = Mathf.FloorToInt(Mathf.Max(0, neighborHeight));

        return y < neighborStartY || y > neighborEndY;
    }
}

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();
    public List<Color> colors = new List<Color>();
}