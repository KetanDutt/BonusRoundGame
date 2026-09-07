# QA checklist

Manual verification plan for `BonusRoundScene`. Run through it after any gameplay or
presentation change, then again once in a release build (standalone/web/mobile).

## Setup

- [ ] Project opens without console errors in Unity 2022.3.x (URP 2D template).
- [ ] Opening `BonusRoundScene` shows no missing-script warnings (purple text).
- [ ] Press Play: no exceptions appear during the whole 10-round show.

## Gameplay rules

- [ ] Boards start fully owned: Player 1 bottom board orange with 20 zones, Player 2 top
      board blue with 20 zones; every zone shows "x2"; both scores read 40.
- [ ] Rounds card starts at 10 and decrements by 1 only after BOTH players have rolled.
- [ ] Exactly 10 rounds run; order is P1 then P2 every round; game ends at 0.
- [ ] Each player's status line shows their latest outcome (STEAL/BOOST/SHIELD/BLANK) and
      stays until their next turn.

### STEAL
- [ ] Steals up to 2 distinct zones; the stolen zone changes colour to the thief and
      counts toward their zone list/score; count/score update on both plates.
- [ ] If the victim controls 1 zone, only 1 zone is stolen.
- [ ] Stealing a shielded zone consumes the shield (blue ring flashes and breaks), the zone
      is NOT stolen, and it is stealable on later rounds.

### BOOST
- [ ] Up to 3 distinct own zones get a multiplier from {x2 35%, x3 30%, x5 25%, x10 10%};
      zone label updates (x2 -> x6, etc.) and score grows accordingly.
- [ ] Fewer zones -> fewer boosts; each zone boosted at most once per BOOST.

### SHIELD
- [ ] Up to 2 unprotected own zones get the blue shield ring.
- [ ] A pick that lands on an already-shielded zone does nothing (no double shield).
- [ ] Shielded zones can still be boosted.

### BLANK
- [ ] Nothing changes on the boards; soft visual/sound only.

## End of game

- [ ] Winner = most zones; zone-count tie broken by score; full tie -> draw banner.
- [ ] Banner shows headline + both players' zone/score stats; win fanfare or draw sting;
      confetti on the win screen.
- [ ] Replay button reloads the scene cleanly (rounds back to 10, scores 40/40, no
      leftover confetti/cards/tweens from the previous run).

## Polish / presentation

- [ ] Every outcome shows: status line pop, announcement card over the acting board
      (icon + name + caption), accent screen flash, reveal tick.
- [ ] Zone state changes always animate (colour cross-fade, pop, ring, floating label) —
      no jump-cuts.
- [ ] Score cards count up/down with a pop sound and +/- delta floats over the plates.
- [ ] Odds legend on the right matches the Inspector weights.
- [ ] Sounds exist and fit: whoosh (cards), tick (counter), steal zap, boost rise, shield
      ping, fanfare/draw.
- [ ] SFX button mutes everything and the choice survives scene reload.
- [ ] SPEED x2.6 visibly accelerates the whole show (waits AND animations) without
      desync, and resets to x1 after replay.
- [ ] Rounds card pulses with a tick each round.
- [ ] No visual elements remain on screen after win screen appears except the intended
      banner + controls.

## Robustness / performance

- [ ] Resize the Game view while playing (16:9, 4:3, ultrawide): HUD stays on screen and
      effects land on the right elements (CanvasScaler works).
- [ ] Profiler: no per-frame GC allocations during steady state (see 05-PERFORMANCE.md).
- [ ] Rapid replay spam (click Replay many times) never double-starts rounds.
- [ ] Disable `OutcomeManager` in the scene -> game still completes with BLANK outcomes.
- [ ] Build a release for your target platform and repeat the full checklist.
