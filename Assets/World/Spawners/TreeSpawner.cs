using UnityEngine;

namespace World.Spawners
{
    /// <summary>
    /// Handles spawning of trees on the voxel terrain.
    /// </summary>
    public class TreeSpawner : Spawner
    {
        [Header("Tree Settings")]
        [Tooltip("The list of tree prefabs to spawn on the terrain.")]
        [SerializeField]
        private GameObject[] treePrefabs;

        [SerializeField]
        private Vector2 heightRange = new Vector2(10f, 50f);

        [SerializeField]
        private float minDistanceBetweenTrees = 10f;

        [SerializeField, Range(0f, 1f)]
        private float spawnChance = 0.25f;

        [SerializeField, Range(0f, 1f)]
        private float spawnThreshold = 0.3f; 

        [SerializeField]
        private float noiseScale = 0.05f;

        protected override GameObject[] Prefabs => treePrefabs;

        protected override Vector2 HeightRange => heightRange;

        protected override float MinDistanceBetweenObjects => minDistanceBetweenTrees;

        protected override float SpawnChance => spawnChance;

        protected override float SpawnThreshold => spawnThreshold;

        protected override float NoiseScale => noiseScale;

        protected override int StepSize(float voxelScale)
        {
            return Mathf.Max(1, Mathf.RoundToInt(MinDistanceBetweenObjects / voxelScale));
        }

    }
}
