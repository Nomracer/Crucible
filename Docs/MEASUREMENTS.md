# Measurements

Every performance number in this project comes from here. The README table is a summary of this log — it is never a separate set of numbers.

## Rules

- **Editor numbers and device numbers are different things.** Never present one as the other. Every entry states which.
- Prefer a release build with the profiler attached. Development builds carry profiler overhead — if a number came from one, say so in the entry.
- The reference scene is fixed and must not change. Changing it invalidates every earlier row.
- Record the distribution, not just the average. The 95th percentile is what a player feels.
- Take a cold capture and a ten-minute sustained capture. Thermal throttling is the honest number.
- If a number was not captured, the cell stays `—`. Never estimate.

## Reference scene

A stone basin filling the lower third of the grid. Sand emitted from a fixed point at the top at a fixed rate. Captured over 30 seconds starting from an empty grid, saved as level data so it replays exactly.

Fixed at M1 and not changed again.

## Device roster

Fill in as devices become available. The performance claim is about mid-range hardware, so a mid-range Android device is the primary target and a flagship is not a substitute.

| Label | Device | SoC | OS | GPU API | Role |
|---|---|---|---|---|---|
| — | — | — | — | — | primary mid-range Android |
| — | — | — | — | — | iOS |
| desktop | Ryzen 7 7800X3D, 32 GB | RTX 5070 Ti | Windows 11 (10.0.26200) | D3D12 | editor reference |

## Capture template

Copy this block for each capture. Do not delete old entries — the history is the point.

```
### <milestone> — <label> — <date>

Device:        
OS:            
Build:         release / development, IL2CPP, ARM64
Graphics API:  
Grid:          <width> x <height> (<cells> cells)
Scene:         reference scene
Duration:      
Toggles:       chunking on/off, jobs on/off, burst on/off

Frame time:    avg __ ms   p95 __ ms   max __ ms
Simulation:    avg __ ms   p95 __ ms
Paint+upload:  avg __ ms
Render:        avg __ ms
GC alloc:      __ B / frame
Active chunks: __ / __
Draw calls:    __      SetPass: __
Memory:        __ MB resident

Notes:
```

## Log

### M1 baseline — editor reference — 2026-09-05

Device:        AMD Ryzen 7 7800X3D, 8 cores / 16 threads, 32 GB RAM
GPU:           NVIDIA GeForce RTX 5070 Ti
OS:            Windows 11 (10.0.26200)
Build:         **Unity editor play mode**, Mono, not a player build
Graphics API:  Direct3D12
Unity:         6000.3.10f1
Grid:          384 x 832 (319,488 cells)
Scene:         **stand-in basin**, not the reference scene — see note
Duration:      ~1400 ticks at a fixed 60 Hz
Toggles:       chunking off, jobs off, burst off (M1 has none of them)

Frame time:    avg 16.49 ms   p95 21.37 ms   max 23.21 ms
Simulation:    avg 5.72 ms    p95 6.07 ms    max 6.11 ms
Paint:         avg 4.98 ms    p95 5.11 ms    max 5.66 ms
Upload:        avg 0.06 ms    p95 0.08 ms    max 0.13 ms
GC alloc:      0 B / frame in the simulation and paint stages, measured in isolation
Active chunks: n/a — M1 has no chunking, the whole grid is scanned every tick
Draw calls:    1 for the play area (2 more for the diagnostic overlay canvas)
Memory:        not captured

Load:          100,812 sand cells, basin interior filled to one third
Second sample: sim avg 5.67 / p95 5.90, paint avg 4.92 / p95 5.00, upload avg 0.04

Notes:

- **This is an editor number, not a device number.** It says nothing about mobile
  performance. It exists so the M3, M4 and M5 editor numbers have something on the
  same machine to be compared against. The mobile baseline is a separate row and
  is still missing.
- The reference scene described above does not exist yet; it is level data and
  arrives in M7. This capture used the stand-in basin that `CrucibleGame.SeedSandbox`
  builds, filled to one third with sand. Any capture taken before M7 is labelled the
  same way and is not comparable with post-M7 captures.
- Frame time is pinned near 16.6 ms because `targetFrameRate` is 60 and
  `vSyncCount` is 0. It measures whether the budget is met, not how much headroom
  is left. Simulation and paint are the numbers that matter here.
- **Paint is over budget.** The budget for pixel conversion plus upload is 2.0 ms;
  the measured cost is 5.04 ms, about 2.5x over. `GridPainter` is a flat per-cell
  loop with no vectorisation, so it is the clearest M4 Burst target. Simulation at
  5.72 ms is inside its 6.0 ms budget, on desktop, unoptimised.
- The GC figure needs care. Unity's `GC Allocated In Frame` counter reports roughly
  120-215 KB per frame in the editor, but the same counter reports **more** with the
  driver disabled entirely, which proves the allocation is editor overhead rather
  than game code. Measured in isolation, 600 iterations of
  `NaiveSimulation.Step` plus `GridPainter.Paint` at this grid size allocate
  **0 bytes** and trigger **0 gen-0 collections**. The honest per-frame figure has to
  come from a development build; the editor counter cannot produce it.
- Draw calls verified in the Frame Debugger: three events total, one
  `Renderer2D Pass / DrawSRPBatcher` for the grid quad and two
  `UI.RenderOverlays` events for the stats canvas. The play area is one draw call
  as designed.
- Correctness alongside the capture: 6/6 EditMode tests pass; sand count held at
  100,812 across 600 ticks; in a controlled 64x128 run the pile centroid moved
  0.043 cells over 2000 ticks, so there is no sideways drift.

## Summary

Filled from the log above. Two tables, never mixed: the editor table exists only so
successive milestones have a same-machine comparison, and the device table is the one
the performance claim rests on.

### Editor reference — Ryzen 7 7800X3D / RTX 5070 Ti / D3D12 / 384x832

Stand-in basin scene until M7. Editor play mode, Mono, not a player build.

| Configuration | Sim ms (avg) | Sim ms (p95) | Paint ms (avg) | Upload ms (avg) |
|---|---|---|---|---|
| M1 — naive: single threaded, no chunking | 5.72 | 6.07 | 4.98 | 0.06 |
| M3 — + chunking and dirty rects | — | — | — | — |
| M4 — + Burst | — | — | — | — |
| M4 — + parallel jobs | — | — | — | — |

### Device — mid-range Android

Not captured yet. This is the table that matters.

| Configuration | Sim ms | Frame ms | FPS |
|---|---|---|---|
| M1 — naive: single threaded, no chunking | — | — | — |
| M3 — + chunking and dirty rects | — | — | — |
| M4 — + Burst | — | — | — |
| M4 — + parallel jobs | — | — | — |
