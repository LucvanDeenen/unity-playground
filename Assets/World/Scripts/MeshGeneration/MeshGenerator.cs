using UnityEngine;

namespace World.MeshGeneration
{
    /// <summary>
    /// Generates voxel-column mesh data from a height map.
    /// <para>
    /// <b>Top faces</b> are colored by the terrain gradient (sampled by world height).
    /// <b>Side and bottom faces</b> use the flat <c>wallColor</c>, giving cliffs a
    /// distinct, dirt/rock appearance and keeping the vertex-color material simple.
    /// </para>
    /// </summary>
    public class MeshGenerator
    {
        private readonly float     voxelScale;
        private readonly Gradient  terrainGradient;
        private readonly Color     wallColor;
        private readonly float     gradientMinHeight;
        private readonly float     gradientMaxHeight;

        // Enum avoids floating-point Vector3 comparisons in the original code
        private enum FaceDir { Up, Down, Left, Right, Forward, Back }

        /// <summary>
        /// Creates a MeshGenerator.
        /// </summary>
        /// <param name="voxelScale">World-space size of one voxel side.</param>
        /// <param name="terrainGradient">Gradient applied to top-face colors by height.</param>
        /// <param name="wallColor">Flat color applied to all side and bottom faces.</param>
        /// <param name="gradientMinHeight">Height that maps to gradient t=0 (default 0).</param>
        /// <param name="gradientMaxHeight">Height that maps to gradient t=1 (default 100).</param>
        public MeshGenerator(
            float    voxelScale,
            Gradient terrainGradient,
            Color    wallColor,
            float    gradientMinHeight = 0f,
            float    gradientMaxHeight = 100f)
        {
            this.voxelScale       = voxelScale;
            this.terrainGradient  = terrainGradient;
            this.wallColor        = wallColor;
            this.gradientMinHeight = gradientMinHeight;
            this.gradientMaxHeight = gradientMaxHeight;
        }

        /// <summary>
        /// Generates mesh data from a height map using a voxel-column approach.
        /// Each column is filled from y=0 up to <c>floor(heightMap[x,z])</c>.
        /// Only exposed faces are added, so interior faces are skipped.
        /// </summary>
        public MeshData GenerateMeshData(float[,] heightMap)
        {
            int chunkSize = heightMap.GetLength(0) - 1;
            MeshData meshData = new MeshData();

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int endY = Mathf.FloorToInt(heightMap[x, z]);

                    for (int y = 0; y <= endY; y++)
                    {
                        Vector3 blockPos = new Vector3(x, y, z) * voxelScale;

                        // Top face — only on the topmost block of the column
                        if (y == endY)
                            AddFace(meshData, blockPos, FaceDir.Up, isTopFace: true);

                        // Bottom face — only at ground level (y = 0)
                        if (y == 0)
                            AddFace(meshData, blockPos, FaceDir.Down, isTopFace: false);

                        // Side faces — only where the neighboring column is shorter
                        if (IsFaceExposed(heightMap, x - 1, z, y))
                            AddFace(meshData, blockPos, FaceDir.Left, isTopFace: false);
                        if (IsFaceExposed(heightMap, x + 1, z, y))
                            AddFace(meshData, blockPos, FaceDir.Right, isTopFace: false);
                        if (IsFaceExposed(heightMap, x, z - 1, y))
                            AddFace(meshData, blockPos, FaceDir.Back, isTopFace: false);
                        if (IsFaceExposed(heightMap, x, z + 1, y))
                            AddFace(meshData, blockPos, FaceDir.Forward, isTopFace: false);
                    }
                }
            }

            return meshData;
        }

        // ─── Private helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Adds a quad (two triangles) for one voxel face to the mesh data.
        /// Top faces get a gradient color; all other faces get the flat wall color.
        /// </summary>
        private void AddFace(MeshData meshData, Vector3 position, FaceDir dir, bool isTopFace)
        {
            Vector3[] verts = GetFaceVertices(position, dir);
            int startIdx = meshData.vertices.Count;

            meshData.vertices.AddRange(verts);

            // Two CCW triangles making one quad
            meshData.triangles.Add(startIdx);
            meshData.triangles.Add(startIdx + 1);
            meshData.triangles.Add(startIdx + 2);
            meshData.triangles.Add(startIdx + 2);
            meshData.triangles.Add(startIdx + 3);
            meshData.triangles.Add(startIdx);

            meshData.uvs.Add(new Vector2(0, 0));
            meshData.uvs.Add(new Vector2(0, 1));
            meshData.uvs.Add(new Vector2(1, 1));
            meshData.uvs.Add(new Vector2(1, 0));

            // Top faces sample the terrain gradient; walls/floors use a flat color
            Color color = isTopFace
                ? GetTopColor(position.y + voxelScale)   // use the top-of-block height
                : wallColor;

            for (int i = 0; i < 4; i++)
                meshData.colors.Add(color);
        }

        private Color GetTopColor(float worldHeight)
        {
            float t = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, worldHeight);
            return terrainGradient.Evaluate(Mathf.Clamp01(t));
        }

        /// <summary>
        /// Returns the four vertices of a voxel face, ordered counter-clockwise
        /// when viewed from outside the block.
        /// </summary>
        private Vector3[] GetFaceVertices(Vector3 pos, FaceDir dir)
        {
            float s = voxelScale;
            switch (dir)
            {
                case FaceDir.Up:
                    return new[]
                    {
                        pos + new Vector3(0, s, 0),
                        pos + new Vector3(0, s, s),
                        pos + new Vector3(s, s, s),
                        pos + new Vector3(s, s, 0)
                    };
                case FaceDir.Down:
                    return new[]
                    {
                        pos + new Vector3(0, 0, s),
                        pos + new Vector3(0, 0, 0),
                        pos + new Vector3(s, 0, 0),
                        pos + new Vector3(s, 0, s)
                    };
                case FaceDir.Left:
                    return new[]
                    {
                        pos + new Vector3(0, 0, 0),
                        pos + new Vector3(0, 0, s),
                        pos + new Vector3(0, s, s),
                        pos + new Vector3(0, s, 0)
                    };
                case FaceDir.Right:
                    return new[]
                    {
                        pos + new Vector3(s, 0, s),
                        pos + new Vector3(s, 0, 0),
                        pos + new Vector3(s, s, 0),
                        pos + new Vector3(s, s, s)
                    };
                case FaceDir.Forward:
                    return new[]
                    {
                        pos + new Vector3(0, 0, s),
                        pos + new Vector3(s, 0, s),
                        pos + new Vector3(s, s, s),
                        pos + new Vector3(0, s, s)
                    };
                case FaceDir.Back:
                    return new[]
                    {
                        pos + new Vector3(s, 0, 0),
                        pos + new Vector3(0, 0, 0),
                        pos + new Vector3(0, s, 0),
                        pos + new Vector3(s, s, 0)
                    };
                default:
                    return new Vector3[4];
            }
        }

        /// <summary>
        /// Returns true if the face between (x, z) and its neighbor should be rendered.
        /// A face is exposed when the neighbor is outside the chunk (border) or its
        /// column does not cover the current y-level.
        /// </summary>
        private static bool IsFaceExposed(float[,] heightMap, int x, int z, int y)
        {
            int chunkSize = heightMap.GetLength(0) - 1;

            // Always show faces at chunk borders (neighboring chunk may differ in height)
            if (x < 0 || x >= chunkSize || z < 0 || z >= chunkSize)
                return true;

            int neighborTop = Mathf.FloorToInt(heightMap[x, z]);
            return y > neighborTop;
        }
    }
}
