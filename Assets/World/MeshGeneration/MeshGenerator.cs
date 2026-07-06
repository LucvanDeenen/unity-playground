using UnityEngine;
using World.Biomes;

namespace World.MeshGeneration
{
    /// <summary>
    /// Generates voxel column mesh data from generated chunk data.
    /// </summary>
    public class MeshGenerator
    {
        private readonly float voxelScale;

        public MeshGenerator(float voxelScale)
        {
            this.voxelScale = voxelScale;
        }

        /// <summary>
        /// Generates mesh data for a chunk. Top faces use the column's blended biome
        /// surface color; exposed side faces use the blended cliff color. The chunk
        /// data's one-column border resolves face visibility across chunk seams, so
        /// only faces that can actually be seen are emitted.
        /// </summary>
        public MeshData GenerateMeshData(ChunkData chunkData)
        {
            int chunkSize = chunkData.chunkSize;
            MeshData meshData = new MeshData();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    BiomeColumn column = chunkData.GetColumn(x, z);
                    int topY = Mathf.Max(0, Mathf.FloorToInt(column.height));

                    AddVoxelFace(meshData, new Vector3(x, topY, z) * voxelScale, Vector3.up, column.surfaceColor);

                    AddExposedSideFaces(meshData, chunkData, x, z, topY, Vector3.left, x - 1, z, column.cliffColor);
                    AddExposedSideFaces(meshData, chunkData, x, z, topY, Vector3.right, x + 1, z, column.cliffColor);
                    AddExposedSideFaces(meshData, chunkData, x, z, topY, Vector3.back, x, z - 1, column.cliffColor);
                    AddExposedSideFaces(meshData, chunkData, x, z, topY, Vector3.forward, x, z + 1, column.cliffColor);
                }
            }

            return meshData;
        }

        /// <summary>
        /// Adds side faces for the span where this column rises above its neighbor.
        /// Columns whose neighbor is equal or higher emit nothing; the taller
        /// neighbor draws the shared wall from its own side.
        /// </summary>
        private void AddExposedSideFaces(MeshData meshData, ChunkData chunkData, int x, int z, int topY, Vector3 direction, int neighborX, int neighborZ, Color color)
        {
            int neighborTopY = Mathf.FloorToInt(chunkData.GetColumn(neighborX, neighborZ).height);

            for (int y = Mathf.Max(0, neighborTopY + 1); y <= topY; y++)
            {
                AddVoxelFace(meshData, new Vector3(x, y, z) * voxelScale, direction, color);
            }
        }

        /// <summary>
        /// Adds a single voxel face to the mesh data.
        /// </summary>
        private void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction, Color color)
        {
            Vector3[] faceVertices = GetFaceVertices(position, direction);
            int vertexIndex = meshData.vertices.Count;

            meshData.vertices.AddRange(faceVertices);
            meshData.triangles.Add(vertexIndex + 0);
            meshData.triangles.Add(vertexIndex + 1);
            meshData.triangles.Add(vertexIndex + 2);
            meshData.triangles.Add(vertexIndex + 2);
            meshData.triangles.Add(vertexIndex + 3);
            meshData.triangles.Add(vertexIndex + 0);

            meshData.uvs.AddRange(new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0)
            });

            for (int i = 0; i < 4; i++)
            {
                meshData.colors.Add(color);
            }
        }

        /// <summary>
        /// Retrieves the vertices for a given face direction.
        /// </summary>
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
    }
}
