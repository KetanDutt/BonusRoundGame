# Changelog

## 1.1.0 - Production polish pass

### Gameplay & bugs fixed
- Fixed crash/undefined behaviour in random zone sampling: indices now always use
  `Random.Range(0, count)` on a scratch copy, so steals/boosts/shields pick N *distinct*
  zones and can never index out of range or skip list items.
- Scores now resync from zone values at startup (the scene previously shipped with stale
  placeholder numbers that made the first steal delta incorrect).
- Boards paint correctly at boot from serialized ownership (fixed leftover mismatched
  per-instance colours from scene authoring).
- BOOST/SHIELD respect "excess picks ignored" cleanly; each zone is boosted or shielded at
  most once per outcome; shield picks that land on shielded zones are ignored per spec.
- Status lines are per-player and persist until that player's next outcome (previously the
  other player's announcement wiped both lines).
- Win flow, replay listener and every scene-bound reference is null-guarded.
- Removed the 1-second hard-coded waits; pacing is now configurable and turbo-aware.

### Presentation (new)
- Lightweight dependency-free tween engine (colour cross-fades, scale pops, overshoots,
  punch, fading, floating texts, delays) that respects a global turbo time-scale.
- Outcome announcement cards above the acting player's board (icon + label + caption,
  player-colour accent bar).
- Zone VFX: takeover colour tweens, scale pops, expanding rings, selection blinks,
  shield-break shakes, floating "TAKEN! / x5 / SHIELDED" labels.
- Score count-up animation, plate pulses and +/- delta floats over score cards.
- Rounds-left counter pop + tick; outcome-tinted screen flashes; win-screen fade-in with
  scale choreography and confetti.
- Procedural SFX for every event (tick, whoosh, steal zap, boost, shield ping, blank,
  fanfare, draw...) with a pooled mixer and persisted mute.
- Runtime HUD: outcome-odds legend card and SFX/speed buttons, generated in code (the
  scene file stays small and stable).

### Engineering
- Scripts reorganized into `Assets/_Scripts/{Core,Audio,FX,UI,Gameplay}` (GUID-preserving
  moves; scene bindings unchanged).
- New architecture layers: service registry (`App`), runtime UI layer (`RuntimeUi`),
  procedural sprite factory, UI factories (`UiKit`), zone effect helpers (`ZoneFx`).
- Static, null-safe caches replace repeated `FindObjectOfType` calls.
- Removed redundant `ProjectArchive.zip`; assignment brief moved into `Docs/`.
- Full documentation set added (`Docs/`, `README.md`, this changelog).

### Known limitations
- The prototype is an autoplay demo by design (no human input mode yet).
- Win text and HUD copy use Liberation Sans SDF (project font).
- Compiled and statically reviewed against Unity 2021.3/2022.3 APIs; final binary
  verification should run once in the Editor per the QA checklist (`Docs/07-QA-CHECKLIST.md`).

## 1.0.0 - Original prototype

Initial bonus-round autoplay prototype per the assignment brief: 10 rounds, 4 outcomes,
two 4x5 boards, win banner. (Pre-polish state.)
