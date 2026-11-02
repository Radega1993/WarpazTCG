# WARPAZ TCG — S2 UI Layout Repair 01

## Verdict

**`UI_LAYOUT_REPAIR_PASS`**

Sprint 2 remains open. Interception remains unvalidated (P-HeroReach external block).

## Confirmed root cause

`BattleCompositionRoot` and most Battle HUD hosts were serialized as plain `Transform` (not `RectTransform`). View `Awake` blocks that cast `transform as RectTransform` became no-ops, so children stacked at the origin (global center collapse). `CardPlaceholderView` had TMP-only chrome (no grey `Image`). `HandView` did not receive/paint hand selection. Legacy `Background/Panel` stub deactivated. Capture harness now waits frames + `ForceUpdateCanvases` + SHA-256 distinctness checks.

## Scope

Presentation-only repair (`BattleLayoutBuilder`, card chrome, HUD/hand/board zoning, Editor/PlayMode harness). Domain/Application not modified by this repair. `S2_UI_VALIDATION_v1` preserved.

## Tests

| Suite | Total | Passed | Failed |
| --- | --- | --- | --- |
| EditMode | 167 | 167 | 0 |
| PlayMode | 13 | 13 | 0 |

Compilation errors: 0. Runtime errors: 0. Missing Scripts: 0.

## Layout

Rival HUD/hand/board/piles top; local board/hand/HUD/piles/actions bottom; center lane for phase/priority/message. Eight board slots (4+4) with grey face-up plates; rival hand face-down; deck/discard face-down with public counts.

## Captures

1920 folder: 01–10. 1600/1280 folders: 01–03 each. Contact sheet: `00_CONTACT_SHEET.png`.

Note: pixel dimensions from Editor GameView were (1600, 900) for captures (SetResolution does not resize GameView); layout tests assert normalized anchors at requested resolutions.

## Duplicate captures

Key sequential SHA-256 pairs distinct: `True`. Capture harness hashes unique: `10/10`.

## External blocks (not repair defects)

- PendingDecision interception unreachable under P-HeroReach (Hero iff 0 Vanguard; interceptor requires Ready Vanguard).
- MatchDebugHud intentionally absent from Battle.
- Mulligan is Keep/Replace only (no per-card selection).

## ZIP

See `Artifacts/UIValidation/WARPAZ_S2_UI_LAYOUT_REPAIR_v1.zip`.
