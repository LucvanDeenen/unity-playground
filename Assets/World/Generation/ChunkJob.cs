using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace World.Generation
{
    [BurstCompile]
    public struct ChunkJob : IJobParallelFor
    {
        public struct MeshData
        {

            public NativeList<int3> Vertices { get; set; }
            public NativeList<int> Triangles { get; set; }
        }

        public struct BlockData
        {

            public NativeArray<int3> Vertices { get; set; }
            public NativeArray<int> Triangles { get; set; }

        }

        public struct ChunkData
        {

            public NativeArray<Block> Blocks { get; set; }

        }

        [ReadOnly] public int chunkSize;
        [ReadOnly] public int maxChunkHeight;
        [ReadOnly] public NativeArray<Block> Blocks;
        [ReadOnly] public ChunkData chunkData;
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

                for (int i = 0; i < 6; i++)
                {
                    var direction = (Direction)i;

                    if (Check(direction, position))
                    {
                        CreateFace(direction, position);
                    }
                }
            }
        }

        private bool Check(Direction direction, int3 pos)
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
            return chunkData.Blocks[neighborIndex].IsEmpty();
        }

        private void CreateFace(Direction direction, int3 pos)
        {
            // Retrieve face vertices using the block data
            int3 v0 = blockData.Vertices[blockData.Triangles[(int)direction * 4 + 0]] + pos;
            int3 v1 = blockData.Vertices[blockData.Triangles[(int)direction * 4 + 1]] + pos;
            int3 v2 = blockData.Vertices[blockData.Triangles[(int)direction * 4 + 2]] + pos;
            int3 v3 = blockData.Vertices[blockData.Triangles[(int)direction * 4 + 3]] + pos;

            // Enqueue vertices
            VerticesQueue.Enqueue(v0);
            VerticesQueue.Enqueue(v1);
            VerticesQueue.Enqueue(v2);
            VerticesQueue.Enqueue(v3);

            // Enqueue triangle indices as relative placeholders (0,1,2,0,2,3)
            TrianglesQueue.Enqueue(0);
            TrianglesQueue.Enqueue(1);
            TrianglesQueue.Enqueue(2);
            TrianglesQueue.Enqueue(0);
            TrianglesQueue.Enqueue(2);
            TrianglesQueue.Enqueue(3);
        }
    }
}