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


        [Header("Materials")]
        public Gradient terrainGradient;
        public Material voxelMaterial;
        public Color wallColor;

        private float gradientMaxHeight = 100f;
        private float gradientMinHeight = 0f;

        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;

        void Awake()
        {
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public IEnumerator GenerateChunk()
        {
            _meshFilter.mesh = new Mesh();

            // Prepare NativeArray for height map
            NativeArray<float> heightMapFlat = new NativeArray<float>(chunkSize * chunkSize, Allocator.TempJob);

            // Create HeightMapJob
            HeightMapJob heightMapJob = new HeightMapJob();

            // Schedule HeightMapJob through NoiseGenerator
            JobHandle heightMapJobHandle = noiseGenerator.GenerateHeightMapAsync(chunkCoord, chunkSize, heightMapFlat, heightMapJob);

            // Initialize NativeArray for blocks
            NativeArray<Block> blocks = new NativeArray<Block>(chunkSize * maxChunkHeight * chunkSize, Allocator.TempJob);

            // Create BlockInitializationJob
            BlockInitializationJob blockInitJob = new BlockInitializationJob
            {
                heightMapFlat = heightMapFlat,
                chunkSize = chunkSize,
                maxChunkHeight = maxChunkHeight,
                heightMultiplier = noiseGenerator.GetHeightMultiplier(),
                blocks = blocks
            };

            // Schedule BlockInitializationJob with dependency on HeightMapJob
            JobHandle blockInitJobHandle = blockInitJob.Schedule(chunkSize * chunkSize, 64, heightMapJobHandle);

            // Create the queues outside the job
            NativeQueue<int3> verticesQueue = new NativeQueue<int3>(Allocator.TempJob);
            NativeQueue<int> trianglesQueue = new NativeQueue<int>(Allocator.TempJob);

            // Create ChunkJob
            ChunkJob chunkJob = new ChunkJob
            {
                chunkSize = chunkSize,
                maxChunkHeight = maxChunkHeight,
                Blocks = blocks,
                blockData = new ChunkJob.BlockData
                {
                    Vertices = BlockData.Vertices,
                    Triangles = BlockData.Triangles
                },
                VerticesQueue = verticesQueue.AsParallelWriter(),
                TrianglesQueue = trianglesQueue.AsParallelWriter()
            };

            // Schedule ChunkJob with dependency on BlockInitializationJob
            JobHandle chunkJobHandle = chunkJob.Schedule(chunkSize * chunkSize, 64, blockInitJobHandle);

            // Wait until the ChunkJob is complete without blocking the main thread
            while (!chunkJobHandle.IsCompleted)
            {
                yield return null;
            }

            // Ensure all jobs are completed
            chunkJobHandle.Complete();

            // After ChunkJob is done, cache the height map
            noiseGenerator.CacheHeightMap(chunkCoord, chunkSize, chunkSize, heightMapFlat);

            // Dispose of the height map NativeArray
            heightMapFlat.Dispose();

            // Combine all MeshData
            var combinedVertices = new NativeList<Vector3>(Allocator.Temp);
            var combinedTriangles = new NativeList<int>(Allocator.Temp);

            int currentVertexIndex = 0;

            // Access the queued vertices and triangles
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

            // Assign the mesh to the MeshFilter and MeshCollider
            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;

            // Assign vertex colors based on height
            AssignVertexColors(mesh);
        }

        /// <summary>
        /// Assigns vertex colors to the mesh based on their height using the defined gradient.
        /// </summary>
        /// <param name="mesh">The mesh to assign colors to.</param>
        private void AssignVertexColors(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                float height = vertices[i].y / voxelScale;
                float normalizedHeight = Mathf.InverseLerp(gradientMinHeight, gradientMaxHeight, height);
                normalizedHeight = Mathf.Clamp01(normalizedHeight);
                Color vertexColor = terrainGradient.Evaluate(normalizedHeight);
                colors[i] = vertexColor;
            }

            mesh.colors = colors;
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
            Vector3 center = transform.position + new Vector3((chunkSize * voxelScale) / 2f, (maxChunkHeight * voxelScale) / 2f, (chunkSize * voxelScale) / 2f);
            Vector3 size = new Vector3(chunkSize * voxelScale, maxChunkHeight * voxelScale, chunkSize * voxelScale);
            Gizmos.DrawWireCube(center, size);
        }

        public void AssignMeshCollider(bool enable)
        {
            _meshCollider.enabled = enable;
        }
    }
}
