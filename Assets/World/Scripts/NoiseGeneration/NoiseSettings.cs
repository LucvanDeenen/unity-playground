using UnityEngine;

namespace World.NoiseGeneration
{
    /// <summary>
    /// ScriptableObject that holds all parameters for terrain noise generation.
    /// Create an asset via Assets > Create > World/Noise Settings, then assign it
    /// to a TerrainManager in the Inspector to tune terrain without editing code.
    /// </summary>
    [CreateAssetMenu(fileName = "NoiseSettings", menuName = "World/Noise Settings", order = 1)]
    public class NoiseSettings : ScriptableObject
    {
        [Header("Octave Settings")]
        [Tooltip("Number of noise octaves to combine. More octaves add finer detail at the cost of performance.")]
        [Range(1, 8)]
        public int octaves = 4;

        [Tooltip("Controls the zoom of terrain features. Smaller values produce broader, more gradual hills.")]
        public float noiseScale = 0.005f;

        [Tooltip("How much each successive octave contributes relative to the previous one (0–1). " +
                 "Lower values make higher-frequency octaves less prominent.")]
        [Range(0f, 1f)]
        public float persistence = 0.5f;

        [Tooltip("How much the frequency increases per octave. Higher values add sharper, finer detail.")]
        [Min(1f)]
        public float lacunarity = 2f;

        [Header("Height Settings")]
        [Tooltip("Multiplier applied to the final noise value to set the overall terrain height range.")]
        public float heightMultiplier = 15f;
    }
}
