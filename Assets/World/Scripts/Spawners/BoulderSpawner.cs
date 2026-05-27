using UnityEngine;

namespace World.Spawners
{
    /// <summary>
    /// Spawns boulders on the terrain using the same noise-driven,
    /// placement-managed pipeline as every other spawner.
    /// </summary>
    public class BoulderSpawner : Spawner
    {
        [Header("Boulder Settings")]
        [Tooltip("Boulder prefabs to randomly pick from when spawning.")]
        [SerializeField] private GameObject[] boulderPrefabs;

        [Tooltip("World-height range in which boulders can appear (y-min, y-max).")]
        [SerializeField] private Vector2 heightRange = new Vector2(5f, 150f);

        [Tooltip("Minimum world-space distance between any two boulders.")]
        [SerializeField] private float minDistanceBetweenBoulders = 8f;

        [Tooltip("Noise threshold for spawning: lower value = fewer, more scattered boulders.")]
        [SerializeField, Range(0f, 1f)] private float spawnThreshold = 0.08f;

        [Tooltip("Noise scale used to cluster boulders. Smaller = broader clusters.")]
        [SerializeField] private float noiseScale = 0.04f;

        // ─── Spawner overrides ────────────────────────────────────────────────

        protected override GameObject[] Prefabs               => boulderPrefabs;
        protected override Vector2      HeightRange           => heightRange;
        protected override float        MinDistanceBetweenObjects => minDistanceBetweenBoulders;

        // SpawnChance is not used directly — we only look at SpawnThreshold
        protected override float SpawnChance    => spawnThreshold;
        protected override float SpawnThreshold => spawnThreshold;
        protected override float NoiseScale     => noiseScale;

        protected override int StepSize(float voxelScale)
            => Mathf.Max(1, Mathf.RoundToInt(minDistanceBetweenBoulders / voxelScale));

        /// <summary>
        /// Boulders sit upright (no prefab-specific -90° tilt needed).
        /// Y-rotation is snapped to 90° increments for a natural, blocky look.
        /// </summary>
        protected override Quaternion GetConstrainedRotation(System.Random prng)
        {
            float randomY  = (float)prng.NextDouble() * 360f;
            float snappedY = Mathf.Round(randomY / 90f) * 90f;
            return Quaternion.Euler(0f, snappedY, 0f);
        }
    }
}
