using System.Runtime.CompilerServices;

namespace Crucible.Core
{
    /// <summary>
    /// A cell is one <see cref="uint"/>. Everything here is a pure bit operation on that value —
    /// no storage, no state.
    ///
    /// <code>
    /// bit  0..7   element id
    /// bit  8..15  variant     colour jitter + per-material state
    /// bit 16..23  lifetime    fire/steam decay, acid depletion
    /// bit 24..30  flags       see CellFlags
    /// bit    31   reserved
    /// </code>
    ///
    /// Four bytes per cell keeps a 288x512 grid at 590 KB, which fits comfortably in L2 on the
    /// devices we care about. That is the whole reason for the packing.
    /// </summary>
    public static class Cell
    {
        public const uint Empty = 0u;

        private const int ElementShift = 0;
        private const int VariantShift = 8;
        private const int LifetimeShift = 16;
        private const int FlagsShift = 24;

        private const uint ElementMask = 0xFFu << ElementShift;
        private const uint VariantMask = 0xFFu << VariantShift;
        private const uint LifetimeMask = 0xFFu << LifetimeShift;
        private const uint FlagsMask = 0x7Fu << FlagsShift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Make(byte element, byte variant = 0, byte lifetime = 0)
        {
            return ((uint)element << ElementShift)
                 | ((uint)variant << VariantShift)
                 | ((uint)lifetime << LifetimeShift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetElement(uint cell) => (byte)((cell & ElementMask) >> ElementShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint WithElement(uint cell, byte element)
            => (cell & ~ElementMask) | ((uint)element << ElementShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetVariant(uint cell) => (byte)((cell & VariantMask) >> VariantShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint WithVariant(uint cell, byte variant)
            => (cell & ~VariantMask) | ((uint)variant << VariantShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetLifetime(uint cell) => (byte)((cell & LifetimeMask) >> LifetimeShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint WithLifetime(uint cell, byte lifetime)
            => (cell & ~LifetimeMask) | ((uint)lifetime << LifetimeShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CellFlags GetFlags(uint cell) => (CellFlags)((cell & FlagsMask) >> FlagsShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlag(uint cell, CellFlags flag)
            => (cell & ((uint)flag << FlagsShift)) != 0u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetFlag(uint cell, CellFlags flag) => cell | ((uint)flag << FlagsShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ClearFlag(uint cell, CellFlags flag) => cell & ~((uint)flag << FlagsShift);

        /// <summary>Clears every flag bit while leaving element, variant and lifetime intact.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ClearAllFlags(uint cell) => cell & ~FlagsMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(uint cell) => GetElement(cell) == Elements.Empty;
    }
}
