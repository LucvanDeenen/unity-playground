using System;

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace World.Generation
{
    public enum Block : ushort
    {

        Null = 0x0000,
        Air = 0x0001,
        Stone = 0x0002

    }

    public enum Direction
    {

        Forward, //+z
        Right,   //+x
        Back,    //-z
        Left,    //-x
        Up,      //+y
        Down     //-y

    }

    public struct BlockData
    {
        [ReadOnly] public static NativeArray<int3> Vertices;

        [ReadOnly] public static NativeArray<int> Triangles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Vertices = new NativeArray<int3>(8, Allocator.Persistent)
            {
                [0] = new int3(1, 1, 1),
                [1] = new int3(0, 1, 1),
                [2] = new int3(0, 0, 1),
                [3] = new int3(1, 0, 1),
                [4] = new int3(0, 1, 0),
                [5] = new int3(1, 1, 0),
                [6] = new int3(1, 0, 0),
                [7] = new int3(0, 0, 0)
            };

            Triangles = new NativeArray<int>(24, Allocator.Persistent)
            {
                [0] = 0,
                [1] = 1,
                [2] = 2,
                [3] = 3,
                [4] = 5,
                [5] = 0,
                [6] = 3,
                [7] = 6,
                [8] = 4,
                [9] = 5,
                [10] = 6,
                [11] = 7,
                [12] = 1,
                [13] = 4,
                [14] = 7,
                [15] = 2,
                [16] = 5,
                [17] = 4,
                [18] = 1,
                [19] = 0,
                [20] = 3,
                [21] = 2,
                [22] = 7,
                [23] = 6
            };

            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDomainUnload()
        {
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        static void OnDomainUnload(object sender, EventArgs e)
        {
            if (Vertices.IsCreated)
                Vertices.Dispose();
            if (Triangles.IsCreated)
                Triangles.Dispose();
        }
    }

    public static class BlockExtensions
    {

        public static NativeArray<int3> GetFaceVertices(Direction direction, int scale, int3 pos)
        {
            var faceVertices = new NativeArray<int3>(4, Allocator.Temp);

            for (int i = 0; i < 4; i++)
            {
                var index = BlockData.Triangles[(int)direction * 4 + i];
                faceVertices[i] = BlockData.Vertices[index] * scale + pos;
            }

            return faceVertices;
        }

        public static int GetBlockIndex(int3 position, int chunkSize) => position.x + position.z * chunkSize + position.y * chunkSize * chunkSize;

        public static bool IsEmpty(this Block block) => block == Block.Air;

        public static int3 GetPositionInDirection(Direction direction, int x, int y, int z)
        {
            switch (direction)
            {
                case Direction.Forward:
                    return new int3(x, y, z + 1);
                case Direction.Right:
                    return new int3(x + 1, y, z);
                case Direction.Back:
                    return new int3(x, y, z - 1);
                case Direction.Left:
                    return new int3(x - 1, y, z);
                case Direction.Up:
                    return new int3(x, y + 1, z);
                case Direction.Down:
                    return new int3(x, y - 1, z);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
