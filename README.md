# Crucible

A falling-sand alchemy puzzle for Android and iOS, built as a mobile performance engineering study.

Sand piles. Water finds its level. Fire jumps to anything flammable. Lava turns to stone on contact with water, and turns sand into glass. Each level gives you fixed geometry, a limited budget of materials, and a target: fill the marked region with a given material and keep it stable for one second.

The game is the vehicle. The point is what it takes to run a 147,000-cell cellular automaton at 60 fps on a mid-range Android phone.

**Status:** M1 done. Sand falls, piles and settles; the grid renders in one draw call; the simulation and paint stages allocate nothing. The editor baseline is recorded. The device baseline is not — that is the number the project is actually about, and it stays `—` until a phone produces it. See the roadmap below.

---

## Technical approach

| Area | Approach |
|---|---|
| Cell storage | `NativeArray<uint>` — 4 bytes per cell, bit-packed (element, variant, lifetime, flags). No per-cell objects, no pointer chasing. |
| Grid size | Width fixed per device tier, height derived from screen aspect and snapped to the chunk size. 288 × 512 by default. |
| Sleeping | 32 × 32 chunks with dirty rects. Settled chunks are skipped entirely. |
| Threading | Checkerboard phasing — chunks split into 4 non-adjacent groups, each phase an `IJobParallelFor`. No locks, no atomics, disjoint writes only. |
| Compilation | Burst on the simulation and pixel-conversion jobs. |
| Rendering | Whole grid converted to one `Texture2D` and drawn on a single quad. One draw call for the play area. |
| Determinism | Position-independent hash seeded from `(tick, cellIndex)`. No shared RNG state, so results do not depend on thread scheduling. |
| Allocation | All buffers allocated once at startup with `Allocator.Persistent`. Target: 0 B GC allocation per frame in steady state. |

Determinism is load-bearing rather than decorative: rewind, frame stepping, and solution validation all depend on it.

### Frame budget

Target is 60 fps on a Snapdragon 6-series class device — a 16.6 ms budget:

| Stage | Budget |
|---|---|
| Simulation (4 phases, Burst, parallel) | ≤ 6.0 ms |
| Pixel conversion + texture upload | ≤ 2.0 ms |
| Rendering (URP 2D, 1 quad + UI) | ≤ 2.0 ms |
| Game logic, input, UI | ≤ 1.5 ms |

If the budget is exceeded, the simulation drops from 60 Hz to 30 Hz. Grid resolution is chosen once at startup and never reduced at runtime.

---

## Measurements

Two tables, never mixed. The device table is the one the performance claim rests on, and it is empty until a phone fills it. No estimated numbers are entered anywhere.

**Device — mid-range Android.** Not captured yet.

| Configuration | Sim ms | Frame ms | FPS |
|---|---|---|---|
| Naive: single-threaded, no chunking | — | — | — |
| + chunking and dirty rects | — | — | — |
| + Burst | — | — | — |
| + parallel jobs | — | — | — |

**Editor reference.** Ryzen 7 7800X3D, RTX 5070 Ti, D3D12, 384 × 832 (319,488 cells), Unity editor play mode, Mono. This exists only so successive milestones have a same-machine comparison; it says nothing about mobile.

| Configuration | Sim avg | Sim p95 | Paint avg | Upload avg |
|---|---|---|---|---|
| M1 — naive: single-threaded, no chunking | 5.72 ms | 6.07 ms | 4.98 ms | 0.06 ms |
| + chunking and dirty rects | — | — | — | — |
| + Burst | — | — | — | — |
| + parallel jobs | — | — | — | — |

Paint is already 2.5× over its 2.0 ms budget at M1, which makes `GridPainter` the clearest Burst target. Steady-state GC allocation in the simulation and paint stages is 0 B, measured in isolation — Unity's per-frame counter cannot be trusted in the editor, since it reports a larger figure with the game driver disabled entirely.

Full capture context, including why the editor GC counter is unusable, is in [`Docs/MEASUREMENTS.md`](Docs/MEASUREMENTS.md).

The build ships a diagnostic overlay with runtime A/B switches for chunking, jobs, and Burst, so the difference each optimization makes can be toggled and observed on the device rather than argued about.

---

## Roadmap

| # | Milestone | Status |
|---|---|---|
| M0 | Project scaffolding, assemblies, mobile settings | done |
| M1 | Grid, texture rendering, brush input — deliberately naive, for the baseline capture | editor baseline done, device baseline pending |
| M2 | Material rules: powder, liquid, gas, solid; reaction table | |
| M3 | Chunking and dirty rects | |
| M4 | Burst and parallel jobs | |
| M5 | Full 14-material set | |
| M6 | Flow control: pause, frame step, snapshot ring, rewind | |
| M7 | Level system, budgets, goal checking, level editor | |
| M8 | UI and diagnostic overlay | |
| M9 | Android and iOS builds, on-device profiling | |
| M10 | 24 levels and sandbox mode | |

---

## Building

Unity `6000.3.10f1`, URP 17.3.0, 2D Renderer. Open the project folder in Unity Hub and let the package manager resolve; there is no other setup step.

Player settings are configured for mobile: IL2CPP, ARM64 only, portrait, medium managed stripping. Android minimum SDK 24; iOS minimum 15.0, Metal only.

## Layout

```
Assets/_Project/Scripts/
  Core/         bit packing, hashing, ring buffer
  Sim/          grid, chunk manager, material rules, jobs
  Gameplay/     level loading, goal checking, input, brush, budget
  UI/           palette, HUD, flow controls
  Diagnostics/  counters, overlay, A/B switches
  Editor/       level editor, material table editor
```

Each folder is a separate assembly definition, so iterating on the simulation only recompiles the simulation.

Design notes, including the full material and reaction design, are in [`Docs/DESIGN.md`](Docs/DESIGN.md) (Turkish).
