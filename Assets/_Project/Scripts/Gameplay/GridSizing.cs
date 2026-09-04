using UnityEngine;

namespace Crucible.Gameplay
{
    public enum DeviceTier
    {
        Low = 0,
        Default = 1,
        High = 2
    }

    /// <summary>
    /// Chooses the grid dimensions once, at startup.
    ///
    /// Width is fixed per tier. Height follows the screen aspect so the play area fills the phone
    /// rather than being letterboxed, and is snapped to a multiple of the chunk size — chunking
    /// arrives in M3 and a grid that is not chunk-aligned would have to be rebuilt then.
    ///
    /// The tier is never changed at runtime. Dropping resolution mid-session to hold a frame rate
    /// would make every measurement incomparable; the simulation drops to 30 Hz instead.
    /// </summary>
    public static class GridSizing
    {
        public const int ChunkSize = 32;

        private const int MinHeight = 256;
        private const int MaxHeight = 1024;

        public static int WidthFor(DeviceTier tier)
        {
            switch (tier)
            {
                case DeviceTier.Low: return 192;
                case DeviceTier.High: return 384;
                default: return 288;
            }
        }

        /// <summary>
        /// A deliberately crude tier guess. Real device tiering needs a lookup table built from
        /// measurements on actual hardware, which is M9 work — this only has to be sane until then.
        /// </summary>
        public static DeviceTier DetectTier()
        {
            int cores = SystemInfo.processorCount;
            int memoryMb = SystemInfo.systemMemorySize;

            if (cores <= 4 || memoryMb <= 3072)
            {
                return DeviceTier.Low;
            }

            if (cores >= 8 && memoryMb >= 7168)
            {
                return DeviceTier.High;
            }

            return DeviceTier.Default;
        }

        /// <summary>Grid height for a given width and screen, snapped to the chunk size.</summary>
        public static int HeightFor(int width, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                screenWidth = 9;
                screenHeight = 16;
            }

            float aspect = (float)screenHeight / screenWidth;
            int raw = Mathf.RoundToInt(width * aspect);

            int snapped = Mathf.RoundToInt(raw / (float)ChunkSize) * ChunkSize;
            if (snapped < ChunkSize)
            {
                snapped = ChunkSize;
            }

            return Mathf.Clamp(snapped, MinHeight, MaxHeight);
        }
    }
}
