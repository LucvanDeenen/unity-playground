using UnityEngine;
using System.Collections.Generic;

public class MeshGenerator
{
    private float gradientMinHeight = 50f;
    private float gradientMaxHeight = 150f;

    private Gradient terrainGradient;
    private float voxelScale;

    private Color wallColor;

    public MeshGenerator(float voxelScale, Gradient terrainGradient, Color wallColor)
    {
        this.voxelScale = voxelScale;
        this.terrainGradient = terrainGradient;
        this.wallColor = wallColor;
    }

    /// <summary>
    /// Generates mesh data based on the height map and cliff areas.
    /// </summary>
    /// <param name="heightMap">Height map data.</param>
    /// <param name="isCliffArea">Boolean array indicating cliff areas.</param>
    /// <returns>Generated mesh data.</returns>
    public MeshData GenerateMeshData(float[,] heightMap, bool[,] isCliffArea)
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

                bool currentIsCliff = isCliffArea[x, z];

                for (int y = startY; y <= endY; y++)
                {
                    Vector3 blockPosition = new Vector3(x, y, z) * voxelScale;
                    float blockHeight = y * voxelScale;

                    // Top face (only for the topmost block).
                    if (y == Mathf.FloorToInt(columnHeight))
                    {
                        // Determine if the top face is part of a cliff
                        bool isTopFaceCliff = currentIsCliff;
                        AddVoxelFace(meshData, blockPosition, Vector3.up, blockHeight + voxelScale, isTopFaceCliff, true);
                    }

                    // Bottom face (only for the bottommost block).
                    if (y == startY)
                    {
                        bool isBottomFaceCliff = currentIsCliff;
                        AddVoxelFace(meshData, blockPosition, Vector3.down, blockHeight, isBottomFaceCliff, false);
                    }

                    // Side faces.
                    if (IsFaceVisible(heightMap, x - 1, z, y))
                    {
                        bool neighborIsCliff = (x - 1 >= 0) ? isCliffArea[x - 1, z] : false;
                        bool isCliffFace = currentIsCliff || neighborIsCliff;
                        AddVoxelFace(meshData, blockPosition, Vector3.left, blockHeight, isCliffFace, false);
                    }

                    if (IsFaceVisible(heightMap, x + 1, z, y))
                    {
                        bool neighborIsCliff = (x + 1 <= chunkSize) ? isCliffArea[x + 1, z] : false;
                        bool isCliffFace = currentIsCliff || neighborIsCliff;
                        AddVoxelFace(meshData, blockPosition, Vector3.right, blockHeight, isCliffFace, false);
                    }

                    if (IsFaceVisible(heightMap, x, z - 1, y))
                    {
                        bool neighborIsCliff = (z - 1 >= 0) ? isCliffArea[x, z - 1] : false;
                        bool isCliffFace = currentIsCliff || neighborIsCliff;
                        AddVoxelFace(meshData, blockPosition, Vector3.back, blockHeight, isCliffFace, false);
                    }

                    if (IsFaceVisible(heightMap, x, z + 1, y))
                    {
                        bool neighborIsCliff = (z + 1 <= chunkSize) ? isCliffArea[x, z + 1] : false;
                        bool isCliffFace = currentIsCliff || neighborIsCliff;
                        AddVoxelFace(meshData, blockPosition, Vector3.forward, blockHeight, isCliffFace, false);
                    }
                }
            }
        }

        return meshData;
    }

    /// <summary>
    /// Adds a voxel face to the mesh data.
    /// </summary>
    /// <param name="meshData">The mesh data to modify.</param>
    /// <param name="position">Position of the voxel.</param>
    /// <param name="direction">Direction of the face.</param>
    /// <param name="height">Height of the face.</param>
    /// <param name="isCliffFace">Indicates if the face is part of a cliff.</param>
    /// <param name="isTopFace">Indicates if the face is a top face.</param>
    private void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction, float height, bool isCliffFace, bool isTopFace)
    {
        Vector3[] faceVertices = GetFaceVertices(position, direction);
        int vertexIndex = meshData.vertices.Count;

        meshData.vertices.AddRange(faceVertices);

        // Define triangles
        meshData.triangles.Add(vertexIndex + 0);
        meshData.triangles.Add(vertexIndex + 1);
        meshData.triangles.Add(vertexIndex + 2);
        meshData.triangles.Add(vertexIndex + 2);
        meshData.triangles.Add(vertexIndex + 3);
        meshData.triangles.Add(vertexIndex + 0);

        // Define UVs
        meshData.uvs.AddRange(new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        });

        Color vertexColor;

        if (isTopFace)
        {
            if (isCliffFace)
            {
                // Top face of a cliff: Use wall color
                vertexColor = wallColor;
            }
            else
            {
                // Top surface: Use terrain gradient
                float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
                normalizedHeight = Mathf.Clamp01(normalizedHeight);
                vertexColor = terrainGradient.Evaluate(normalizedHeight);
            }
        }
        else
        {
            if (isCliffFace)
            {
                // Cliff wall: Use wall color
                vertexColor = wallColor;
            }
            else
            {
                // Side of normal terrain: Use terrain gradient
                float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
                normalizedHeight = Mathf.Clamp01(normalizedHeight);
                vertexColor = terrainGradient.Evaluate(normalizedHeight);
            }
        }

        // Assign the color to all vertices of the face
        for (int i = 0; i < 4; i++)
        {
            meshData.colors.Add(vertexColor);
        }
    }

    /// <summary>
    /// Retrieves the vertices for a given face direction.
    /// </summary>
    /// <param name="position">Position of the voxel.</param>
    /// <param name="direction">Direction of the face.</param>
    /// <returns>Array of four vertices.</returns>
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

    /// <summary>
    /// Determines if a face should be visible based on neighboring blocks.
    /// </summary>
    /// <param name="heightMap">Height map data.</param>
    /// <param name="x">X-coordinate of the neighbor.</param>
    /// <param name="z">Z-coordinate of the neighbor.</param>
    /// <param name="y">Y-coordinate of the current block.</param>
    /// <returns>True if the face is visible; otherwise, false.</returns>
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

/// <summary>
/// Represents mesh data for a terrain chunk.
/// </summary>
public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();
    public List<Color> colors = new List<Color>();
}
