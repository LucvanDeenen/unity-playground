using UnityEngine;
using System.Linq;

namespace World.MeshGeneration
{
    /// <summary>
    /// Generates mesh data from a height map.
    /// </summary>
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
        /// Generates mesh data based on the height map.
        /// </summary>
        /// <param name="heightMap">Height map data.</param>
        /// <returns>Generated mesh data.</returns>
        public MeshData GenerateMeshData(float[,] heightMap)
        {
            int chunkSize = heightMap.GetLength(0) - 1;
            int lowestY = Mathf.RoundToInt(heightMap.OfType<float>().Min());

            MeshData meshData = new MeshData();
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float columnHeight = heightMap[x, z];

                    int startY = Mathf.FloorToInt(lowestY);
                    int endY = Mathf.FloorToInt(columnHeight);
                    for (int y = startY; y <= endY; y++)
                    {
                        Vector3 blockPosition = new Vector3(x, y, z) * voxelScale;
                        float blockHeight = y * voxelScale;

                        if (y == startY)
                        {
                            AddVoxelFace(meshData, blockPosition, Vector3.down, blockHeight + voxelScale, true);
                        }

                        if (y == endY)
                        {
                            AddVoxelFace(meshData, blockPosition, Vector3.up, blockHeight, false);
                        }

                        // Side faces.
                        if (IsFaceVisible(heightMap, x - 1, z, y))
                        {
                            AddVoxelFace(meshData, blockPosition, Vector3.left, blockHeight, false);
                        }

                        if (IsFaceVisible(heightMap, x + 1, z, y))
                        {
                            AddVoxelFace(meshData, blockPosition, Vector3.right, blockHeight, false);
                        }

                        if (IsFaceVisible(heightMap, x, z - 1, y))
                        {
                            AddVoxelFace(meshData, blockPosition, Vector3.back, blockHeight, false);
                        }

                        if (IsFaceVisible(heightMap, x, z + 1, y))
                        {
                            AddVoxelFace(meshData, blockPosition, Vector3.forward, blockHeight, false);
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
        /// <param name="isTopFace">Indicates if the face is a top face.</param>
        private void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction, float height, bool isTopFace)
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
                float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
                normalizedHeight = Mathf.Clamp01(normalizedHeight);
                vertexColor = terrainGradient.Evaluate(normalizedHeight);
            }
            else
            {
                float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
                normalizedHeight = Mathf.Clamp01(normalizedHeight);
                vertexColor = terrainGradient.Evaluate(normalizedHeight);
            }

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
                // Top face.
                faceVertices[0] = position + new Vector3(0, s, 0);
                faceVertices[1] = position + new Vector3(s, s, 0);
                faceVertices[2] = position + new Vector3(s, s, s);
                faceVertices[3] = position + new Vector3(0, s, s);
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
                // Neighbor is outside the chunk; face is visible.
                return true;
            }

            float neighborHeight = heightMap[x, z];
            int neighborStartY = Mathf.FloorToInt(Mathf.Min(0, neighborHeight));
            int neighborEndY = Mathf.FloorToInt(Mathf.Max(0, neighborHeight));

            // Face is visible if there is no neighbor block at the same 'y' level
            return y < neighborStartY || y > neighborEndY;
        }
    }
}
