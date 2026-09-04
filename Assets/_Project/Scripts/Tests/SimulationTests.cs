using Crucible.Core;
using Crucible.Sim;
using NUnit.Framework;
using Unity.Collections;

namespace Crucible.Tests
{
    /// <summary>
    /// These tests exist to keep the optimisation work honest.
    ///
    /// From M3 onward the optimised simulation has to agree with <see cref="NaiveSimulation"/> cell
    /// for cell. That comparison is only meaningful if the naive implementation is itself
    /// well-defined, which is what the determinism test pins down, and unbiased, which is what the
    /// drift test pins down.
    /// </summary>
    public sealed class SimulationTests
    {
        private const int Width = 64;
        private const int Height = 96;
        private const int FloorHeight = 4;

        [Test]
        public void SameStartingStateProducesIdenticalResults()
        {
            var first = CreateGrid();
            var second = CreateGrid();

            try
            {
                SeedScatteredSand(ref first);
                SeedScatteredSand(ref second);

                for (uint tick = 0; tick < 500; tick++)
                {
                    NaiveSimulation.Step(ref first, tick);
                    NaiveSimulation.Step(ref second, tick);
                }

                for (int i = 0; i < first.Length; i++)
                {
                    Assert.AreEqual(first.Cells[i], second.Cells[i],
                        $"Grids diverged at index {i}. The simulation is not deterministic, so rewind, "
                        + "frame stepping and the equivalence test are all unsound.");
                }
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }
        }

        [Test]
        public void PowderDoesNotDriftSideways()
        {
            var grid = CreateGrid();

            try
            {
                // A single narrow column dropped onto a flat floor. It should spread into a
                // symmetrical pile centred where it started.
                int centreX = Width / 2;
                for (int y = 20; y < 90; y++)
                {
                    grid.Set(centreX, y, Cell.Make(Elements.Sand, (byte)y));
                }

                for (uint tick = 0; tick < 2000; tick++)
                {
                    NaiveSimulation.Step(ref grid, tick);
                }

                double centroid = SandCentroidX(in grid);

                // One cell of tolerance. With seventy grains the pile is not perfectly symmetrical
                // on any single run, so a tighter bound would be flaky rather than strict. A real
                // directional bias — scanning the same way every tick, or always trying the same
                // diagonal first — moves this by tens of cells, well clear of the noise.
                Assert.That(centroid, Is.EqualTo(centreX).Within(1.0),
                    "The pile drifted sideways, which means the scan or the diagonal choice is biased.");
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SandIsNeitherCreatedNorDestroyed()
        {
            var grid = CreateGrid();

            try
            {
                SeedScatteredSand(ref grid);
                int before = CountElement(in grid, Elements.Sand);

                for (uint tick = 0; tick < 600; tick++)
                {
                    NaiveSimulation.Step(ref grid, tick);
                }

                Assert.AreEqual(before, CountElement(in grid, Elements.Sand),
                    "Sand count changed. A move is overwriting a cell instead of swapping into empty space.");
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void StoneNeverMoves()
        {
            var grid = CreateGrid();

            try
            {
                // Stone suspended in mid-air with nothing under it.
                grid.Set(10, 50, Cell.Make(Elements.Stone));
                grid.Set(11, 50, Cell.Make(Elements.Stone));

                for (uint tick = 0; tick < 300; tick++)
                {
                    NaiveSimulation.Step(ref grid, tick);
                }

                Assert.AreEqual(Elements.Stone, Cell.GetElement(grid.Get(10, 50)));
                Assert.AreEqual(Elements.Stone, Cell.GetElement(grid.Get(11, 50)));
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void SandSettlesOnTheFloor()
        {
            var grid = CreateGrid();

            try
            {
                grid.Set(20, 80, Cell.Make(Elements.Sand));

                for (uint tick = 0; tick < 300; tick++)
                {
                    NaiveSimulation.Step(ref grid, tick);
                }

                Assert.AreEqual(Elements.Sand, Cell.GetElement(grid.Get(20, FloorHeight)),
                    "The grain did not come to rest directly on top of the floor.");
            }
            finally
            {
                grid.Dispose();
            }
        }

        [Test]
        public void MovedFlagsAreClearedEachTick()
        {
            var grid = CreateGrid();

            try
            {
                SeedScatteredSand(ref grid);

                for (uint tick = 0; tick < 50; tick++)
                {
                    NaiveSimulation.Step(ref grid, tick);
                }

                for (int i = 0; i < grid.Length; i++)
                {
                    Assert.IsFalse(Cell.HasFlag(grid.Cells[i], CellFlags.Moved),
                        $"Cell {i} kept its Moved flag past the end of the tick.");
                }
            }
            finally
            {
                grid.Dispose();
            }
        }

        private static SandGrid CreateGrid()
        {
            var grid = new SandGrid(Width, Height, Allocator.Persistent);

            for (int y = 0; y < FloorHeight; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    grid.Set(x, y, Cell.Make(Elements.Stone));
                }
            }

            return grid;
        }

        /// <summary>
        /// Scatters sand using the same hash the simulation uses, so the starting state is fixed
        /// without needing a stored fixture.
        /// </summary>
        private static void SeedScatteredSand(ref SandGrid grid)
        {
            for (int y = FloorHeight; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int index = grid.Index(x, y);
                    if ((Hash.Mix(7u, index) & 3u) == 0u)
                    {
                        grid.Cells[index] = Cell.Make(Elements.Sand, Hash.Byte(11u, index));
                    }
                }
            }
        }

        private static int CountElement(in SandGrid grid, byte element)
        {
            int count = 0;
            for (int i = 0; i < grid.Length; i++)
            {
                if (Cell.GetElement(grid.Cells[i]) == element)
                {
                    count++;
                }
            }

            return count;
        }

        private static double SandCentroidX(in SandGrid grid)
        {
            double total = 0.0;
            int count = 0;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (Cell.GetElement(grid.Get(x, y)) == Elements.Sand)
                    {
                        total += x;
                        count++;
                    }
                }
            }

            return count == 0 ? double.NaN : total / count;
        }
    }
}
