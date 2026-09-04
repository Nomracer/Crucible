using TMPro;
using UnityEngine;

namespace Crucible.Diagnostics
{
    /// <summary>
    /// The M1 readout. Deliberately plain — the full overlay with the A/B switches is M8, and this
    /// exists only so the baseline can be read off the screen.
    ///
    /// It has no <c>Update</c>; the driver ticks it, like everything else. Text goes to TextMeshPro
    /// through <c>SetCharArray</c> so that displaying the allocation counter does not itself
    /// allocate.
    /// </summary>
    public sealed class StatsOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        private readonly CharBuffer _buffer = new CharBuffer(256);

        public void Render(FrameStats stats, int gridWidth, int gridHeight, uint tick)
        {
            if (_label == null)
            {
                return;
            }

            _buffer.Clear();

            _buffer.Append("frame  ");
            _buffer.AppendFixed(stats.FrameMs, 2);
            _buffer.Append(" ms");
            _buffer.AppendLine();

            _buffer.Append("sim    ");
            _buffer.AppendFixed(stats.SimulationMs, 2);
            _buffer.Append(" ms");
            _buffer.AppendLine();

            _buffer.Append("paint  ");
            _buffer.AppendFixed(stats.PaintMs, 2);
            _buffer.Append(" ms");
            _buffer.AppendLine();

            _buffer.Append("upload ");
            _buffer.AppendFixed(stats.UploadMs, 2);
            _buffer.Append(" ms");
            _buffer.AppendLine();

            _buffer.Append("gc     ");
            _buffer.AppendInt(stats.GcAllocatedBytes);
            _buffer.Append(" B/frame");
            _buffer.AppendLine();

            _buffer.Append("grid   ");
            _buffer.AppendInt(gridWidth);
            _buffer.Append('x');
            _buffer.AppendInt(gridHeight);
            _buffer.Append("  tick ");
            _buffer.AppendInt(tick);

            _label.SetCharArray(_buffer.Chars, 0, _buffer.Length);
        }
    }
}
