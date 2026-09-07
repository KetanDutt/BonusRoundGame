# Scene wiring reference

This scene was authored manually (no runtime builder), so the bindings below are
**load-bearing**. Renaming any listed field or class silently breaks the scene data
(Unity keeps the serialized values but stops applying them).

All gameplay scripts live in `Assets/_Scripts/Gameplay/`. Moving a script together with its
`.meta` file is safe (Unity tracks MonoBehaviours by GUID); renaming the class or adding a
namespace is not.

## Scene structure (simplified)

```
BonusRoundScene
+-- Main Camera (AudioListener)
+-- Global Light 2D
+-- EventSystem
+-- OutcomeManager              (component: OutcomeManager)
+-- BonusRoundManager           (component: BonusRoundManager)
+-- Canvas  (CanvasScaler: ScaleWithScreenSize 1920x1080, match width)
    +-- Background              (full-screen dark image)
    |   +-- RoundsLeft          (white card, left-centre)
    |   |   +-- Number          (TMP, big counter  -> roundsLeftText)
    |   |   +-- Name / Text     ("Rounds\nLeft")
    |   +-- Player1             (white plate, bottom-centre; name strip orange)
    |   |   +-- Name / Text     ("Player 1")
    |   |   +-- Score           (TMP -> P1 scoreText)
    |   +-- Player2             (white plate, top-centre; name strip blue)
    |   |   +-- Score           (TMP -> P2 scoreText)
    |   |   +-- Name / Text     ("Player 2")
    |   +-- Grids               (vertical layout, centred)
    |   |   +-- Grid1           (4x5 grid, 100 px cells - Player 2's board)
    |   |   +-- Grid2           (4x5 grid - Player 1's board)
    |   +-- Player 1 update     (TMP, left/bottom -> player1Text status line)
    |   +-- Player 2 update     (TMP, left/top    -> player2Text status line)
    +-- WinScreen               (full-screen overlay, inactive by default)
        +-- WinText             (TMP headline/stats)
        +-- Replay / Text (TMP) (Button "Replay")
```

Grid1's 20 prefab instances all carry the `controllingPlayer = 2` override and
Grid2's carry `controllingPlayer = 1`; visually Grid1 is the top board (under Player 2's
plate) and Grid2 the bottom one. Boards themselves are identical; ownership lives on the
instances and in each `PlayerController.gridElements` list (20 zone references wired in the
scene).

### How the boards get positioned (no code needed)

Both board rects serialize with anchor 0,0 and centred pivots, but the parent object
`Grids` (anchored 0.5/0.5 at canvas centre) carries a **VerticalLayoutGroup** (spacing 5,
middle-centre, no child control) plus a **ContentSizeFitter** (expand both). The layout
group overrides child placement every frame, so at runtime the two boards stack vertically
and centred: Grid1 (Player 2) top, Grid2 (Player 1) bottom, 5 px apart.

Each board rect is exactly the size its GridLayoutGroup needs — cells 100x100, spacing 5,
constraint 5 columns -> content 520x415 (5*100+4*5 wide, 4*100+3*5 tall). The boards'
`sizeDelta` (520x415) matches, so no fitting surprises occur. If you ever change the cell
size or the board sizeDelta, keep them in sync (ContentSizeFitter sizes `Grids`, not the
boards).

## Scripts -> serialized fields

### BonusRoundManager (`BonusRoundManager.cs`)

| Field | Type | Scene binding |
| --- | --- | --- |
| `roundsLeftText` | TMP | Canvas/Background/RoundsLeft/Number |
| `player1Controller` | PlayerController | Player1 plate's controller component |
| `player1Text` | TMP | Background/"Player 1 update" |
| `player2Controller` | PlayerController | Player2 plate's controller component |
| `player2Text` | TMP | Background/"Player 2 update" |
| `roundsLeft` | int | 10 (start count) |
| `winScreen` | GameObject | Canvas/WinScreen |
| `winText` | TMP | WinScreen/WinText |
| `replay` | Button | WinScreen/Replay |

New optional fields: `settleDelay`, `turnGap`, `roundGap` (seconds).

### OutcomeManager (`OutcomeManager.cs`)

`blankProbability` (0.35), `stealProbability` (0.25), `boostProbability` (0.30),
`shieldProbability` (0.10) — the four weights must sum to 1.0.

### PlayerController (one per player)

`scoreText` (their Score TMP), `gridElements` (20 `GridElement` refs, the zones they
control), `score` (runtime), `number` (1 or 2).

### GridElement (40 instances of `_Prefabs/Element.prefab`)

`player1Color` / `player2Color` (orange / sky-blue tints), `image` (BG Image), `text`
("x2" TMP), `shieldedGO` / `selectedGO` (overlay GameObjects), `value` (default 2),
`isShielded`, `controllingPlayer` (1 or 2 per board).

### Element prefab internals

```
Element (root, 100x100 via GridLayoutGroup)
+-- BG          white square (tinted per owner)
+-- Text (TMP)  "x2" (auto-size, bold)
+-- Shield      overlay -> re-skinned at runtime as a blue ring
+-- Selected    overlay -> re-skinned at runtime as a white selection ring
```

The prefab's placeholder overlays (solid black/green squares) are re-skinned in
`GridElement.Start` with procedural sprites — no prefab/scene edits required.

## Runtime-created objects (do not wire in the scene)

Created under the Canvas by `RuntimeUi` on the first frame: `[RuntimeUi]` root, `OddsLegend`
card (right side, mirrors the rounds card), `Controls` (SFX + SPEED buttons, bottom right)
and the `FxLayer` that hosts every transient effect (announcement cards, floating texts,
flash image, confetti).

`Sfx` keeps a `[SfxManager]` GameObject with 8 pooled AudioSources; `Tweens` keeps a
`[TweenRunner]`. Both are `DontDestroyOnLoad`, so sounds and tweens survive the replay
scene load seamlessly.

## Editing checklist

After changing anything serialized:

1. Keep field names/classes exactly as above.
2. If you move a `.cs`, move its `.meta` with it (git mv does this automatically).
3. If you add fields, Unity keeps old scene data and adds defaults — verify in the
   Inspector that Inspector values match the scene wiring table.
4. UI position conventions: the Canvas reference resolution is 1920x1080; boards are 520 px
   wide and stacked in the centre column; runtime HUD lives at x ~ +455 (right side),
   static HUD on the left (rounds card at x -400).
