# Extending the game

Practical recipes for the most common modifications. Read [Architecture](02-ARCHITECTURE.md)
first, and respect the constraints in [Scene wiring](04-SCENE-WIRING.md).

## Change outcome probabilities

Open `BonusRoundScene.unity`, select the `OutcomeManager` object and edit the four weights
in the Inspector (they should sum to 1.0). The odds legend on the right side of the HUD
reads these values automatically.

## Change how many rounds are played

Select `BonusRoundManager` and change the serialized `roundsLeft` (default 10). The rounds
card, pacing and win flow all adapt automatically.

## Change colours (players, outcomes)

- Player tints: select `_Prefabs/Element.prefab` and edit `player1Color` / `player2Color`.
  Because the runtime palette is resolved from a live `GridElement`, every plate, card,
  status accent, ring and win text picks the new colour up automatically.
- Outcome accents (STEAL red, BOOST green, SHIELD blue, BLANK grey): edit
  `OutcomeVisuals` in `Assets/_Scripts/Gameplay/OutcomeType.cs`.

## Add a new outcome

1. Add the value to `enum OutcomeType` (`Gameplay/OutcomeType.cs`).
2. Add its weight field to `OutcomeManager` and include it in
   `DetermineRandomOutcome()`'s cumulative ladder; extend `PlayOutcome`'s switch with an
   `ApplyXxx` coroutine that mutates zones through `PlayerController`/`GridElement` APIs.
3. Give it presentation metadata in `OutcomeVisuals` (label, caption, colour, icon) and,
   if it deserves a sound, add an `SfxId` + synth recipe in `SfxManager`.
4. The announcement card, status line, legend and floats will all handle it automatically.

## Add a new sound

1. Add the id to `enum SfxId` (`Audio/SfxManager.cs`).
2. Add a `case` in `Synthesize(...)` — helper builders available: `Tone`, `Sweep`,
   `TwoTone`, `NoiseBurst`, `NoiseSweep`, `Fanfare`, `Blend`.
3. Call `Sfx.Play(SfxId.YourId, jitter)` wherever the moment happens.

## Add a new zone effect

Compose it from primitives in `FX/ZoneFx.cs` (`Pop`, `Shake`, `EmitRing`,
`FloatTextOnCell`) or `Tweens` directly. For a looping effect on every cell, hook into
`GridElement` (e.g. `PlayBoostVisuals`) or `Start()` for once-only setup.

## Change the HUD layout (legend / buttons / cards)

All positions are constants in `RuntimeUi.Build*` methods — they use canvas-centre
coordinates (1920x1080 reference space) and never depend on the scene:

- `BuildOddsLegend`: card position `(455, 30)`, row `y` offsets.
- `BuildControls`: button bar at `(455, -190)`.
- `AnnounceOutcome`: card size 250x80 and the +52 offset above the board centre.

## Make the game interactive instead of autoplay

The clean seam is `BonusRoundManager.PlayTurn`: it currently calls
`DetermineRandomOutcome()` internally. Replace it with a coroutine that waits for input
(a button / keyboard), then call the same resolve path
(`AnnounceOutcome` -> `OutcomeManager.PlayOutcome` -> score refresh) unchanged.

## Add pause / resume

`WaitFor.Seconds` and the tween engine respect `Time.timeScale == 0` naturally (they
accumulate `Time.deltaTime`), so pausing is literally `Time.timeScale = 0f` — the show
freezes mid-animation and resumes cleanly. The SFX/speed buttons remain responsive since
they run on click events, not timers.

## Build a release

File > Build Settings > add `BonusRoundScene` > build for your platform. The runtime UI,
audio and FX layers need no asset bundles or extra scenes. TMP essentials are already in
the project, so nothing else is required for the UI to render.
