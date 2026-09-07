# Architecture

## Overview

The project is intentionally small and flat: one scene, one prefab (a grid cell) and a
handful of C# scripts in `Assets/_Scripts/`. There is **no assembly definition** and **no
namespaces** — MonoBehaviours are referenced from the scene by file GUID, so class names and
the serialized field names listed in [Scene wiring](04-SCENE-WIRING.md) are load-bearing.

```
Unity scene (BonusRoundScene)
  Canvas ──────────────── static HUD (rounds card, player plates, boards, win screen)
  Background             dark backdrop
  Player1 / Player2      name + score plates (bottom / top)
  Grids > Grid1, Grid2   boards made of 20 Element prefab instances each
  OutcomeManager         weights for BLANK/STEAL/BOOST/SHIELD
  BonusRoundManager      the show director
  [RuntimeUi]            created at runtime: legend card, SFX/speed buttons,
                         FX layer (announcement cards, floats, flash, confetti)
```

## Script map

| Script | Layer | Responsibility |
| --- | --- | --- |
| `BonusRoundManager` | Gameplay | Round loop, turn order, pacing, status lines, scores, win screen. |
| `OutcomeManager` | Gameplay | Weighted rolls + applying each outcome with staged feedback. |
| `PlayerController` | Gameplay | Zone list ownership (take/drop), score calculation + count-up. |
| `GridElement` | Gameplay | One zone: owner/value/shield model + animated visuals. |
| `OutcomeType` | Gameplay | The enum + shared presentation metadata (name, colour, icon, captions). |
| `AppServices` | Core | Lazy service registry (Canvas, coordinate conversion, verbose log). |
| `Tweens` / `Easing` | Core | Dependency-free tween engine (float/color/vector/scale/fade, delays). |
| `Sfx` | Audio | Runtime-synthesized SFX + pooled AudioSources + persisted volume. |
| `RuntimeUi` | UI | Runtime HUD: odds legend, SFX/speed buttons, FX layer & coroutines. |
| `UiKit` | UI | Factories for rects/images/TMP texts (used by RuntimeUi & FX). |
| `SpriteFactory` | FX | Cached procedural sprites (rounded rect, circle, ring, diamond...). |
| `ZoneFx` | FX | Cell effects: pop, shake, expanding rings, floating text on a cell. |

## Round lifecycle

A round plays through these stages (see `BonusRoundManager.RunBonusRounds`):

1. **Intro beat** — soft screen flash + whoosh after the scene settles.
2. **P1 turn** (`PlayTurn`):
   a. Roll outcome (`OutcomeManager.DetermineRandomOutcome`).
   b. Pulse both plates; update P1 status line (coloured outcome label).
   c. Announcement card over P1's board (`RuntimeUi.AnnounceOutcome`, ~0.9 s).
   d. Accent screen flash, reveal tick.
   e. `OutcomeManager.PlayOutcome(...)` resolves effects with per-zone animations
      (staggered waits so each affected zone is readable).
   f. Both score cards count up; `+N/-N` floats appear over the plates.
3. **P2 turn** — identical with roles swapped.
4. **Round end** — rounds-left card pops + tick; short gap; repeat.
5. **Game end** (`EndGame`) — dramatic tick/flash, winner resolution, fanfare or draw
   sting, confetti, win screen fade-in with stats, Replay button.

Every wait (`WaitFor.Seconds`) and every tween honours `Tweens.TimeScale`, so **turbo mode
never desyncs logic from animation**.

## Data flow for a zone state change

```
OutcomeManager ── PickDistinct(rival, 2)
   └─> rival.DropZone(zone)             (list only)
   └─> actor.TakeZone(zone)             zone.ChangeControl(actor.number, animated)
                                          └─> colour tween + pop + ring + selection flash
Score cards are recalculated after each outcome via PlayerController.RefreshScore(true)
(count-up tween + tick), then deltas float up from the plates.
```

## Coordinate system for runtime FX

Runtime elements are parented to the full-screen Canvas and positioned with the
"canvas-centre" anchor. `App.TryGetCanvasCenter(rect)` converts any RectTransform's world
centre into that coordinate space (it handles the CanvasScaler for any screen size), so
announcement cards and floating texts always land on the right element.

## Design constraints (read before refactoring)

1. **Keep the four gameplay class names** — the scene references them by GUID/name.
2. **Keep the serialized field names** listed in `04-SCENE-WIRING.md` — renaming orphans the
   scene data (silently zeroing values).
3. **No namespaces** for MonoBehaviour classes (see above).
4. New gameplay code should go through `App` / `RuntimeUi` / `Sfx` / `Tweens` instead of
   duplicating lookups — all of them are Unity-null-safe after scene reloads.
5. Coroutine waits should use `WaitFor.Seconds(...)` (turbo-aware), not raw `WaitForSeconds`.
