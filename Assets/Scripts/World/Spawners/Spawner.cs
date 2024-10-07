using UnityEngine;

/// <summary>
/// Abstract base class for all spawner types.
/// </summary>
public abstract class Spawner : MonoBehaviour
{
    protected ObjectPlacementManager placementManager;

    public void SetPlacementManager(ObjectPlacementManager manager)
    {
        placementManager = manager;
    }

    /// <summary>
    /// Spawns objects on a terrain chunk.
    /// </summary>
    public abstract void Spawn(GameObject chunkObject, int[,] heightMap, float voxelScale, int chunkSize, Vector2Int chunkCoord);

    /// <summary>
    /// Gets the constrained rotation with z-axis set to 90 degrees.
    /// </summary>
    protected Quaternion GetConstrainedRotation()
    {
        // Randomize the y-axis rotation for variety
        float yRotation = Random.Range(0f, 360f);
        // Set z-axis rotation to 90 degrees
        return Quaternion.Euler(0f, yRotation, 90f);
    }
}
