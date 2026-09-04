using Unity.Profiling;

namespace Crucible.Diagnostics
{
    /// <summary>
    /// Named markers for the stages of a frame. Declared in one place because
    /// <see cref="FrameStats"/> attaches recorders to them by name, and a typo in either half
    /// silently produces a zero rather than an error.
    /// </summary>
    public static class ProfilerMarkers
    {
        public const string SimulationName = "Crucible.Simulation";
        public const string PaintName = "Crucible.Paint";
        public const string UploadName = "Crucible.Upload";

        public static readonly ProfilerMarker Simulation = new ProfilerMarker(SimulationName);
        public static readonly ProfilerMarker Paint = new ProfilerMarker(PaintName);
        public static readonly ProfilerMarker Upload = new ProfilerMarker(UploadName);
    }
}
