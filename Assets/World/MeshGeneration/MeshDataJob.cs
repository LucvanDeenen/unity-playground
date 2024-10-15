using World.Terrain;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace World.MeshGeneration
{
    [BurstCompile]
    public struct MeshDataJob : IJobParallelFor
    {
        public int mapSize;

        [ReadOnly]
        public NativeArray<float> heightMap;

        [WriteOnly]
        public NativeArray<VertexData> vertexData;

        public void Execute(int index)
        {
            int x = index % mapSize;
            int y = index / mapSize;

            float height = heightMap[index];

            VertexData data = new VertexData
            {
                position = new float3(x, height, y),
                normal = new float3(0, 1, 0),
                uv = new float2(x / (float)mapSize, y / (float)mapSize)
            };

            vertexData[index] = data;
        }
    }
}