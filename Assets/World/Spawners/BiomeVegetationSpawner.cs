using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using World.Biomes;

namespace World.Spawners
{
    /// <summary>
    /// Spawns vegetation and props driven by the per-column biome densities in
    /// the chunk data. Trees and boulders are prefab instances; grass is baked
    /// into a single mesh per chunk to keep object counts low.
    /// </summary>
    public class BiomeVegetationSpawner : Spawner
    {
        [Serializable]
        public class CategorySettings
        {
            [Tooltip("Minimum distance between objects in world units; also sets the sampling grid.")]
            public float minSpacing = 6f;
            [Tooltip("Scale of the clustering noise; lower values make larger clusters.")]
            public float clusterNoiseScale = 0.05f;
            [Tooltip("Cluster noise window: below x nothing spawns, above y the biome density fully applies.")]
            public Vector2 clusterWindow = new Vector2(0.35f, 0.6f);
            [Tooltip("Maximum height difference to neighboring columns, in blocks.")]
            public int maxSlope = 1;
            [Tooltip("No spawns above this height in blocks (tree line).")]
            public float maxHeightBlocks = 32f;
        }

        [Header("Trees")]
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private CategorySettings treeSettings = new CategorySettings
        {
            minSpacing = 4.5f,
            clusterNoiseScale = 0.05f,
            clusterWindow = new Vector2(0.35f, 0.6f),
            maxSlope = 1,
            maxHeightBlocks = 30f,
        };

        [Header("Grass (baked into one mesh per chunk)")]
        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private CategorySettings grassSettings = new CategorySettings
        {
            minSpacing = 1.5f,
            clusterNoiseScale = 0.08f,
            clusterWindow = new Vector2(0.25f, 0.5f),
            maxSlope = 2,
            maxHeightBlocks = 40f,
        };

        [Header("Boulders")]
        [SerializeField] private GameObject[] boulderPrefabs;
        [SerializeField] private CategorySettings boulderSettings = new CategorySettings
        {
            minSpacing = 10f,
            clusterNoiseScale = 0.03f,
            clusterWindow = new Vector2(0.45f, 0.7f),
            maxSlope = 3,
            maxHeightBlocks = 999f,
        };

        // Salts decorrelate each category's random rolls and cluster noise.
        private const int TreeSalt = 1013;
        private const int GrassSalt = 2027;
        private const int BoulderSalt = 3041;

        public override void Spawn(GameObject chunkObject, ChunkData chunkData, float voxelScale, Vector2Int chunkCoord)
        {
            if (placementManager == null || noiseGenerator == null)
            {
                Debug.LogError($"{nameof(BiomeVegetationSpawner)} is missing its placement manager or noise generator.");
                return;
            }

            SpawnPrefabs(treePrefabs, treeSettings, TreeSalt, column => column.treeDensity, chunkObject, chunkData, voxelScale, chunkCoord);
            SpawnPrefabs(boulderPrefabs, boulderSettings, BoulderSalt, column => column.boulderDensity, chunkObject, chunkData, voxelScale, chunkCoord);
            BakeGrass(chunkObject, chunkData, voxelScale, chunkCoord);
        }

        private void SpawnPrefabs(GameObject[] prefabs, CategorySettings settings, int salt, Func<BiomeColumn, float> density, GameObject chunkObject, ChunkData chunkData, float voxelScale, Vector2Int chunkCoord)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            Vector3 chunkPosition = chunkObject.transform.position;
            int step = Mathf.Max(1, Mathf.RoundToInt(settings.minSpacing / voxelScale));

            for (int x = 0; x < chunkData.chunkSize; x += step)
            {
                for (int z = 0; z < chunkData.chunkSize; z += step)
                {
                    if (!TryPickSpawn(chunkData, settings, salt, density, voxelScale, chunkCoord, chunkPosition, x, z, out Vector3 position, out System.Random prng))
                    {
                        continue;
                    }

                    if (!placementManager.IsPositionAvailable(position, settings.minSpacing))
                    {
                        continue;
                    }

                    GameObject prefab = prefabs[prng.Next(prefabs.Length)];
                    GameObject instance = Instantiate(prefab, position, GetConstrainedRotation(prng), chunkObject.transform);
                    instance.transform.localScale *= (float)(prng.NextDouble() * 0.2 + 0.9);
                    placementManager.RegisterObjectPosition(position);
                }
            }
        }

        private void BakeGrass(GameObject chunkObject, ChunkData chunkData, float voxelScale, Vector2Int chunkCoord)
        {
            if (grassPrefabs == null || grassPrefabs.Length == 0)
            {
                return;
            }

            var sourceMeshes = new List<Mesh>();
            var sourceMatrices = new List<Matrix4x4>();
            Material material = null;
            foreach (GameObject prefab in grassPrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                sourceMeshes.Add(filter.sharedMesh);
                // Preserve any transform the mesh node carries inside the prefab.
                sourceMatrices.Add(prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix);

                if (material == null)
                {
                    MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        material = renderer.sharedMaterial;
                    }
                }
            }

