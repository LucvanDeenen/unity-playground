using System.Collections.Generic;
using UnityEngine;

namespace World.Shared
{
    /// <summary>
    /// Manages object placements to prevent overlaps by keeping track of occupied positions.
    /// </summary>
    public class ObjectPlacementManager : MonoBehaviour
    {
        [Tooltip("The size of each cell in the spatial grid.")]
        public float cellSize = 5f;

        // Dictionary to keep track of occupied cells and their object positions.
        private Dictionary<Vector2Int, List<Vector3>> occupiedCells = new Dictionary<Vector2Int, List<Vector3>>();

        /// <summary>
        /// Checks if a position is available for object placement.
        /// </summary>
        /// <param name="position">The world position to check.</param>
        /// <param name="minDistance">The minimum required distance from other objects.</param>
        /// <returns>True if the position is available; otherwise, false.</returns>
        public bool IsPositionAvailable(Vector3 position, float minDistance)
        {
            Vector2Int cellCoord = GetCellCoordFromPosition(position);
            List<Vector3> objectsInCell;

            // Check the current cell and neighboring cells.
            for (int x = cellCoord.x - 1; x <= cellCoord.x + 1; x++)
            {
                for (int y = cellCoord.y - 1; y <= cellCoord.y + 1; y++)
                {
                    Vector2Int neighborCell = new Vector2Int(x, y);
                    if (occupiedCells.TryGetValue(neighborCell, out objectsInCell))
                    {
                        foreach (var objPos in objectsInCell)
                        {
                            if (Vector3.Distance(position, objPos) < minDistance)
                            {
                                return false; // Position is too close to an existing object.
                            }
                        }
                    }
                }
            }

            return true; // Position is available.
        }

        /// <summary>
        /// Registers an object position in the occupied cells.
        /// </summary>
        /// <param name="position">The world position of the placed object.</param>
        public void RegisterObjectPosition(Vector3 position)
        {
            Vector2Int cellCoord = GetCellCoordFromPosition(position);
            List<Vector3> objectsInCell;

            if (!occupiedCells.TryGetValue(cellCoord, out objectsInCell))
            {
                objectsInCell = new List<Vector3>();
                occupiedCells[cellCoord] = objectsInCell;
            }

            objectsInCell.Add(position);
        }

        /// <summary>
        /// Converts a world position to cell coordinates.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <returns>The cell coordinates as a Vector2Int.</returns>
        private Vector2Int GetCellCoordFromPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / cellSize);
            int y = Mathf.FloorToInt(position.z / cellSize);
            return new Vector2Int(x, y);
        }
    }
}
