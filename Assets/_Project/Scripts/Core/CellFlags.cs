using System;

namespace Crucible.Core
{
    /// <summary>
    /// Per-cell state bits, stored in bits 24..30 of a cell. Seven bits available.
    /// </summary>
    [Flags]
    public enum CellFlags : byte
    {
        None = 0,

        /// <summary>
        /// Set when a cell has already moved this tick, cleared in a sweep at the end of the tick.
        /// Falling alone does not strictly need it — a bottom-up scan cannot visit a falling cell
        /// twice — but horizontal spreading in M2 does, and the flag has to exist before the rules
        /// that depend on it.
        /// </summary>
        Moved = 1 << 0,

        // 1 << 1 .. 1 << 6 reserved for Static, Burning, Wet.
    }
}
