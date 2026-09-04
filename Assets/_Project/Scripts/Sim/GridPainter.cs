using Crucible.Core;
using Unity.Collections;
using UnityEngine;

namespace Crucible.Sim
{
    /// <summary>
    /// Turns the grid into a pixel buffer. Pure and blittable — no managed types, no engine calls —
    /// so it converts into a Burst job at M4 without being rewritten. Uploading the buffer to a
    /// texture is a managed operation and lives in the gameplay layer instead.
    /// </summary>
    public static class GridPainter
    {
        public static void Paint(in SandGrid grid, in GridPalette palette, NativeArray<Color32> destination)
        {
            var cells = grid.Cells;
            int count = cells.Length;

            for (int i = 0; i < count; i++)
            {
                uint cell = cells[i];
                byte element = Cell.GetElement(cell);

                Color32 colour = palette.BaseColour[element];
                byte jitter = palette.Jitter[element];

                if (jitter != 0)
                {
                    // Variant is fixed when the cell is placed, so a settled grain keeps its shade
                    // instead of flickering. Six bits of it map to a signed offset around zero,
                    // which reads as grain rather than as noise.
                    int offset = ((Cell.GetVariant(cell) & 63) - 32) * jitter / 32;

                    colour.r = ClampToByte(colour.r + offset);
                    colour.g = ClampToByte(colour.g + offset);
                    colour.b = ClampToByte(colour.b + offset);
                }

                destination[i] = colour;
            }
        }

        private static byte ClampToByte(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 255)
            {
                return 255;
            }

            return (byte)value;
        }
    }
}
