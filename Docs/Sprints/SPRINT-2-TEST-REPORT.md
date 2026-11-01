# Sprint 2 — Test report

## Results (2026-09-04)

| Suite | Result |
| --- | --- |
| EditMode | **150/150** PASS |
| PlayMode | **8/8** PASS |

Sprint 1 golden + EndTurn soak remain green; Sprint 2 adds board/play/reorg/attack/combat/golden/soak + PlayMode board combat.

## Suites

| Suite | Coverage |
| --- | --- |
| EditMode (S1 regression) | Golden S1, stress EndTurn-only, mulligan, create match, domain assembly |
| EditMode (S2) | Board invariants, Play/Replace, Reorg, Attack/Combat/Intercept, Preparation Recent, Golden S2, Stress S2 |
| PlayMode | Bootstrap→Battle board projection; session Play when Character affordable; prior S1 UI flows |

## Policies under test

P-HeroReach, P-AttackRow, P-HeroTie, P-RecentClear, P-ReplaceSlot, P-InterceptWindow (no reaction window).

## Notes

- Under P-HeroReach, Hero-target attacks occur only with empty enemy Vanguard, so Guardián-less intercept windows are rare in soak; intercept handlers remain covered by EditMode unit tests.
- Sprint 1 golden hash must remain unchanged (no board occupants in that fixture).
