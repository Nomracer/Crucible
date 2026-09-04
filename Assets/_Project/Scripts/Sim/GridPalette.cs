using System;
using Crucible.Core;
using Unity.Collections;
using UnityEngine;

namespace Crucible.Sim
{
    /// <summary>
    /// Element id to colour, plus how much per-cell colour jitter each element gets.
    ///
    /// Flat arrays indexed by element id rather than a switch, so adding a material stays a data
    /// change. In M2 these tables are baked from the authoring assets instead of being written
    /// here; the shape of the lookup does not change when that happens.
    /// </summary>
    public struct GridPalette : IDisposable
    {
        public NativeArray<Color32> BaseColour;

        /// <summary>Peak signed deviation applied per channel, in 0..255 units.</summary>
        public NativeArray<byte> Jitter;

        public bool IsCreated => BaseColour.IsCreated;

        public static GridPalette CreateDefault(Allocator allocator)
        {
            var palette = new GridPalette
            {
                BaseColour = new NativeArray<Color32>(Elements.Count, allocator, NativeArrayOptions.ClearMemory),
                Jitter = new NativeArray<byte>(Elements.Count, allocator, NativeArrayOptions.ClearMemory)
            };

            // Near-black ground so that bright materials carry the whole image. Cheap on OLED and
            // it keeps contrast high on a small screen in daylight.
            palette.BaseColour[Elements.Empty] = new Color32(10, 10, 12, 255);
            palette.Jitter[Elements.Empty] = 0;

            palette.BaseColour[Elements.Sand] = new Color32(216, 166, 87, 255);
            palette.Jitter[Elements.Sand] = 18;

            palette.BaseColour[Elements.Stone] = new Color32(74, 74, 82, 255);
            palette.Jitter[Elements.Stone] = 8;

            return palette;
        }

        public void Dispose()
        {
            if (BaseColour.IsCreated)
            {
                BaseColour.Dispose();
            }

            if (Jitter.IsCreated)
            {
                Jitter.Dispose();
            }
        }
    }
}
