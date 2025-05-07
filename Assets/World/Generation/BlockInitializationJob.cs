using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace World.Generation
{
    [BurstCompile]
    public struct BlockInitializationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> heightMapFlat;
        [ReadOnly] public int chunkSize;
        [ReadOnly] public int maxChunkHeight;
        [ReadOnly] public float heightMultiplier;

        [WriteOnly] public NativeArray<Block> blocks;

        public void Execute(int index)
        {
            int x = index / chunkSize;
            int z = index % chunkSize;

            float heightValue = heightMapFlat[index];
            float yHeight = math.floor(heightValue);
            yHeight = math.clamp(yHeight, 0, maxChunkHeight - 1);

            for (int y = 0; y < maxChunkHeight; y++)
            {
                int blockIndex = BlockExtensions.GetBlockIndex(new int3(x, y, z), chunkSize, maxChunkHeight);
                blocks[blockIndex] = y <= yHeight ? Block.Ground : Block.Air;
            }
        }
    }
}
