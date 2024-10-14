namespace World.Managers
{
    /// <summary>
    /// Manages world terrain chunks around the player.
    /// </summary>
    public class WorldTerrainManager : TerrainManager
    {
        protected override void Start()
        {
            renderDistance = 20;
            base.Start();
            noiseGenerator.SetHeightMultiplier(25f);
        }

    }
}
