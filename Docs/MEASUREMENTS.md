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
| — | — | — | — | — | editor reference |

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

No captures yet. The first is the M1 baseline.

## Summary

Filled from the log above. Same reference scene, same device, one row per milestone.

| Configuration | Sim ms | Frame ms | FPS |
|---|---|---|---|
| M1 — naive: single-threaded, no chunking | — | — | — |
| M3 — + chunking and dirty rects | — | — | — |
| M4 — + Burst | — | — | — |
| M4 — + parallel jobs | — | — | — |
