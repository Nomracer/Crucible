using System.Runtime.CompilerServices;

namespace Crucible.Core
{
    /// <summary>
    /// Stateless hashing, used wherever the simulation needs a coin flip.
    ///
    /// The simulation must never touch <c>UnityEngine.Random</c> or any other shared generator.
    /// Once chunks are simulated in parallel, a shared generator would make the result depend on
    /// which thread got there first, and determinism is what rewind, frame stepping and the
    /// naive-versus-optimised equivalence test are all built on.
    ///
    /// Every value here is a pure function of (tick, cell index), so the same grid and the same
    /// tick always produce the same decisions regardless of scheduling.
    /// </summary>
    public static class Hash
    {
        /// <summary>Deterministic 32-bit hash of a tick and a cell index.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Mix(uint tick, int index)
        {
            // Two odd multipliers and shift-xor folding: cheap, and good enough that neighbouring
            // indices on the same tick do not correlate. Verified by the drift test — a biased
            // hash shows up immediately as powders walking sideways.
            uint h = (uint)index * 0x9E3779B1u;
            h ^= tick * 0x85EBCA6Bu;
            h ^= h >> 15;
            h *= 0x2545F491u;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
            return h;
        }

        /// <summary>Even odds, derived from the given tick and index.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CoinFlip(uint tick, int index) => (Mix(tick, index) & 1u) != 0u;

        /// <summary>A byte of noise, used for per-cell colour jitter.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Byte(uint tick, int index) => (byte)(Mix(tick, index) & 0xFFu);
    }
}
