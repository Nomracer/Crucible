using Crucible.Core;

namespace Crucible.Sim
{
    /// <summary>
    /// The reference implementation: single-threaded, no chunking, no Burst, whole grid every tick.
    ///
    /// This class is permanent. From M3 onward the optimised simulation has to produce a
    /// bit-identical grid to this one over N ticks from the same seed — chunking, jobs and Burst
    /// are optimisations, not behaviour changes, and this is what proves it. It is test
    /// infrastructure, not dead code, and it does not get deleted when something faster exists.
    ///
    /// It is also the M1 baseline. Every number in the measurement table is a comparison against
    /// this, so it must stay deliberately unoptimised.
    /// </summary>
    public static class NaiveSimulation
    {
        public static void Step(ref SandGrid grid, uint tick)
        {
            SimulateCells(ref grid, tick);
            ClearMovedFlags(ref grid);
        }

        private static void SimulateCells(ref SandGrid grid, uint tick)
        {
            int width = grid.Width;
            int height = grid.Height;

            // Bottom-up. A grain that falls from y to y-1 lands on a row already visited this tick,
            // so it cannot be processed twice.
            for (int y = 0; y < height; y++)
            {
                // Flip the horizontal scan direction every tick. Scanning one way forever makes
                // powders creep sideways, because the cell that gets asked first always wins the
                // contested diagonal. The drift test fails without this.
                bool leftToRight = (tick & 1u) == 0u;

                for (int i = 0; i < width; i++)
                {
                    int x = leftToRight ? i : width - 1 - i;
                    StepCell(ref grid, x, y, tick);
                }
            }
        }

        private static void StepCell(ref SandGrid grid, int x, int y, uint tick)
        {
            int index = grid.Index(x, y);
            uint cell = grid.Cells[index];

            if (Cell.GetElement(cell) != Elements.Sand)
            {
                return;
            }

            if (Cell.HasFlag(cell, CellFlags.Moved))
            {
                return;
            }

            // Straight down first.
            if (grid.IsPassable(x, y - 1))
            {
                Move(ref grid, index, grid.Index(x, y - 1), cell);
                return;
            }

            // Then the two diagonals, in an order chosen by the hash so that a symmetrical pile
            // does not lean. Trying left before right unconditionally builds a visibly skewed heap.
            bool preferLeft = Hash.CoinFlip(tick, index);
            int firstX = preferLeft ? x - 1 : x + 1;
            int secondX = preferLeft ? x + 1 : x - 1;

            if (grid.IsPassable(firstX, y - 1))
            {
                Move(ref grid, index, grid.Index(firstX, y - 1), cell);
                return;
            }

            if (grid.IsPassable(secondX, y - 1))
            {
                Move(ref grid, index, grid.Index(secondX, y - 1), cell);
            }
        }

        private static void Move(ref SandGrid grid, int fromIndex, int toIndex, uint cell)
        {
            grid.Cells[toIndex] = Cell.SetFlag(cell, CellFlags.Moved);
            grid.Cells[fromIndex] = Cell.Empty;
        }

        /// <summary>
        /// Clears the Moved flag across the whole grid.
        ///
        /// A full extra pass over every cell is exactly the kind of cost chunking removes later,
        /// which is why it is written the obvious way here rather than folded into the main loop.
        /// </summary>
        private static void ClearMovedFlags(ref SandGrid grid)
        {
            var cells = grid.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = Cell.ClearAllFlags(cells[i]);
            }
        }
    }
}
