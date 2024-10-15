using World.MeshGeneration;
using World.Terrain;
using Unity.Collections;
using World.NoiseGeneration;
using Unity.Jobs;

namespace World.Managers
{
    /// <summary>
    /// Manages world terrain chunks around the player.
    /// </summary>
    public class WorldTerrainManager : TerrainManager
    {
        protected override void Start()
        {
            renderDistance = 1;
            base.Start();
            
            noiseGenerator.SetHeightMultiplier(25f);
        }

        protected override void GenerateTerrain(Chunk chunk)
        {
            int mapSize = chunk.chunkSize + 1;
            int totalSize = mapSize * mapSize;

            // Create NativeArrays
            NativeArray<float> heightMap = new NativeArray<float>(totalSize, Allocator.TempJob);
            NativeArray<VertexData> vertexData = new NativeArray<VertexData>(totalSize, Allocator.TempJob);

            // Schedule jobs
            var heightMapJob = new HeightMapJob
            {
                mapSize = mapSize,
                chunkCoord = chunk.chunkCoord,
                chunkSize = chunk.chunkSize,
                seed = seed,
                heightMap = heightMap
            };
            JobHandle heightMapHandle = heightMapJob.Schedule(totalSize, 64);

            var meshDataJob = new MeshDataJob
            {
                mapSize = mapSize,
                heightMap = heightMap,
                vertexData = vertexData
            };
            JobHandle meshDataHandle = meshDataJob.Schedule(totalSize, 64, heightMapHandle);

            // Complete jobs
            meshDataHandle.Complete();

            // Use results
            MeshData meshData = meshGenerator.CreateMeshDataFromJob(vertexData);
            // chunk.UpdateChunkMesh(meshData);

            // Dispose of NativeArrays
            heightMap.Dispose();
            vertexData.Dispose();
        }
    }
}
