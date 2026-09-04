using System;
using Unity.Profiling;

namespace Crucible.Diagnostics
{
    /// <summary>
    /// Samples the numbers the project is judged on: frame time, the cost of each stage, and how
    /// much garbage a frame produced.
    ///
    /// GC allocation is reported as the last frame's raw value rather than an average, because the
    /// target is zero and an average hides an occasional spike. Times are averaged over a short
    /// window so the readout is legible while still moving when something changes.
    /// </summary>
    public sealed class FrameStats : IDisposable
    {
        private const int SampleWindow = 30;

        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _gcAllocated;
        private ProfilerRecorder _simulation;
        private ProfilerRecorder _paint;
        private ProfilerRecorder _upload;

        public double FrameMs { get; private set; }
        public double SimulationMs { get; private set; }
        public double PaintMs { get; private set; }
        public double UploadMs { get; private set; }
        public long GcAllocatedBytes { get; private set; }

        public void Start()
        {
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", SampleWindow);
            _gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _simulation = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, ProfilerMarkers.SimulationName, SampleWindow);
            _paint = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, ProfilerMarkers.PaintName, SampleWindow);
            _upload = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, ProfilerMarkers.UploadName, SampleWindow);
        }

        public void Sample()
        {
            FrameMs = AverageMilliseconds(_mainThread);
            SimulationMs = AverageMilliseconds(_simulation);
            PaintMs = AverageMilliseconds(_paint);
            UploadMs = AverageMilliseconds(_upload);
            GcAllocatedBytes = _gcAllocated.Valid ? _gcAllocated.LastValue : 0L;
        }

        /// <summary>
        /// Averages a timing recorder's samples and converts to milliseconds. Recorder values are
        /// nanoseconds.
        /// </summary>
        private static double AverageMilliseconds(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
            {
                return 0.0;
            }

            int count = recorder.Count;
            if (count == 0)
            {
                return 0.0;
            }

            double total = 0.0;
            for (int i = 0; i < count; i++)
            {
                total += recorder.GetSample(i).Value;
            }

            return total / count * 1e-6;
        }

        public void Dispose()
        {
            _mainThread.Dispose();
            _gcAllocated.Dispose();
            _simulation.Dispose();
            _paint.Dispose();
            _upload.Dispose();
        }
    }
}
