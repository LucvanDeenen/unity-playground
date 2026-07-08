using UnityEngine;

namespace World.Biomes
{
    /// <summary>
    /// 128-bit column solidity mask: bit y set = solid block at height y.
    /// </summary>
    public struct SolidMask
    {
        /// <summary>Heights 0..63.</summary>
        public ulong low;
        /// <summary>Heights 64..127.</summary>
        public ulong high;

        public bool IsEmpty => (low | high) == 0;

        public bool IsSolid(int y)
        {
            if (y < 0 || y > 127)
            {
                return false;
            }

            return y < 64
                ? (low & (1UL << y)) != 0
                : (high & (1UL << (y - 64))) != 0;
        }

        public void SetSolid(int y)
        {
            if (y < 0 || y > 127)
            {
                return;
            }

            if (y < 64)
            {
                low |= 1UL << y;
            }
            else
            {
                high |= 1UL << (y - 64);
            }
        }

        /// <summary>
        /// A mask that is solid from 0 up to and including topY.
        /// </summary>
        public static SolidMask FullTo(int topY)
        {
            SolidMask mask;
            if (topY < 0)
            {
                mask.low = 0;
                mask.high = 0;
            }
            else if (topY < 63)
            {
                mask.low = (1UL << (topY + 1)) - 1;
                mask.high = 0;
            }
            else
            {
                mask.low = ulong.MaxValue;
                mask.high = topY >= 127 ? ulong.MaxValue : (1UL << (topY - 63)) - 1;
            }

            return mask;
        }
    }

    /// <summary>
    /// Per-column result of biome generation: blended height, colors, and vegetation
    /// densities, plus the dominant biome for discrete decisions.
    /// </summary>
    public struct BiomeColumn
    {
        /// <summary>Height in blocks of the topmost solid block.</summary>
        public float height;
        /// <summary>Height above the local continental elevation; use for tree lines.</summary>
        public float localHeight;
        /// <summary>Column solidity; supports air pockets for overhangs.</summary>
        public SolidMask solidMask;
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
