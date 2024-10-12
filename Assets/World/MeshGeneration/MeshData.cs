using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace World.MeshGeneration
{

    /// <summary>
    /// Represents mesh data for a terrain chunk.
    /// </summary>
    public class MeshData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> triangles = new List<int>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<Color> colors = new List<Color>();
    }
}