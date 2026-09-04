using Crucible.Core;
using Crucible.Diagnostics;
using Crucible.Sim;
using Unity.Collections;
using UnityEngine;

namespace Crucible.Gameplay
{
    /// <summary>
    /// The composition root and the only <c>Update</c> in the project.
    ///
    /// Everything is constructed here and handed its dependencies explicitly — there is no service
    /// locator, no singleton and no lazy initialisation. If a system needs something, it is passed
    /// in from this file, which means the whole wiring of the game is readable in one place.
    ///
    /// Systems are ticked in a fixed order: input, then simulation, then paint, then upload. That
    /// order is the reason a grain poured this frame is visible this frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrucibleGame : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Camera _camera;
        [SerializeField] private MeshRenderer _quad;
        [SerializeField] private Shader _gridShader;
        [SerializeField] private StatsOverlay _overlay;

        [Header("Simulation")]
        [SerializeField] private bool _overrideTier;
        [SerializeField] private DeviceTier _tier = DeviceTier.Default;
        [SerializeField, Range(1, 12)] private int _brushRadius = 3;

        /// <summary>Simulation runs at a fixed 60 Hz regardless of the display frame rate.</summary>
        private const float TickInterval = 1f / 60f;

        /// <summary>
        /// A stall must not turn into a spiral. If the accumulator falls more than this many ticks
        /// behind, the backlog is dropped rather than paid off — simulated time is allowed to slip,
        /// but the frame rate is not allowed to collapse chasing it.
        /// </summary>
        private const int MaxCatchUpTicks = 2;

        private SandGrid _grid;
        private GridPalette _palette;
        private NativeArray<Color32> _pixels;

        private GridDisplay _display;
        private BrushInput _brush;
        private FrameStats _stats;

        private uint _tick;
        private float _accumulator;
        private bool _paused;

        private void Awake()
        {
            // Explicit, always. Leaving these to the platform default is how a game silently runs
            // at 30 on one device and 120 on another, making every capture meaningless.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            DeviceTier tier = _overrideTier ? _tier : GridSizing.DetectTier();
            int width = GridSizing.WidthFor(tier);
            int height = GridSizing.HeightFor(width, Screen.width, Screen.height);

            // Every runtime buffer is allocated here and never again. Nothing below this line is
            // allowed to allocate per frame.
            _grid = new SandGrid(width, height, Allocator.Persistent);
            _palette = GridPalette.CreateDefault(Allocator.Persistent);
            _pixels = new NativeArray<Color32>(_grid.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            _display = new GridDisplay();
            _display.Initialise(_camera, _quad, _gridShader, width, height);

            _brush = new BrushInput { Radius = _brushRadius, Element = Elements.Sand };
            _brush.Initialise(_camera, width, height);

            _stats = new FrameStats();
            _stats.Start();

            SeedSandbox();
        }

        private void Update()
        {
            if (_paused)
            {
                return;
            }

            _display.TickScreen();

            // Input first, so material poured this frame falls this frame.
            _brush.Tick(ref _grid, _tick);

            StepSimulation();

            using (ProfilerMarkers.Paint.Auto())
            {
                GridPainter.Paint(in _grid, in _palette, _pixels);
            }

            _display.Upload(_pixels);

            _stats.Sample();
            if (_overlay != null)
            {
                _overlay.Render(_stats, _grid.Width, _grid.Height, _tick);
            }
        }

        private void StepSimulation()
        {
            _accumulator += Time.deltaTime;

            int ticksThisFrame = 0;
            while (_accumulator >= TickInterval && ticksThisFrame < MaxCatchUpTicks)
            {
                using (ProfilerMarkers.Simulation.Auto())
                {
                    NaiveSimulation.Step(ref _grid, _tick);
                }

                _tick++;
                _accumulator -= TickInterval;
                ticksThisFrame++;
            }

            if (_accumulator >= TickInterval)
            {
                // Still behind after the catch-up budget: drop the debt.
                _accumulator = 0f;
            }
        }

        /// <summary>
        /// A stone basin to pour into. M1 has no level format yet — the reference scene used for
        /// measurements is built properly in M7, and this stands in until then.
        /// </summary>
        private void SeedSandbox()
        {
            int width = _grid.Width;
            int height = _grid.Height;

            const int FloorThickness = 6;
            const int WallThickness = 6;
            int wallHeight = height / 3;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isFloor = y < FloorThickness;
                    bool isWall = y < wallHeight && (x < WallThickness || x >= width - WallThickness);

                    if (!isFloor && !isWall)
                    {
                        continue;
                    }

                    int index = _grid.Index(x, y);
                    _grid.Cells[index] = Cell.Make(Elements.Stone, Hash.Byte(0u, index));
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // Stop simulating in the background, and do not let the accumulator bank the time spent
            // away — otherwise resuming would try to pay off minutes of ticks in one frame.
            _paused = paused;
            _accumulator = 0f;
        }

        private void OnApplicationFocus(bool focused)
        {
            _paused = !focused;
            _accumulator = 0f;
        }

        private void OnDestroy()
        {
            _stats?.Dispose();
            _display?.Dispose();

            if (_pixels.IsCreated)
            {
                _pixels.Dispose();
            }

            _palette.Dispose();
            _grid.Dispose();
        }
    }
}
