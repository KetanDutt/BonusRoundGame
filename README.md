# Bonus Round Game

An auto-playing, two-bot **bonus round slot feature** built in Unity (URP 2D + TextMesh Pro).

Two bots — **Player 1** (orange) and **Player 2** (blue) — battle over two 4x5 zone boards for
10 bonus rounds. Every round each bot rolls one of four outcomes — `BLANK`, `STEAL`, `BOOST`,
`SHIELD` — that steals zones, multiplies scores or protects territory. No input is required:
the game runs itself from launch to the final victory banner.

> Created with **Unity 2022.3.5f1 LTS** (also compatible with 2021.3+). Open the project,
> load `Assets/Scenes/BonusRoundScene.unity`, press Play.

---

## Highlights

- **Full autoplay demo** — 10 rounds, both players take turns automatically, round counter
  ticks down, results are announced and applied with animated feedback.
- **Faithful rules** — outcome probabilities and effects follow the original spec
  (35/25/30/10 %; steal up to 2 zones, boost up to 3 zones with 2x/3x/5x/10x multipliers,
  shield up to 2 zones; shielded zones block steals).
- **Win logic per spec** — most controlled zones wins; equal zones fall back to highest
  score; otherwise a draw banner is shown.
- **Runtime FX & UI layer** — outcome announcement cards, floating texts, screen flashes,
  confetti, count-up scores — all generated in code (no external art dependencies).
- **Procedural SFX** — every sound effect (ticks, whooshes, steals, boosts, shields, fanfares)
  is synthesized at runtime; the repo ships zero binary audio.
- **Zero third-party runtime dependencies** — the tween engine, sprite factory and audio
  synth are self-contained, so the project builds offline and stays tiny.
- **Built-in controls** — SFX mute and turbo speed buttons are added to the HUD at runtime.

## How to run

1. Install **Unity 2022.3.x LTS** (or newer) with the **2D (URP)** template modules.
2. Open this folder as a project (Unity will import assets and create `Library/`).
3. Open `Assets/Scenes/BonusRoundScene.unity`.
4. Press **Play** — the show starts automatically.

Optional editor niceties:

- Set **`App.VerboseLogging`** (Assets/_Scripts/Core/AppServices.cs) to log each round's
  outcome and final stats to the Console.

## Controls (runtime HUD)

| Control | What it does |
| --- | --- |
| **SFX: ON / OFF** (bottom right) | Mutes/unmutes all sound effects (persisted between sessions). |
| **SPEED x1 / x2.6** (bottom right) | Toggles turbo mode — speeds up waits *and* animations by 2.6x. |

## Project layout

```
Assets/
  Scenes/            BonusRoundScene.unity — the only gameplay scene
  _Prefabs/          Element.prefab — one grid cell (20 instances per board)
  Settings/          URP 2D pipeline settings
  TextMesh Pro/      TMP essentials (font, materials, shaders)
  _Scripts/
    Core/            AppServices (service registry), Easing, Tween engine
    Audio/           SfxManager — runtime synthesized SFX pool
    FX/              SpriteFactory (procedural sprites), ZoneFx (cell effects)
    UI/              UiKit (UI factories), RuntimeUi (runtime HUD/FX layer)
    Gameplay/        BonusRoundManager, OutcomeManager, PlayerController,
                     GridElement, OutcomeType
Docs/                Full documentation set (see below)
```

## Documentation

Everything you need is in the [Docs](Docs) folder:

| Document | Contents |
| --- | --- |
| [Gameplay & rules](Docs/01-GAMEPLAY-AND-RULES.md) | Exact game rules, probabilities and scoring. |
| [Architecture](Docs/02-ARCHITECTURE.md) | System overview, round lifecycle, code map. |
| [Polish & FX guide](Docs/03-POLISH-AND-FX.md) | Tweens, SFX synth, VFX catalog and tuning knobs. |
| [Scene wiring](Docs/04-SCENE-WIRING.md) | How the scene binds to scripts (serialized refs). |
| [Performance](Docs/05-PERFORMANCE.md) | Allocations, pooling and profiling notes. |
| [Extending the game](Docs/06-EXTENDING.md) | Add outcomes, sounds, boards, new UI. |
| [QA checklist](Docs/07-QA-CHECKLIST.md) | Manual verification plan for a release build. |

Also see [CHANGELOG.md](CHANGELOG.md) for the polish/overhaul history and the
[assignment brief](Docs/Assignment%20Brief%20(Bonus%20Round%20Slot%20Game).docx) for the
original requirements this prototype was built against.

## Design notes

- **No namespaces.** Scripts are compiled into the default `Assembly-CSharp`, and the scene
  references MonoBehaviours by file GUID — namespaces would silently break those bindings.
  All serialized field names documented in [Scene wiring](Docs/04-SCENE-WIRING.md) are
  load-bearing and must not be renamed.
- **All polish is code-driven.** The announcement cards, rings, confetti and sounds spawn at
  runtime, which keeps the scene small and makes every effect easy to tune without
  hand-editing UI prefabs.
- **Turbo mode** is implemented as a global tween/waits time-scale
  (`Tweens.TimeScale`), so speeding up never desyncs animations from game logic.

## License

See [LICENSE](LICENSE). Font: Liberation Sans (SIL OFL) — see
`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`.
