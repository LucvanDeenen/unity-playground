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
            StartCoroutine(GenerateChunk());
        }

        IEnumerator GenerateChunk()
        {
            int seed = 42;
            noiseGenerator = new NoiseGenerator(seed);
            noiseGenerator.SetHeightMultiplier(10f);

            // Calculate chunk coordinates based on position
            chunkCoordX = Mathf.FloorToInt(transform.position.x / chunkSize);
            chunkCoordZ = Mathf.FloorToInt(transform.position.z / chunkSize);
            Vector2Int chunkCoord = new Vector2Int(chunkCoordX, chunkCoordZ);

            // Generate the height map
            float[,] heightMap = noiseGenerator.GenerateHeightMap(chunkSize + 1, chunkSize + 1, chunkCoord, chunkSize);

            // Initialize the blocks array
            var blocks = new NativeArray<Block>(chunkSize * chunkSize * chunkSize, Allocator.TempJob);
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float heightValue = heightMap[x, z];
                    int yHeight = Mathf.FloorToInt(heightValue);
                    yHeight = math.clamp(yHeight, 0, chunkSize - 1); // Clamp to prevent out-of-bounds

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

            // Prepare block data
            var blockData = new ChunkJob.BlockData
            {
                Vertices = BlockData.Vertices,
                Triangles = BlockData.Triangles
            };

            // Initialize NativeQueues for vertices and triangles
            var verticesQueue = new NativeQueue<int3>(Allocator.TempJob);
            var trianglesQueue = new NativeQueue<int>(Allocator.TempJob);

            // Prepare ChunkData
            var chunkData = new ChunkJob.ChunkData
            {
                Blocks = blocks
            };

            // Schedule the parallel job
            ChunkJob chunkJob = new ChunkJob
            {
                chunkSize = chunkSize,
                chunkData = chunkData,
                blockData = blockData,
                VerticesQueue = verticesQueue.AsParallelWriter(),
                TrianglesQueue = trianglesQueue.AsParallelWriter()
            };

            JobHandle jobHandle = chunkJob.Schedule(chunkSize * chunkSize, 64); // Batch size of 64

            // Wait until the job is complete
            yield return new WaitUntil(() => jobHandle.IsCompleted);

            // Ensure the job is completed
            jobHandle.Complete();

            // Combine all MeshData
            var combinedVertices = new NativeList<Vector3>(Allocator.Temp);
            var combinedTriangles = new NativeList<int>(Allocator.Temp);

            int currentVertexIndex = 0;

            // Process all queued vertices and triangles using explicit loops
            while (verticesQueue.Count > 0)
            {
                // Dequeue four vertices per face
                if (verticesQueue.Count < 4)
                {
                    Debug.LogWarning("Incomplete face vertices detected.");
                    break; // Avoid dequeuing incomplete faces
                }

                int3 v0 = verticesQueue.Dequeue();
                int3 v1 = verticesQueue.Dequeue();
                int3 v2 = verticesQueue.Dequeue();
                int3 v3 = verticesQueue.Dequeue();

                // Add vertices to the combined list
                combinedVertices.Add(new Vector3(v0.x, v0.y, v0.z));
                combinedVertices.Add(new Vector3(v1.x, v1.y, v1.z));
                combinedVertices.Add(new Vector3(v2.x, v2.y, v2.z));
                combinedVertices.Add(new Vector3(v3.x, v3.y, v3.z));

                // Define two triangles for the face with correct indices
                combinedTriangles.Add(currentVertexIndex);
                combinedTriangles.Add(currentVertexIndex + 1);
                combinedTriangles.Add(currentVertexIndex + 2);

                combinedTriangles.Add(currentVertexIndex);
                combinedTriangles.Add(currentVertexIndex + 2);
                combinedTriangles.Add(currentVertexIndex + 3);

                // Update the vertex index offset
                currentVertexIndex += 4;
            }

            // Dispose of the queues
            verticesQueue.Dispose();
            trianglesQueue.Dispose();

            // Create the final mesh
            var mesh = new Mesh
            {
                vertices = combinedVertices.AsArray().ToArray(),
                triangles = combinedTriangles.AsArray().ToArray()
            };

            // Dispose of native lists and blocks array
            combinedVertices.Dispose();
            combinedTriangles.Dispose();
            blocks.Dispose();

            // Recalculate mesh properties
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            // Assign the mesh to the MeshFilter
            _meshFilter.mesh = mesh;
        }
    }
}