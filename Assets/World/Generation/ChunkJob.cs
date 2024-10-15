using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace World.Generation
{
    [BurstCompile(CompileSynchronously = true)]
    public struct ChunkJob : IJobParallelFor
    {
        public int chunkSize;

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

        [WriteOnly] public MeshData meshData;

        [ReadOnly] public ChunkData chunkData;
        [ReadOnly] public BlockData blockData;

        private int vCount;

        public void Execute(int index)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = 0; y < chunkSize; y++)
                    {
                        if (chunkData.Blocks[BlockExtensions.GetBlockIndex(new int3(x, y, z), chunkSize)].IsEmpty()) continue;

                        for (int i = 0; i < 6; i++)
                        {
                            var direction = (Direction)i;

                            if (Check(BlockExtensions.GetPositionInDirection(direction, x, y, z)))
                            {
                                CreateFace(direction, new int3(x, y, z));
                            }
                        }
                    }
                }
            }
        }

        private void CreateFace(Direction direction, int3 pos)
        {
            var _vertices = GetFaceVertices(direction, 1, pos);

            meshData.Vertices.AddRange(_vertices);

            _vertices.Dispose();

            vCount += 4;

            meshData.Triangles.Add(vCount - 4);
            meshData.Triangles.Add(vCount - 4 + 1);
            meshData.Triangles.Add(vCount - 4 + 2);
            meshData.Triangles.Add(vCount - 4);
            meshData.Triangles.Add(vCount - 4 + 2);
            meshData.Triangles.Add(vCount - 4 + 3);
        }

        private bool Check(int3 position)
        {
            if (position.x >= chunkSize || position.x < 0 ||
            position.y >= chunkSize || position.y < 0 ||
            position.z >= chunkSize || position.z < 0)
            {
                // Assume blocks outside the chunk are air
                return true;
            }

            int index = BlockExtensions.GetBlockIndex(position, chunkSize);
            return chunkData.Blocks[index].IsEmpty();
        }

        public NativeArray<int3> GetFaceVertices(Direction direction, int scale, int3 pos)
        {
            var faceVertices = new NativeArray<int3>(4, Allocator.Temp);

            for (int i = 0; i < 4; i++)
            {
                var index = blockData.Triangles[(int)direction * 4 + i];
                faceVertices[i] = blockData.Vertices[index] * scale + pos;
            }

            return faceVertices;
        }
    }
}