# Performance and optimization notes

This is a tiny UI game (2x20 zones), so raw throughput was never the problem — the goals
were: **no per-frame garbage, no repeated scene lookups, no update loops scattered across
components, and no load-time stalls from big assets.**

## What was done

### 1. Single tween engine instead of per-object Update()s
All animations run through `Tweens` (one `[TweenRunner]` Update). Playable code never
implements `Update()`, so frame cost stays proportional to the number of *active* tweens
(typically < 40, most are one-shot).

### 2. No repeated FindObjectOfType in gameplay paths
`App` caches the Canvas (Unity-null-aware: destroyed references auto-refresh after a scene
reload). `RuntimeUi`, `Sfx` and `Tweens` are created once per play session. The only
scene searches are startup one-offs (`BonusRoundManager.Awake`, `RuntimeUi.Build`).

### 3. Zero binary assets
- SFX are synthesized once per session and cached as `AudioClip`s (8 pooled sources).
- Sprites are procedural textures generated once per shape and cached forever.
- The whole polish layer adds ~100 KB of code, no textures, no audio, no particles.

### 4. Bounded transient UI
Floating texts, rings and confetti destroy themselves when finished. Announcement cards
are strictly one-at-a-time. Confetti is capped (~52 pieces). Floats are capped implicitly
by gameplay pacing (~2-3 concurrent).

### 5. Cheap score/state updates
Scores are recomputed only after an outcome changes state (a 20-element sum), and the
count-up is a single float tween. No per-frame string building — TMP text is only written
when the displayed value actually changes.

### 6. Correct, allocation-light random access
All "pick N random zones" sampling removes from a scratch `List` copy (bounded at 20
elements), and array indices always use `Random.Range(0, count)` — the exclusive upper
bound — avoiding both the classic `IndexOutOfRangeException` bug and unbounded re-roll
loops.

## Cost model (rough, per second of gameplay at x1)

- 2-6 active tweens (each one float/color interpolation + one delegate call).
- 1-3 coroutines (manager flow + short FX coroutines).
- Transient objects created/destroyed: ~2/s average, all tiny UI GameObjects.
- Audio: at most 1-2 one-shot clips simultaneously (8-source pool, no clipping).

Worst case (whole-screen outcome moment): ~60 tiny GameObjects alive for < 1 s.

## Profiling checklist

1. Unity Profiler -> CPU Usage: script main thread should be < 1-2 ms with zero GC
   allocations on steady frames (some startup/creation allocations on round transitions).
2. Check `Audio` mixer for clip-overlap warnings if you raise effect frequency.
3. If you add per-zone behaviours, prefer pooling (reuse GameObjects) over create/destroy.
4. Memory footprint: procedural textures are 128x128 RGBA (64 KB each, ~8 sprites cached)
   and clips are a few hundred KB in total.

## Mobile note

Canvas is Screen-Space-Overlay with a 1920x1080 reference; everything scales via
CanvasScaler. All runtime code is resolution independent (`App.TryGetCanvasCenter`). The
game is single-threaded UI — no physics, no heavy rendering — so it runs comfortably on
low-end devices; cap `Application.targetFrameRate = 60` if battery matters in a release.
