using System.Collections.Generic;
using UnityEngine;
using World.NoiseGeneration;

namespace World.Biomes
{
    /// <summary>
    /// Samples climate noise per column, blends biome terrain parameters by climate
    /// weight, and produces the per-chunk data consumed by meshing and spawning.
    /// </summary>
    public class BiomeGenerator
    {
        /// <summary>
        /// Generation options provided by the terrain manager.
        /// </summary>
        public class Settings
        {
            public float seaLevel;
            public bool generatePaths;
            public Color pathColor;
            public float pathWidth;
            public float pathDepth;
            public float terraceHeight;
            public bool useFlatGroundColor;
            public Color flatGroundColor;
            public float overhangStrength;
            public float elevationRange;
            public bool generateLandmarks;
            public float boulderMountainHeight;
            public float valleyDepth;
        }

        // Fractal noise clusters around 0.5, so the raw climate values would almost
        // never reach the extremes where deserts, tundra, and mountains live.
        private const float ClimateContrast = 1.6f;

        private readonly NoiseGenerator noise;
        private readonly IReadOnlyList<BiomeDefinition> biomes;
        private readonly Settings settings;

        /// <summary>Water level in blocks; reserved for the future water milestone.</summary>
        public float SeaLevel => settings.seaLevel;

        public BiomeGenerator(NoiseGenerator noise, IReadOnlyList<BiomeDefinition> biomes, Settings settings)
        {
            this.noise = noise;
            this.biomes = biomes;
            this.settings = settings;
        }

        /// <summary>
        /// Generates biome data for a chunk, including its one-column border.
        /// All sampling is in world coordinates, so neighboring chunks agree exactly.
        /// </summary>
        public ChunkData GenerateChunkData(Vector2Int chunkCoord, int chunkSize)
        {
            ChunkData data = new ChunkData(chunkSize);
            float[] weights = new float[biomes.Count];

            for (int x = -1; x <= chunkSize; x++)
            {
                for (int z = -1; z <= chunkSize; z++)
                {
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldZ = chunkCoord.y * chunkSize + z;
                    data.SetColumn(x, z, GenerateColumn(worldX, worldZ, weights));
                }
            }

            return data;
        }

        private BiomeColumn GenerateColumn(float worldX, float worldZ, float[] weights)
        {
            float temperature = Spread(noise.Temperature.Sample01(worldX, worldZ));
            float moisture = Spread(noise.Moisture.Sample01(worldX, worldZ));

            // Continental elevation. Only a small share of it lifts the ground
            // floor — most of the budget swells the landforms themselves (the
            // hill boost below), so highlands read as clusters of tall hills
            // rising out of flat plains instead of uniformly raised plateaus.
            // It also biases relief so mountains cluster on the high ground.
            float elevationNorm = Mathf.Clamp01((noise.Elevation.Sample01(worldX, worldZ) - 0.5f) * 1.8f + 0.5f);
            float elevationSwell = Mathf.SmoothStep(0f, 1f, elevationNorm);
            float elevation = elevationSwell * settings.elevationRange * 0.3f;
            float relief = Spread(noise.Relief.Sample01(worldX, worldZ));
            relief = Mathf.Clamp01(relief * 0.7f + elevationNorm * 0.35f);

            float totalWeight = 0f;
            int dominant = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = biomes[i].ClimateWeight(temperature, moisture, relief);
                totalWeight += weights[i];
                if (weights[i] > weights[dominant])
                {
                    dominant = i;
                }
            }

            if (totalWeight <= 0.0001f)
            {
                // The climate sample fell in a gap between all biome windows.
                weights[0] = 1f;
                totalWeight = 1f;
                dominant = 0;
            }