            if (sourceMeshes.Count == 0)
            {
                return;
            }

            var combines = new List<CombineInstance>();
            Vector3 chunkPosition = chunkObject.transform.position;
            int step = Mathf.Max(1, Mathf.RoundToInt(grassSettings.minSpacing / voxelScale));

            for (int x = 0; x < chunkData.chunkSize; x += step)
            {
                for (int z = 0; z < chunkData.chunkSize; z += step)
                {
                    if (!TryPickSpawn(chunkData, grassSettings, GrassSalt, column => column.grassDensity, voxelScale, chunkCoord, chunkPosition, x, z, out Vector3 position, out System.Random prng))
                    {
                        continue;
                    }

                    // Grass skips the placement manager: registering thousands of
                    // tufts would crowd out trees and bloat the spatial grid.
                    int sourceIndex = prng.Next(sourceMeshes.Count);
                    float scale = (float)(prng.NextDouble() * 0.4 + 0.8);
                    Quaternion rotation = Quaternion.Euler(-90f, (float)(prng.NextDouble() * 360.0), 0f);
                    Matrix4x4 matrix = Matrix4x4.TRS(position - chunkPosition, rotation, Vector3.one * scale) * sourceMatrices[sourceIndex];
                    combines.Add(new CombineInstance { mesh = sourceMeshes[sourceIndex], transform = matrix });
                }
            }

            if (combines.Count == 0)
            {
                return;
            }

            Mesh baked = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };
            baked.CombineMeshes(combines.ToArray(), true, true);

            GameObject foliage = new GameObject("Foliage");
            foliage.transform.SetParent(chunkObject.transform, false);
            foliage.AddComponent<MeshFilter>().mesh = baked;
            MeshRenderer bakedRenderer = foliage.AddComponent<MeshRenderer>();
            bakedRenderer.sharedMaterial = material;
            bakedRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private bool TryPickSpawn(ChunkData chunkData, CategorySettings settings, int salt, Func<BiomeColumn, float> density, float voxelScale, Vector2Int chunkCoord, Vector3 chunkPosition, int x, int z, out Vector3 position, out System.Random prng)
        {
            position = default;
            prng = null;

            BiomeColumn column = chunkData.GetColumn(x, z);
            if (column.height > settings.maxHeightBlocks)
            {
                return false;
            }

            // Keep trails clear of vegetation.
            if (column.pathMask > 0.25f)
            {
                return false;
            }

            float baseDensity = density(column);
            if (baseDensity <= 0f)
            {
                return false;
            }

            float worldX = chunkPosition.x + x * voxelScale;
            float worldZ = chunkPosition.z + z * voxelScale;

            float cluster = noiseGenerator.GetNormalizedNoiseValue(worldX + salt, worldZ + salt, settings.clusterNoiseScale);
            float clusterWeight = Mathf.InverseLerp(settings.clusterWindow.x, settings.clusterWindow.y, cluster);
            float probability = baseDensity * clusterWeight;
            if (probability <= 0f)
            {
                return false;
            }

            int gridX = chunkCoord.x * chunkData.chunkSize + x;
            int gridZ = chunkCoord.y * chunkData.chunkSize + z;
            int hash = (noiseGenerator.Seed + gridX * 73856093) ^ (gridZ * 19349663) ^ (salt * 83492791);
            prng = new System.Random(hash);
            if (prng.NextDouble() >= probability)
            {
                return false;
            }

            if (MaxNeighborSlope(chunkData, x, z) > settings.maxSlope)
            {
                return false;
            }

            // The walkable surface is the top face of the block, one voxel above its base.
            float worldY = (Mathf.FloorToInt(column.height) + 1) * voxelScale;
            position = new Vector3(worldX, worldY, worldZ);
            return true;
        }

        private static int MaxNeighborSlope(ChunkData chunkData, int x, int z)
        {
            int height = Mathf.FloorToInt(chunkData.GetColumn(x, z).height);
            int slope = Mathf.Abs(height - Mathf.FloorToInt(chunkData.GetColumn(x - 1, z).height));
            slope = Mathf.Max(slope, Mathf.Abs(height - Mathf.FloorToInt(chunkData.GetColumn(x + 1, z).height)));
            slope = Mathf.Max(slope, Mathf.Abs(height - Mathf.FloorToInt(chunkData.GetColumn(x, z - 1).height)));
            slope = Mathf.Max(slope, Mathf.Abs(height - Mathf.FloorToInt(chunkData.GetColumn(x, z + 1).height)));
            return slope;
        }
    }
}
