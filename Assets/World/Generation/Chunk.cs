using System.Collections;
using System.Linq;

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using World.NoiseGeneration;

namespace World.Generation
{
    public class Chunk : MonoBehaviour
    {

        public MeshFilter _meshFilter;
        public int chunkSize = 32;

        private NoiseGenerator noiseGenerator;
        private int chunkCoordX;
        private int chunkCoordZ;


        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        void Start()
        {
            int seed = 42;
            noiseGenerator = new NoiseGenerator(seed);
            noiseGenerator.SetHeightMultiplier(35f);

            // Calculate chunk coordinates based on position
            chunkCoordX = Mathf.FloorToInt(transform.position.x / chunkSize);
            chunkCoordZ = Mathf.FloorToInt(transform.position.z / chunkSize);
            Vector2Int chunkCoord = new Vector2Int(chunkCoordX, chunkCoordZ);
            float[,] heightMap = noiseGenerator.GenerateHeightMap(chunkSize + 1, chunkSize + 1, chunkCoord, chunkSize);

            var blocks = new NativeArray<Block>(chunkSize * chunkSize * chunkSize, Allocator.TempJob);
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float heightValue = heightMap[x, z];
                    int yHeight = Mathf.FloorToInt(heightValue);

                    for (int y = 0; y < chunkSize; y++)
                    {
                        int index = BlockExtensions.GetBlockIndex(new int3(x, y, z), chunkSize);
                        if (y <= yHeight)
                            blocks[index] = Block.Stone;
                        else
                            blocks[index] = Block.Air;
                    }
                }
            }

            var meshData = new ChunkJob.MeshData
            {
                Vertices = new NativeList<int3>(Allocator.TempJob),
                Triangles = new NativeList<int>(Allocator.TempJob)
            };

            var jobHandle = new ChunkJob
            {
                chunkSize = chunkSize,
                meshData = meshData,
                chunkData = new ChunkJob.ChunkData
                {
                    Blocks = blocks
                },
                blockData = new ChunkJob.BlockData
                {
                    Vertices = BlockData.Vertices,
                    Triangles = BlockData.Triangles
                }
            }.Schedule();

            jobHandle.Complete();
            StartCoroutine(CompleteJob(jobHandle, meshData, blocks));
        }

        IEnumerator CompleteJob(JobHandle jobHandle, ChunkJob.MeshData meshData, NativeArray<Block> blocks)
        {
            // Wait for a frame or condition
            yield return null;

            jobHandle.Complete();

            // updating the mesh
            var mesh = new Mesh
            {
                vertices = meshData.Vertices.AsArray().ToArray().Select(vertex => new Vector3(vertex.x, vertex.y, vertex.z)).ToArray(),
                triangles = meshData.Triangles.AsArray().ToArray()
            };

            meshData.Vertices.Dispose();
            meshData.Triangles.Dispose();
            blocks.Dispose();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            _meshFilter.mesh = mesh;
        }
    }
}