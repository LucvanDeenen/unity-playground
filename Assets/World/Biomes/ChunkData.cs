using UnityEngine;

namespace World.Biomes
{
    /// <summary>
    /// Per-column result of biome generation: blended height, colors, and vegetation
    /// densities, plus the dominant biome for discrete decisions.
    /// </summary>
    public struct BiomeColumn
    {
        /// <summary>Terrain height in blocks.</summary>
        public float height;
        public Color surfaceColor;
        public Color cliffColor;
        /// <summary>Blended per-column vegetation densities (0..1 chance per sample cell).</summary>
        public float treeDensity;
        public float grassDensity;
        public float boulderDensity;
        /// <summary>1 on the centerline of a trail, fading to 0 at its edge.</summary>
        public float pathMask;
        /// <summary>Index into the manager's biome list.</summary>
        public byte dominantBiome;
        /// <summary>Normalized weight of the dominant biome; falls toward 0.5 at borders.</summary>
        public float dominantWeight;
    }

    /// <summary>
    /// Generated data for one chunk, including a one-column border on every side so
    /// meshing and slope checks never need to guess across chunk seams.
    /// </summary>
    public class ChunkData
    {
        public readonly int chunkSize;

        private readonly BiomeColumn[] columns;
        private readonly int stride;

        public ChunkData(int chunkSize)
        {
            this.chunkSize = chunkSize;
            stride = chunkSize + 2;
            columns = new BiomeColumn[stride * stride];
        }

        /// <summary>
        /// Valid for x and z in [-1, chunkSize].
        /// </summary>
        public BiomeColumn GetColumn(int x, int z)
        {
            return columns[(x + 1) + (z + 1) * stride];
        }

        public void SetColumn(int x, int z, BiomeColumn column)
        {
            columns[(x + 1) + (z + 1) * stride] = column;
        }
    }
}