            // Cube World-style relief: flat valley floors with discrete rounded
            // mounds on top, instead of a tilted noise plane. The hill curve
            // clamps low noise to a dead-flat floor and high noise to a plateau,
            // so hills read as objects sitting on the ground.
            noise.Height.SampleSmoothAndRidged(worldX, worldZ, out float mountainSmooth, out float mountainRidged);
            // Stretch the massif noise so mountains regularly reach the top of
            // the hill curve (full peaks) and drop to true valley floors.
            float mountainShape = HillCurve(Mathf.Clamp01((Mathf.Lerp(mountainSmooth, mountainRidged, 0.35f) - 0.5f) * 1.6f + 0.5f));
            float hillShape = HillCurve(noise.Hills.Sample01(worldX, worldZ));
            float hilliness = Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.75f, noise.HillMask.Sample01(worldX, worldZ))));

            // The elevation budget not spent on floor lift amplifies the hills:
            // lowland bumps stay a few blocks tall while highland hills tower
            // as landmarks. Ridged (mountain) biomes are mostly exempt — their
            // amplitude is tuned directly.
            float hillBoost = Mathf.Lerp(0.35f, 1f + settings.elevationRange * 0.045f, elevationSwell);

            // Tall boosted hills borrow from the massif channel: where both
            // fields peak together, a green hill grows into a small mountain
            // on the plains — variety between rounded domes and real peaks.
            float tall = Mathf.InverseLerp(1f, 2.2f, hillBoost);
            float greenShape = Mathf.Lerp(hillShape, Mathf.Max(hillShape, mountainShape), tall * 0.6f);

            float height = elevation + (noise.BaseSwell.Sample01(worldX, worldZ) - 0.5f) * 8f;
            float blendedShape = 0f;
            float blendedRidged = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                BiomeDefinition biome = biomes[i];
                // Regional hilliness tempers local knolls but not mountain
                // massifs, which are already gated by the relief channel.
                float shape = Mathf.Lerp(greenShape, mountainShape, biome.ridged) * Mathf.Lerp(hilliness, 1f, biome.ridged);
                float amplitude = biome.amplitude * Mathf.Lerp(hillBoost, 1f, biome.ridged);
                height += weights[i] / totalWeight * (biome.baseHeight + amplitude * shape);
                blendedShape += weights[i] / totalWeight * shape;
                blendedRidged += weights[i] / totalWeight * biome.ridged;
            }

            // Tall green hills earn a hint of the mountain treatment — light
            // crag carving and faint ledges — so their summits read as peaks.
            blendedRidged = Mathf.Max(blendedRidged, tall * 0.45f);

            // Mountain-style steps: rugged relief snaps into broad flat ledges,
            // while rolling terrain keeps its smooth one-block voxel staircase.
            float stepiness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.75f, blendedRidged));
            if (stepiness > 0f)
            {
                height = Mathf.Lerp(height, ApplyTerracing(height), stepiness);
            }

            // Set-piece landmarks from one rare-spot noise field: boulder
            // mountains where it peaks, dished valley bowls where it bottoms
            // out. Both are shapes stamped onto whatever terrain is beneath.
            float butteWall = 0f;
            if (settings.generateLandmarks)
            {
                float landmarkNoise = noise.Landmark.Sample01(worldX, worldZ);

                // The wall climbs its full height over a very narrow noise band,
                // giving near-vertical sides wrapped in tight ledge banding; the
                // plateau above carries a stepped dome cap. Kept off rugged
                // mountain relief so boulders rise from calm ground.
                butteWall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 0.72f, landmarkNoise));
                butteWall *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.65f, relief));
                if (butteWall > 0f)
                {
                    float dome = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.9f, landmarkNoise)) * 12f;
                    height += TerraceBands(butteWall * settings.boulderMountainHeight + dome, 3f);
                }

                float valley = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.22f, landmarkNoise));
                if (valley > 0f)
                {
                    // Future lake beds: the dish always stays above the world floor.
                    height = Mathf.Max(3f, height - valley * settings.valleyDepth);
                }
            }

            // Palette lookups ignore the continental offset so a biome's gradient
            // spans its own local terrain, not the whole world's height range.
            float heightForColor = height - elevation;

            Color surface = Color.clear;
            Color cliff = Color.clear;
            float treeDensity = 0f;
            float grassDensity = 0f;
            float boulderDensity = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                BiomeDefinition biome = biomes[i];
                float weight = weights[i] / totalWeight;
                float gradientTime = Mathf.Clamp01(Mathf.InverseLerp(biome.baseHeight, biome.baseHeight + biome.amplitude, heightForColor));
                surface += weight * biome.surfaceGradient.Evaluate(gradientTime);
                cliff += weight * biome.cliffColor;
                treeDensity += weight * biome.treeDensity;
                grassDensity += weight * biome.grassDensity;
                boulderDensity += weight * biome.boulderDensity;
            }

            surface.a = 1f;
            cliff.a = 1f;

            if (settings.useFlatGroundColor)
            {
                surface = settings.flatGroundColor;
            }

            // Trails follow the midline contour of a broad noise field, which
            // yields long connected winding paths across the world. The distance
            // to the contour is estimated as |n - 0.5| / |gradient| so trails
            // keep a constant width instead of blobbing out where the noise
            // plateaus near its midline.
            float pathMask = 0f;
            if (settings.generatePaths)
            {
                const float gradientStep = 2f;
                float pathNoise = noise.Paths.Sample01(worldX, worldZ);
                float gradX = noise.Paths.Sample01(worldX + gradientStep, worldZ) - pathNoise;
                float gradZ = noise.Paths.Sample01(worldX, worldZ + gradientStep) - pathNoise;
                float gradientPerBlock = Mathf.Sqrt(gradX * gradX + gradZ * gradZ) / gradientStep;
                float distanceBlocks = Mathf.Abs(pathNoise - 0.5f) / Mathf.Max(gradientPerBlock, 0.0004f);
                pathMask = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(settings.pathWidth * 0.6f, settings.pathWidth, distanceBlocks));
                // Fade trails out on rugged mountain relief and off boulder walls.
                pathMask *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.65f, relief));
                pathMask *= 1f - butteWall;
                surface = Color.Lerp(surface, settings.pathColor, pathMask);

                // Sink the trail bed below the surrounding terrain.
                if (pathMask > 0.45f)
                {
                    height -= settings.pathDepth;
                }
            }

            SolidMask solidMask = BuildSolidMask(worldX, worldZ, height, blendedShape, blendedRidged, pathMask, out int topSolidY);

            return new BiomeColumn
            {
                height = topSolidY,
                localHeight = topSolidY - elevation,
                solidMask = solidMask,
                surfaceColor = surface,
                cliffColor = cliff,
                treeDensity = treeDensity,
                grassDensity = grassDensity,
                boulderDensity = boulderDensity,
                pathMask = pathMask,
                dominantBiome = (byte)dominant,
                dominantWeight = weights[dominant] / totalWeight,
            };
        }

        /// <summary>
        /// Builds the column's solidity bitmask. A voxel is solid when
        /// y &lt;= surfaceHeight + signed 3D noise * strength — because the 3D
        /// term varies with y, slopes get undercuts and overhang lips. Strength
        /// scales with hill shape so valley floors and trails stay flat.
        /// </summary>
        private SolidMask BuildSolidMask(float worldX, float worldZ, float height, float blendedShape, float blendedRidged, float pathMask, out int topSolidY)
        {
            int topY = Mathf.Clamp(Mathf.FloorToInt(height), 2, 124);

            // 4s(1-s) peaks on hill flanks (shape ~0.5) and fades to zero on
            // both valley floors and plateau tops, so undercuts appear where
            // lips can form and walkable surfaces stay clean. Rugged (ridged)
            // biomes carve at full strength; smooth green hills barely at all,
            // which keeps rolling terrain clean.
            float strength = settings.overhangStrength
                * Mathf.Clamp01(4f * blendedShape * (1f - blendedShape))
                * Mathf.Lerp(0.05f, 1f, blendedRidged)
                * (1f - pathMask);

            // Below this the carve cannot form real overhangs — it only pebbles
            // slopes with one-block lumps — so gentle terrain opts out entirely.
            if (strength < 1.5f)
            {
                topSolidY = topY;
                return SolidMask.FullTo(topY);
            }

            int bandMin = Mathf.Max(3, topY - Mathf.CeilToInt(strength) - 2);
            int bandMax = Mathf.Min(126, topY + Mathf.CeilToInt(strength));

            // Solid everywhere below the noise band.
            SolidMask mask = SolidMask.FullTo(bandMin - 1);
            for (int y = bandMin; y <= bandMax; y++)
            {
                float effectiveSurface = height + (noise.Sample3D01(worldX, y, worldZ) - 0.5f) * 2f * strength;
                if (y <= effectiveSurface)
                {
                    mask.SetSolid(y);
                }
            }

            topSolidY = bandMax;
            while (topSolidY > 0 && !mask.IsSolid(topSolidY))
            {
                topSolidY--;
            }

            return mask;
        }

        /// <summary>
        /// Maps raw noise to hill presence: below the window it is flat valley
        /// floor, inside it is mound side, above it is plateau top.
        /// </summary>
        private static float HillCurve(float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.8f, value));
        }

        /// <summary>
        /// Quantizes a landmark's height contribution into tight ledges, giving
        /// sheer walls the stacked contour-band look.
        /// </summary>
        private static float TerraceBands(float value, float step)
        {
            float steps = value / step;
            float tread = Mathf.Floor(steps);
            float riser = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.7f, steps - tread));
            return (tread + riser) * step;
        }

        /// <summary>
        /// Snaps height into flat treads separated by sharp risers, turning smooth
        /// rolling noise into rigid stepped hills.
        /// </summary>
        private float ApplyTerracing(float height)
        {
            if (settings.terraceHeight < 0.01f)
            {
                return height;
            }

            const float riserWidth = 0.2f;
            float steps = height / settings.terraceHeight;
            float tread = Mathf.Floor(steps);
            float riser = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f - riserWidth, 0.5f + riserWidth, steps - tread));
            return (tread + riser) * settings.terraceHeight;
        }

        private static float Spread(float value)
        {
            return Mathf.Clamp01((value - 0.5f) * ClimateContrast + 0.5f);
        }
    }
}
