using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates mesh data from a height map.
/// </summary>
public class MeshGenerator
{
    private float voxelScale;
    private Gradient terrainGradient;
    private float gradientMinHeight;
    private float gradientMaxHeight;

    public MeshGenerator(float voxelScale, Gradient terrainGradient, float gradientMinHeight, float gradientMaxHeight)
    {
        this.voxelScale = voxelScale;
        this.terrainGradient = terrainGradient;
        this.gradientMinHeight = gradientMinHeight;
        this.gradientMaxHeight = gradientMaxHeight;
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

                    // Top face (only for the topmost block).
                    if (y == Mathf.FloorToInt(columnHeight))
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.up, blockHeight + voxelScale);
                    }

                    // Bottom face (only for the bottommost block).
                    if (y == startY)
                    {
                        AddVoxelFace(meshData, blockPosition, Vector3.down, blockHeight);
                    }

                    // Side faces.
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

        // Define triangles in clockwise order.
        meshData.triangles.Add(vertexIndex + 0);
        meshData.triangles.Add(vertexIndex + 1);
        meshData.triangles.Add(vertexIndex + 2);

        meshData.triangles.Add(vertexIndex + 2);
        meshData.triangles.Add(vertexIndex + 3);
        meshData.triangles.Add(vertexIndex + 0);

        // Add UVs for texturing.
        meshData.uvs.AddRange(new Vector2[]
        {
            new Vector2(0, 0), // Bottom-left
            new Vector2(0, 1), // Top-left
            new Vector2(1, 1), // Top-right
            new Vector2(1, 0)  // Bottom-right
        });

        // Calculate normalized height for color evaluation using static min and max heights
        float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
        normalizedHeight = Mathf.Clamp01(normalizedHeight);

        // Get the color from the gradient
        Color vertexColor = terrainGradient.Evaluate(normalizedHeight);

        // Add vertex colors
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
            // Top face.
            faceVertices[0] = position + new Vector3(0, s, 0);
            faceVertices[1] = position + new Vector3(0, s, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(s, s, 0);
        }
        else if (direction == Vector3.down)
        {
            // Bottom face.
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(s, 0, 0);
            faceVertices[3] = position + new Vector3(s, 0, s);
        }
        else if (direction == Vector3.left)
        {
            // Left face.
            faceVertices[0] = position + new Vector3(0, 0, 0);
            faceVertices[1] = position + new Vector3(0, 0, s);
            faceVertices[2] = position + new Vector3(0, s, s);
            faceVertices[3] = position + new Vector3(0, s, 0);
        }
        else if (direction == Vector3.right)
        {
            // Right face.
            faceVertices[0] = position + new Vector3(s, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, 0);
            faceVertices[2] = position + new Vector3(s, s, 0);
            faceVertices[3] = position + new Vector3(s, s, s);
        }
        else if (direction == Vector3.forward)
        {
            // Front face.
            faceVertices[0] = position + new Vector3(0, 0, s);
            faceVertices[1] = position + new Vector3(s, 0, s);
            faceVertices[2] = position + new Vector3(s, s, s);
            faceVertices[3] = position + new Vector3(0, s, s);
        }
        else if (direction == Vector3.back)
        {
            // Back face.
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
            // Neighbor is outside the chunk; face is visible.
            return true;
        }

        float neighborHeight = heightMap[x, z];
        int neighborStartY = Mathf.FloorToInt(Mathf.Min(0, neighborHeight));
        int neighborEndY = Mathf.FloorToInt(Mathf.Max(0, neighborHeight));

        // Adjust the logic for visibility when y < 0.
        if (y >= 0)
        {
            return y > neighborEndY || y < neighborStartY;
        }
        else
        {
            return y < neighborStartY || y > neighborEndY;
        }
    }
}
