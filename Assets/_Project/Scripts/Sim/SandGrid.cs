using System;
using System.Runtime.CompilerServices;
using Crucible.Core;
using Unity.Collections;

namespace Crucible.Sim
{
    /// <summary>
    /// The play area: one flat array of packed cells, allocated once and never resized.
    ///
    /// Row-major with the origin at the bottom-left, so <c>index = y * Width + x</c> and row 0 is
    /// the floor. That matches how <see cref="UnityEngine.Texture2D"/> stores rows, which means the
    /// painter can copy straight across without flipping anything.
    /// </summary>
    public struct SandGrid : IDisposable
    {
        public NativeArray<uint> Cells;

        public readonly int Width;
        public readonly int Height;

        public SandGrid(int width, int height, Allocator allocator)
        {
            Width = width;
            Height = height;
            // NativeArrayOptions.ClearMemory gives an empty grid, which is element id 0.
            Cells = new NativeArray<uint>(width * height, allocator, NativeArrayOptions.ClearMemory);
        }

        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Cells.Length;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Cells.IsCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int Index(int x, int y) => y * Width + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint Get(int x, int y) => Cells[y * Width + x];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, uint cell) => Cells[y * Width + x] = cell;

        /// <summary>Reads a cell, returning <see cref="Cell.Empty"/> outside the grid.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint GetOrEmpty(int x, int y)
            => InBounds(x, y) ? Cells[y * Width + x] : Cell.Empty;

        /// <summary>
        /// True when a cell can be displaced by a falling powder. Only empty space qualifies for
        /// now; once liquids exist this becomes a density comparison.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsPassable(int x, int y)
            => InBounds(x, y) && Cell.GetElement(Cells[y * Width + x]) == Elements.Empty;

        public void Clear()
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i] = Cell.Empty;
            }
        }

        public void Dispose()
        {
            if (Cells.IsCreated)
            {
                Cells.Dispose();
            }
        }
    }
}
