using UnityEngine;

namespace World.Spawners
{
    /// <summary>
    /// Spawns ground foliage (grass, ferns, flowers, etc.) using the same noise-driven,
    /// placement-managed pipeline as every other spawner.
    /// Foliage is dense and low-lying, so it uses a tight step size and a high threshold.
    /// </summary>
    public class FoliageSpawner : Spawner
    {
        [Header("Foliage Settings")]
        [Tooltip("Foliage prefabs to randomly pick from when spawning.")]
        [SerializeField] private GameObject[] foliagePrefabs;

        [Tooltip("World-height range in which foliage can appear (y-min, y-max).")]
        [SerializeField] private Vector2 heightRange = new Vector2(3f, 40f);

        [Tooltip("Minimum world-space distance between foliage instances.")]
        [SerializeField] private float minDistanceBetweenFoliage = 1.5f;

        [Tooltip("Noise threshold for spawning: higher value = denser coverage.")]
        [SerializeField, Range(0f, 1f)] private float spawnThreshold = 0.55f;

        [Tooltip("Noise scale used to cluster foliage patches. Smaller = broader patches.")]
        [SerializeField] private float noiseScale = 0.08f;

        // ─── Spawner overrides ────────────────────────────────────────────────

        protected override GameObject[] Prefabs               => foliagePrefabs;
        protected override Vector2      HeightRange           => heightRange;
        protected override float        MinDistanceBetweenObjects => minDistanceBetweenFoliage;

        protected override float SpawnChance    => spawnThreshold;
        protected override float SpawnThreshold => spawnThreshold;
        protected override float NoiseScale     => noiseScale;

        protected override int StepSize(float voxelScale)
            => Mathf.Max(1, Mathf.RoundToInt(minDistanceBetweenFoliage / voxelScale));

        /// <summary>
        /// Foliage stands upright.  Y-rotation is fully randomized so each blade
        /// or plant faces a different direction for a natural look.
        /// </summary>
        protected override Quaternion GetConstrainedRotation(System.Random prng)
        {
            float randomY = (float)prng.NextDouble() * 360f;
            return Quaternion.Euler(-90f, randomY, 0f);
        }
    }
}
