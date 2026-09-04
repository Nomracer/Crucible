using Crucible.Core;

namespace Crucible.Sim
{
    /// <summary>
    /// Writes elements into the grid. Kept next to the grid rather than in the input layer so the
    /// level loader, the tests and the pointer can all stamp the same way.
    /// </summary>
    public static class GridBrush
    {
        /// <summary>
        /// Stamps a filled circle. Empty cells are overwritten only when the brush element is not
        /// empty; an empty brush erases anything it touches, which is how the eraser works.
        /// </summary>
        public static void Stamp(ref SandGrid grid, int centreX, int centreY, int radius, byte element, uint tick)
        {
            int radiusSquared = radius * radius;

            int minY = centreY - radius;
            int maxY = centreY + radius;
            int minX = centreX - radius;
            int maxX = centreX + radius;

            for (int y = minY; y <= maxY; y++)
            {
                if ((uint)y >= (uint)grid.Height)
                {
                    continue;
                }

                int dy = y - centreY;

                for (int x = minX; x <= maxX; x++)
                {
                    if ((uint)x >= (uint)grid.Width)
                    {
                        continue;
                    }

                    int dx = x - centreX;
                    if (dx * dx + dy * dy > radiusSquared)
                    {
                        continue;
                    }

                    int index = grid.Index(x, y);

                    if (element == Elements.Empty)
                    {
                        grid.Cells[index] = Cell.Empty;
                        continue;
                    }

                    // Do not bury existing material — pouring onto a pile should stack, not replace.
                    if (Cell.GetElement(grid.Cells[index]) != Elements.Empty)
                    {
                        continue;
                    }

                    // Variant is the per-cell colour jitter, fixed at placement so a settled pile
                    // does not shimmer as it moves.
                    byte variant = Hash.Byte(tick, index);
                    grid.Cells[index] = Cell.Make(element, variant);
                }
            }
        }
    }
}
