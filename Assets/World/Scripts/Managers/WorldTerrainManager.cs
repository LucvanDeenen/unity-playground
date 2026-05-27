using World.Chunks;

namespace World.Managers
{
    /// <summary>
    /// Manages the open-world terrain by streaming chunks around the player.
    /// <para>
    /// All settings (chunk size, voxel scale, render distance, noise) are configured
    /// on the base <see cref="TerrainManager"/> component in the Inspector — nothing
    /// needs to be hardcoded here.
    /// </para>
    /// </summary>
    public class WorldTerrainManager : TerrainManager
    {
        protected override void GenerateChunk(TerrainChunk chunk)
        {
            // Generate the float height map (used by the mesh generator)
            float[,] heightMap = noiseGenerator.GenerateHeightMap(
                chunk.chunkSize + 1,
                chunk.chunkSize + 1,
                chunk.chunkCoord,
                chunk.chunkSize);

            // Build and apply the mesh
            chunk.UpdateChunkMesh(meshGenerator.GenerateMeshData(heightMap));

            // Convert to int heights for spawners (voxel-integer space)
            int[,] heightMapInt = ToIntHeightMap(heightMap);

            foreach (var spawner in spawners)
            {
                if (spawner != null)
                    spawner.Spawn(chunk.chunkObject, heightMapInt, voxelScale, chunk.chunkSize, chunk.chunkCoord);
            }
        }
    }
}
