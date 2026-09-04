# Sprint 1 — Retrospective

- Date: 2026-09-04
- Closed with: **GO** for Sprint 2 (see SPRINT-1-REVIEW.md)

## What went well

- Layering held: Domain/Application stayed `noEngineReferences`; UI submits commands and reads `LegalActionQuery`.
- Determinism is enforceable: golden hash, dual-run stress hashes, and canonical `GameStateHasher`.
- Grey battle table is enough to exercise mulligan, pass-device, and five EndTurns without card play.
- Soak (100 matches × 20 EndTurns) caught a runner bug (global vs per-match command cap) before it looked like an engine failure.
- Shared `Sprint1StateAssertions` kept integration and stress checks aligned.

## What was painful

- Unity MCP scene editing for dense uGUI remains brittle; layout ended up partly in view `Awake` positioning.
- PlayMode tests needed inactive-object searches for `PassDeviceOverlay` and careful waits on observable phase, not fixed sleeps.
- Editor Play Mode starts the **open** scene; Bootstrap-as-index-0 does not override that unless the open scene is Bootstrap (PlayMode tests load Bootstrap explicitly).
- `Screen.SetResolution` in the Editor does not change reported `Screen.width/height` (Game View owns size), which complicates resolution audits.

## What we would do differently

- Expose a tiny test seam on `BattleCompositionRoot` for `GameSession` instead of reflecting `_session`.
- Add `TestResults/` hygiene was already in `.gitignore`; keep root screenshots out of the repo root (moved under `Captures/`).
- Document MulliganP0/P1 ↔ “P1/P2” naming once in STATE_MACHINE and stop re-litigating it in prompts.
- Consider an Editor “Play From Bootstrap” menu item so manual audits do not depend on which scene is open.

## Keep / drop

| Keep | Drop / postpone |
| --- | --- |
| Command + event pipeline | UI legality conditionals |
| Fixture decks for Sprint 1 | Full deck builder (Sprint later) |
| Pass-device overlay (hotseat) | Network / accounts |
| Soak as EditMode runner | Heuristic bot (Sprint 6) |
| Declarative empty effects | Per-card MonoBehaviours / interpreting effectText |

## Actions for Sprint 2

1. Introduce the first playable card command(s) behind Domain handlers + LegalActions — not Presentation ifs.
2. Replace grey placeholders with real card presentation data still keyed by `cardId`.
3. Keep soak/golden green when the turn pipeline gains new automatic steps.
4. First commit baseline on `master` (repo still had no commits at Sprint 1 close).
