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
        Ground = 0x0002

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
            Vertices = new NativeArray<int3>(new int3[]
            {
                // Front Face
                new int3(0, 0, 0),
                new int3(1, 0, 0),
                new int3(1, 1, 0),
                new int3(0, 1, 0),

                // Back Face
                new int3(0, 0, 1),
                new int3(1, 0, 1),
                new int3(1, 1, 1),
                new int3(0, 1, 1)
            }, Allocator.Persistent);

            Triangles = new NativeArray<int>(new int[]
            {
                // Front Face
                0, 1, 2,
                0, 2, 3,

                // Back Face
                4, 6, 5,
                4, 7, 6,

                // Left Face
                4, 5, 1,
                4, 1, 0,

                // Right Face
                3, 2, 6,
                3, 6, 7,

                // Top Face
                1, 5, 6,
                1, 6, 2,

                // Bottom Face
                4, 0, 3,
                4, 3, 7
            }, Allocator.Persistent);

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
        public static int GetBlockIndex(int3 position, int chunkSize, int maxChunkHeight) => position.x + position.z * chunkSize + position.y * chunkSize * chunkSize;

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
