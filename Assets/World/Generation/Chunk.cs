using System.Collections;

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using World.NoiseGeneration;

namespace World.Generation
{
    public class Chunk : MonoBehaviour
    {
        private NoiseGenerator noiseGenerator;
        private Vector2Int chunkCoord;
        private float voxelScale;
        private int chunkSize;
        private int maxChunkHeight;

        public void SetNoiseGenerator(NoiseGenerator noiseGenerator) => this.noiseGenerator = noiseGenerator;
        public void SetChunkCoord(Vector2Int chunkCoord) => this.chunkCoord = chunkCoord;
        public void SetVoxelScale(float voxelScale) => this.voxelScale = voxelScale;
        public void SetChunkSize(int chunkSize, int maxChunkHeight)
        {
            this.chunkSize = chunkSize;
            this.maxChunkHeight = maxChunkHeight;
        }

        private MeshFilter _meshFilter;

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        public IEnumerator GenerateChunk()
        {
            _meshFilter.mesh = new Mesh();

            // Generate the height map
            float[,] heightMap = noiseGenerator.GenerateHeightMap(chunkCoord, chunkSize);

            // Initialize the blocks array
            var blocks = new NativeArray<Block>(chunkSize * maxChunkHeight * chunkSize, Allocator.TempJob);
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float heightValue = heightMap[x, z];
                    int yHeight = Mathf.FloorToInt(heightValue);

                    for (int y = 0; y < maxChunkHeight; y++)
                    {
                        int index = BlockExtensions.GetBlockIndex(new int3(x, y, z), chunkSize, maxChunkHeight);
                        blocks[index] = y <= yHeight ? Block.Ground : Block.Air;
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
                blockData = blockData,
                chunkData = chunkData,
                chunkSize = chunkSize,
                maxChunkHeight = maxChunkHeight,
                VerticesQueue = verticesQueue.AsParallelWriter(),
                TrianglesQueue = trianglesQueue.AsParallelWriter()
            };

            JobHandle jobHandle = chunkJob.Schedule(chunkSize * chunkSize, 64);

            // Wait until the job is complete without blocking the main thread
            while (!jobHandle.IsCompleted)
            {
                yield return null;
            }

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
                combinedVertices.Add(new Vector3(v0.x * voxelScale, v0.y * voxelScale, v0.z * voxelScale));
                combinedVertices.Add(new Vector3(v1.x * voxelScale, v1.y * voxelScale, v1.z * voxelScale));
                combinedVertices.Add(new Vector3(v2.x * voxelScale, v2.y * voxelScale, v2.z * voxelScale));
                combinedVertices.Add(new Vector3(v3.x * voxelScale, v3.y * voxelScale, v3.z * voxelScale));

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

        public void ResetMesh()
        {
            if (_meshFilter.mesh != null)
            {
                _meshFilter.mesh.Clear();
            }
            else
            {
                _meshFilter.mesh = new Mesh();
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 center = transform.position + new Vector3((chunkSize * voxelScale) / 2f, (chunkSize * voxelScale) / 2f, (chunkSize * voxelScale) / 2f);
            Vector3 size = new Vector3(chunkSize * voxelScale, chunkSize * voxelScale, chunkSize * voxelScale);
            Gizmos.DrawWireCube(center, size);
        }
    }
}