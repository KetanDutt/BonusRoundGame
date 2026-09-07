# Polish & FX guide

Everything below is generated at runtime — the scene ships without a single imported
sound or particle asset. This section explains what exists, how it works and how to tune it.

## Tween engine (`Core/Tween.cs`)

A small, allocation-conscious engine driven by one hidden `[TweenRunner]` GameObject
(`DontDestroyOnLoad`). Every method returns a `TweenHandle` you can `Cancel()`.

Public API (all honour `Tweens.TimeScale`):

| Method | Purpose |
| --- | --- |
| `Tweens.Float(...)` | any float value |
| `Tweens.Color(...)` | colour cross-fades |
| `Tweens.Vector2/Vector3(...)` | positions / scales |
| `Tweens.Delay(seconds, action)` | scheduled callback |
| `Tweens.Scale(transform, to, dur, ease)` | overshoot scale-ins |
| `Tweens.PunchScale(transform, amount, dur)` | pop out and back to rest |
| `Tweens.AnchoredPosition(...)` | UI movement |
| `Tweens.Alpha(canvasGroup, ...)` | fade whole groups |
| `Tweens.FadeGraphic(graphic, ...)` | fade any UI Graphic's alpha |
| `WaitFor.Seconds(n)` | turbo-aware coroutine wait (custom yield) |

Easing curves live in `Easing.cs` (`Ease` enum — Penner-style set incl. `BackOut`,
`ElasticOut`, `BounceOut`).

**Turbo mode** multiplies `Tweens.TimeScale` (default 2.6). Tweens, `WaitFor.Seconds` waits,
and the confetti coroutine all read it, so speeding the show up never breaks sequencing.

## Sounds (`Audio/SfxManager.cs`)

All SFX are PCM-synthesized once, cached in a dictionary and played through a pool of
8 `AudioSource`s (round-robin). Synthesis is simple oscillator/noise math — no binary
assets, no licensing, no import settings to break.

| `SfxId` | Character | Used for |
| --- | --- | --- |
| `UiClick` | short sine blip | buttons |
| `Tick` | triangle up-chirp | round counter, reveal beats |
| `Whoosh` | filtered noise swell | announcement cards, intro |
| `Reveal` | two-tone | outcome resolution moment |
| `Blank` | soft air + low tone | BLANK outcome |
| `Steal` | saw down-sweep + noise zap | zone takeover |
| `StealFail` | low thud | shield intercepts a steal |
| `Boost` | sine rise + sparkle | BOOST |
| `Shield` | metallic ring (fundamental + harmonic) | SHIELD |
| `ZoneGain` | rising blip | ownership flip (unused by default flow) |
| `ScorePop` | bright pop | score count-up |
| `RoundEnd` | descending two-tone | end of a round (unused by default flow) |
| `Win` | C5-E5-G5-C6 fanfare | winner banner |
| `Draw` | neutral two-tone | draw banner |

Tuning:

- Master volume: `Sfx.Volume` (0..1) — persisted in PlayerPrefs under
  `BonusRoundGame.SfxVolume`; the HUD button toggles it.
- Pitch jitter: pass `pitchJitter` to `Sfx.Play(id, jitter)`.
- To change a recipe, edit the matching `Synthesize` case (frequencies, durations, gains,
  waveforms). Clips are rebuilt on next play after an edit in the Editor (static cache is
  per play session).
- `Blend(a, b)` mixes two clips with soft clipping for layered effects.

## Visual effects catalog

| Effect | Trigger | Implementation |
| --- | --- | --- |
| Announcement card | each outcome | rounded card + player-colour accent bar + outcome icon/label/caption; fades & overshoots in above the acting board, exits before resolution |
| Zone colour cross-fade | ownership change | 0.28 s `Color` tween between player tints |
| Zone pop | any state change | `PunchScale` 1.14-1.24 |
| Expanding ring | takeover / boost / shield break | `ZoneFx.EmitRing` (ring sprite, grows + fades) |
| Selection blink | every zone change | white ring quickly scales in and hides |
| Shield ring | shield on | blue ring sprite scales from 1.5 to 1; fades out on break |
| Zone shake | shield break | damped horizontal shake |
| Floating labels | per effect | "TAKEN!", "x5", "SHIELDED", "SHIELD!" rise 64 px and fade |
| Score count-up | every score change | numeric tween + text punch + pop sound |
| Score deltas | after each outcome | "+N / -N" floats above the player plates |
| Status line | outcome reveal | coloured outcome name pulses on the player's update text |
| Screen flash | phase resolution, end beats | full-screen tinted flash fading over 0.3-0.6 s |
| Confetti | win banner | 50+ spinning rectangle/diamond pieces with sway fall |
| Win screen | game end | overlay fades in, headline scales in with back-ease, replay button pops in, legend card hides |
| Rounds-left pop | round end | counter punches & ticks |
| Plate pulse | turn start | both score plates do a subtle 1.035 punch |

## Where the knobs live

- **Pacing** — `BonusRoundManager` Inspector: `settleDelay`, `turnGap`, `roundGap`.
- **Card choreography** — `RuntimeUi.AnnounceOutcome` (in/hold/out durations).
- **Zone effect strengths** — `ZoneFx`, `GridElement` (`Pop` amounts, ring scales).
- **Confetti amount/colors** — `RuntimeUi.BurstConfetti` / `ConfettiColors`.
- **Flash strength** — call sites pass peak alpha (typically 0.05-0.16).
- **Turbo factor** — `RuntimeUi.TurboTimeScale`.

## Accessibility / readability choices

- Outcome statuses use colour **plus text** (never colour alone).
- All floating texts and card text sit on dark contrast backgrounds.
- Mute and speed buttons persist only volume; both reset with the scene on replay.
- Announcement holds (~0.4 s) are deliberately long enough to read at x1 speed.
