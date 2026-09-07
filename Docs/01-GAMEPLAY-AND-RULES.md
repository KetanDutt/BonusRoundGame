# Gameplay and Rules

This document is the single source of truth for how the bonus round plays. It is written to
match the original assignment brief (`Assignment Brief (Bonus Round Slot Game).docx`) and
documents the exact behaviour implemented in code.

## Concept

- Two bot players battle over two 4x5 boards (20 zones each).
  **Player 1** (orange) starts with the bottom board, **Player 2** (blue) with the top board.
- Every zone starts with value **x2** and belongs to its player.
- The game is a "bonus round" show: it plays automatically with **no user interaction**.
- There are **10 bonus rounds**. Each round = one outcome for Player 1, then one outcome for
  Player 2. The round counter on the left decrements once both players have rolled.
- The first round starts as soon as the game launches; the next round starts automatically.

## Outcomes

Each outcome is rolled from the same weighted table for both players:

| Outcome | Chance |
| --- | --- |
| BLANK | 35 % |
| STEAL | 25 % |
| BOOST | 30 % |
| SHIELD | 10 % |

Weights are serialized on the `OutcomeManager` component in the scene, so they can be tuned
from the Inspector (they should sum to 1).

### STEAL

- Takes control of up to **2 random zones** currently controlled by the opponent.
- If the opponent controls fewer zones, the excess steal attempts are ignored.
- If a **shielded** zone is picked, the shield is consumed and destroyed instead — the zone
  stays with the opponent and becomes stealable from the next round.
- Picked zones are distinct (a zone cannot be stolen twice in the same outcome).

### BOOST

- Multiplies the value of up to **3 random zones** the player controls.
- The multiplier is rolled per zone:

| Multiplier | Chance |
| --- | --- |
| x2 | 35 % |
| x3 | 30 % |
| x5 | 25 % |
| x10 | 10 % |

- If the player controls fewer zones, the excess boost attempts are ignored (each zone can
  only be boosted once per outcome).
- Boosted zone values are multiplied, e.g. x2 -> x10 after a x5 boost.

### SHIELD

- Protects up to **2 random zones** the player controls.
- If a pick lands on an already-shielded zone, that pick is ignored (the zone is not
  shielded twice).
- Shielded zones can still be boosted.
- If a steal targets a shielded zone, the shield breaks and the zone is not stolen.

### BLANK

- No action is performed (soft visual/audio only).

## Scoring and winner

- A player's score is the **sum of the values of all zones they currently control**.
- When the 10 rounds are over:

1. The player controlling **more zones** wins.
2. If both control the same number of zones, the player with the **higher total score** wins.
3. If both are also tied on score, the game is a **draw**.

A full-screen banner announces the result with zone and score statistics for both players,
and a Replay button restarts the demo (fresh scene load).

## Display rules

- Each player has a status line (the "update" texts in the scene) that shows their **latest
  outcome** — it is coloured per outcome and pulses when it changes.
- An outcome announcement card pops up above the acting player's board before the effect
  resolves, showing the outcome icon, name and short caption.
- The rounds-left card counts down with a pop and tick each round.
- Zone changes always animate: colour cross-fades, scale pops, expanding rings, floating
  labels ("TAKEN!", "x5", "SHIELDED") and screen flashes keep the state changes readable.

## Failure modes (defensive behaviour)

If a controller reference is missing from the scene, the manager logs and skips that turn
gracefully. If `OutcomeManager` is missing, outcomes default to `BLANK`. If any UI reference
is missing the game still runs its logic without that visual.
