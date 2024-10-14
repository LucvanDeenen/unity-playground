using World.MeshGeneration;
using World.Chunks;

namespace World.Managers
{
    /// <summary>
    /// Manages world terrain chunks around the player.
    /// </summary>
    public class WorldTerrainManager : TerrainManager
    {
        protected override void GenerateTerrain(TerrainChunk chunk)
        {
            // Generate height map
            float[,] heightMapFloat = noiseGenerator.GenerateHeightMap(chunk.chunkSize + 1, chunk.chunkSize + 1, chunk.chunkCoord, chunk.chunkSize);

            // Generate mesh data
            MeshData meshData = meshGenerator.GenerateMeshData(heightMapFloat);

            // Update chunk mesh
            chunk.UpdateChunkMesh(meshData);
        }
    }
}
