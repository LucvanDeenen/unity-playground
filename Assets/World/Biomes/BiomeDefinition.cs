using System.Collections.Generic;
using UnityEngine;

namespace World.Biomes
{
    /// <summary>
    /// Defines one biome: the climate window that selects it, the terrain shape
    /// it produces, and its surface palette.
    /// </summary>
    [System.Serializable]
    public class BiomeDefinition
    {
        public string name = "Biome";

        [Header("Climate window (0..1)")]
        [Tooltip("Temperature band this biome occupies; biomes cross-fade near band edges.")]
        public Vector2 temperatureRange = new Vector2(0f, 1f);
        [Tooltip("Moisture band this biome occupies.")]
        public Vector2 moistureRange = new Vector2(0f, 1f);
        [Tooltip("Relief selects by terrain ruggedness rather than climate; mountains use the top band.")]
        public Vector2 reliefRange = new Vector2(0f, 1f);

        [Header("Terrain shape (in blocks)")]
        public float baseHeight = 10f;
        public float amplitude = 8f;
        [Range(0f, 1f)]
        [Tooltip("0 = local rounded knolls, 1 = broad mountain massifs with ridged crests.")]
        public float ridged = 0f;

        [Header("Surface palette")]
        [Tooltip("Evaluated from the biome's lowest (0) to highest (1) terrain.")]
        public Gradient surfaceGradient = new Gradient();
        [Tooltip("Color of exposed vertical faces (cliffs and block steps).")]
        public Color cliffColor = new Color(0.45f, 0.33f, 0.22f);

        [Header("Vegetation density (0..1 chance per sample cell)")]
        [Range(0f, 1f)] public float treeDensity = 0.3f;
        [Range(0f, 1f)] public float grassDensity = 0.3f;
        [Range(0f, 1f)] public float boulderDensity = 0.1f;

        private const float ClimateFalloff = 0.08f;

        /// <summary>
        /// How strongly this biome applies at the given climate sample. Membership
        /// fades smoothly over a margin at each band edge so neighboring biomes blend
        /// instead of forming hard borders.
        /// </summary>
        public float ClimateWeight(float temperature, float moisture, float relief)
        {
            return AxisWeight(temperature, temperatureRange)
                 * AxisWeight(moisture, moistureRange)
                 * AxisWeight(relief, reliefRange);
        }

        private static float AxisWeight(float value, Vector2 range)
        {
            float rise = range.x <= 0.001f
                ? 1f
                : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(range.x - ClimateFalloff, range.x + ClimateFalloff, value));
            float fall = range.y >= 0.999f
                ? 1f
                : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(range.y + ClimateFalloff, range.y - ClimateFalloff, value));
            return rise * fall;
        }
    }

    /// <summary>
    /// Built-in biome set used when the manager's biome list is empty.
    /// </summary>
    public static class BiomeDefaults
    {
        public static List<BiomeDefinition> CreateDefaults()
        {
            return new List<BiomeDefinition>
            {
                new BiomeDefinition
                {
                    name = "Plains",
                    temperatureRange = new Vector2(0.25f, 0.75f),
                    moistureRange = new Vector2(0f, 0.5f),
                    reliefRange = new Vector2(0f, 0.6f),
                    baseHeight = 12f,
                    amplitude = 18f,
                    ridged = 0f,
                    surfaceGradient = MakeGradient(
                        (new Color(0.42f, 0.82f, 0.08f), 0f),
                        (new Color(0.56f, 0.88f, 0.12f), 1f)),
                    cliffColor = new Color(0.5f, 0.34f, 0.2f),
                    treeDensity = 0.04f,
                    grassDensity = 0.30f,
                    boulderDensity = 0.01f,
                },
                new BiomeDefinition
                {
                    name = "Forest",
                    temperatureRange = new Vector2(0.25f, 0.75f),
                    moistureRange = new Vector2(0.5f, 1f),
                    reliefRange = new Vector2(0f, 0.6f),
                    baseHeight = 13f,
                    amplitude = 26f,
                    ridged = 0.15f,
                    surfaceGradient = MakeGradient(
                        (new Color(0.28f, 0.7f, 0.06f), 0f),
                        (new Color(0.36f, 0.76f, 0.09f), 0.5f),
                        (new Color(0.46f, 0.82f, 0.12f), 1f)),
                    cliffColor = new Color(0.48f, 0.33f, 0.2f),
                    treeDensity = 0.65f,
                    grassDensity = 0.30f,
                    boulderDensity = 0.01f,
                },
                new BiomeDefinition
                {
                    name = "Desert",
                    temperatureRange = new Vector2(0.75f, 1f),
                    moistureRange = new Vector2(0f, 1f),
                    reliefRange = new Vector2(0f, 0.6f),
                    baseHeight = 11f,
                    amplitude = 13f,
                    ridged = 0.05f,
                    surfaceGradient = MakeGradient(
                        (new Color(0.62f, 0.8f, 0.16f), 0f),
                        (new Color(0.75f, 0.85f, 0.24f), 1f)),
                    cliffColor = new Color(0.55f, 0.42f, 0.22f),
                    treeDensity = 0f,
                    grassDensity = 0.03f,
                    boulderDensity = 0.03f,
                },
                new BiomeDefinition
                {
                    name = "Tundra",
                    temperatureRange = new Vector2(0f, 0.25f),
                    moistureRange = new Vector2(0f, 1f),
                    reliefRange = new Vector2(0f, 0.6f),
                    baseHeight = 13f,
                    amplitude = 22f,
                    ridged = 0.2f,
                    surfaceGradient = MakeGradient(
                        (new Color(0.36f, 0.68f, 0.24f), 0f),
                        (new Color(0.52f, 0.78f, 0.32f), 0.55f),
                        (new Color(0.68f, 0.85f, 0.45f), 1f)),
                    cliffColor = new Color(0.48f, 0.4f, 0.28f),
                    treeDensity = 0.08f,
                    grassDensity = 0.10f,
                    boulderDensity = 0.05f,
                },
                new BiomeDefinition
                {
                    name = "Mountains",
                    temperatureRange = new Vector2(0f, 1f),
                    moistureRange = new Vector2(0f, 1f),
                    reliefRange = new Vector2(0.6f, 1f),
                    baseHeight = 18f,
                    amplitude = 70f,
                    ridged = 0.9f,
                    surfaceGradient = MakeGradient(
                        (new Color(0.46f, 0.45f, 0.44f), 0f),
                        (new Color(0.6f, 0.6f, 0.6f), 0.5f),
                        (new Color(0.88f, 0.9f, 0.93f), 0.72f),
                        (new Color(0.98f, 0.99f, 1f), 1f)),
                    cliffColor = new Color(0.35f, 0.34f, 0.36f),
                    treeDensity = 0.04f,
                    grassDensity = 0.06f,
                    boulderDensity = 0.06f,
                },
            };
        }

        private static Gradient MakeGradient(params (Color color, float time)[] keys)
        {
            var colorKeys = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                colorKeys[i] = new GradientColorKey(keys[i].color, keys[i].time);
            }

            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
