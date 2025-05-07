using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace World.Generation
{
    [BurstCompile]
    public struct ChunkJob : IJobParallelFor
    {
        public struct BlockData
        {
            [ReadOnly] public NativeArray<int3> Vertices;
            [ReadOnly] public NativeArray<int> Triangles;
        }
        
        [ReadOnly] public int chunkSize;
        [ReadOnly] public int maxChunkHeight;
        [ReadOnly] public NativeArray<Block> Blocks;
        [ReadOnly] public BlockData blockData;

        // NativeQueues for thread-safe enqueueing
        public NativeQueue<int3>.ParallelWriter VerticesQueue;
        public NativeQueue<int>.ParallelWriter TrianglesQueue;

        public void Execute(int index)
        {
            int x = index / chunkSize;
            int z = index % chunkSize;

            for (int y = 0; y < maxChunkHeight; y++)
            {
                int3 position = new int3(x, y, z);
                int blockIndex = BlockExtensions.GetBlockIndex(position, chunkSize, maxChunkHeight);
                Block block = Blocks[blockIndex];

                if (block.IsEmpty()) continue;

                // Iterate through all six directions
                for (int i = 0; i < 6; i++)
                {
                    Direction direction = (Direction)i;

                    if (IsFaceVisible(direction, position))
                    {
                        CreateFace(direction, position);
                    }
                }
            }
        }

        private bool IsFaceVisible(Direction direction, int3 pos)
        {
            int3 neighborPos = BlockExtensions.GetPositionInDirection(direction, pos.x, pos.y, pos.z);
            if (neighborPos.x >= chunkSize || neighborPos.x < 0 ||
                neighborPos.y >= maxChunkHeight || neighborPos.y < 0 ||
                neighborPos.z >= chunkSize || neighborPos.z < 0)
            {
                // Blocks outside the chunk are considered air
                return true;
            }

            int neighborIndex = BlockExtensions.GetBlockIndex(neighborPos, chunkSize, maxChunkHeight);
            return Blocks[neighborIndex].IsEmpty();
        }

        private void CreateFace(Direction direction, int3 pos)
        {
            // Retrieve face vertices using the block data
            int baseIndex = (int)direction * 4;
            int3 v0 = blockData.Vertices[blockData.Triangles[baseIndex + 0]] + pos;
            int3 v1 = blockData.Vertices[blockData.Triangles[baseIndex + 1]] + pos;
            int3 v2 = blockData.Vertices[blockData.Triangles[baseIndex + 2]] + pos;
            int3 v3 = blockData.Vertices[blockData.Triangles[baseIndex + 3]] + pos;

            // Enqueue vertices
            VerticesQueue.Enqueue(v0);
            VerticesQueue.Enqueue(v1);
            VerticesQueue.Enqueue(v2);
            VerticesQueue.Enqueue(v3);

            // Enqueue triangle indices (0,1,2,0,2,3) relative to current face's vertices
            TrianglesQueue.Enqueue(0);
            TrianglesQueue.Enqueue(1);
            TrianglesQueue.Enqueue(2);
            TrianglesQueue.Enqueue(0);
            TrianglesQueue.Enqueue(2);
            TrianglesQueue.Enqueue(3);
        }
    }
}
