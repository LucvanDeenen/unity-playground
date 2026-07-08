using UnityEngine;
using World.Biomes;

namespace World.MeshGeneration
{
    /// <summary>
    /// Generates voxel mesh data from per-column solidity masks. Everything
    /// renders as unit blocks — flat tops and vertical faces — so hillsides read
    /// as staircases of visible cubes; the gradual Cube World feel comes from
    /// broad, low-slope landforms, not from smoothing the mesh.
    /// </summary>
    public class MeshGenerator
    {
        // Walls up to this many blocks tall stay grass-colored (the surface
        // wraps over shallow steps); taller cliff faces show the mud wall color.
        private const int GrassWrapMaxSpan = 2;

        private readonly float voxelScale;
        private readonly Color wallColor;
        private readonly Color ceilingColor;

        public MeshGenerator(float voxelScale, Color wallColor)
        {
            this.voxelScale = voxelScale;
            this.wallColor = wallColor;

            ceilingColor = wallColor * 0.7f;
            ceilingColor.a = 1f;
        }

        /// <summary>
        /// Generates mesh data for a chunk, using the chunk data's one-column
        /// border to resolve which side faces are exposed, so adjacent chunks
        /// meet without gaps or duplicated walls.
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
                    if (column.solidMask.IsEmpty)
                    {
                        continue;
                    }

                    AddColumn(meshData, chunkData, x, z, Mathf.FloorToInt(column.height), column);
                }
            }

            return meshData;
        }

        private void AddColumn(MeshData meshData, ChunkData chunkData, int x, int z, int topY, in BiomeColumn column)
        {
            SolidMask mask = column.solidMask;

            for (int y = 0; y <= topY; y++)
            {
                if (!mask.IsSolid(y))
                {
                    continue;
                }

                if (!mask.IsSolid(y + 1))
                {
                    AddVoxelFace(meshData, new Vector3(x, y, z) * voxelScale, Vector3.up, column.surfaceColor);
                }

                // Skip the world floor at y = 0.
                if (y > 0 && !mask.IsSolid(y - 1))
                {
                    AddVoxelFace(meshData, new Vector3(x, y, z) * voxelScale, Vector3.down, ceilingColor);
                }
            }

            AddExposedSideFaces(meshData, x, z, mask, chunkData.GetColumn(x - 1, z), topY, Vector3.left, column);
            AddExposedSideFaces(meshData, x, z, mask, chunkData.GetColumn(x + 1, z), topY, Vector3.right, column);
            AddExposedSideFaces(meshData, x, z, mask, chunkData.GetColumn(x, z - 1), topY, Vector3.back, column);
            AddExposedSideFaces(meshData, x, z, mask, chunkData.GetColumn(x, z + 1), topY, Vector3.forward, column);
        }

        /// <summary>
        /// Adds side faces where this column is solid and the neighbor is air.
        /// Short spans and every span's lip block wrap in surface color; the
        /// rest of a tall face shows the wall color.
        /// </summary>
        private void AddExposedSideFaces(MeshData meshData, int x, int z, in SolidMask mask, in BiomeColumn neighbor, int topY, Vector3 direction, in BiomeColumn column)
        {
            Color grassWrap = column.surfaceColor * 0.8f;
            grassWrap.a = 1f;

            int y = 0;
            while (y <= topY)
            {
                if (!mask.IsSolid(y) || neighbor.solidMask.IsSolid(y))
                {
                    y++;
                    continue;
                }

                int spanStart = y;
                while (y <= topY && mask.IsSolid(y) && !neighbor.solidMask.IsSolid(y))
                {
                    y++;
                }

                int spanTop = y - 1;
                int span = spanTop - spanStart + 1;

                for (int faceY = spanStart; faceY <= spanTop; faceY++)
                {
                    Color color = span <= GrassWrapMaxSpan || faceY == spanTop ? grassWrap : wallColor;
                    AddVoxelFace(meshData, new Vector3(x, faceY, z) * voxelScale, direction, color);
                }
            }
        }

        /// <summary>
        /// Adds a quad from four explicit vertices.
        /// </summary>
        private void AddQuad(MeshData meshData, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            int vertexIndex = meshData.vertices.Count;

            meshData.vertices.Add(v0);
            meshData.vertices.Add(v1);
            meshData.vertices.Add(v2);
            meshData.vertices.Add(v3);

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
        /// Adds a single axis-aligned voxel face.
        /// </summary>
        private void AddVoxelFace(MeshData meshData, Vector3 position, Vector3 direction, Color color)
        {
            Vector3[] faceVertices = GetFaceVertices(position, direction);
            AddQuad(meshData, faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3], color);
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
    }
}
