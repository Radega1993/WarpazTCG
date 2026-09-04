# Sprint 1 — Test Report

- Date: 2026-09-04
- Editor: Unity **6000.3.20f1**
- Method: Unity MCP `run_tests` + `execute_code` soak + Play Mode Bootstrap→Battle manual flow

## Summary totals

| Suite | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| EditMode | 126 | 126 | 0 | 0 |
| PlayMode | 7 | 7 | 0 | 0 |
| **Sum** | **133** | **133** | **0** | **0** |

No `[Ignore]` tests under `Assets/Warpaz/Tests`.

## EditMode highlights

| Area | Result |
| --- | --- |
| WZ1 catalog / manifest hash | PASS (`Wz1CatalogTests`) |
| CreateMatch + fixture seats | PASS |
| Mulligan keep/replace | PASS |
| Turn / PA / draw / Resolution0 GameOver | PASS (`TurnPhaseTests`) |
| LegalActionQuery | PASS |
| View privacy (rival hand) | PASS (`MatchViewProjectorTests`) |
| Domain/Application assembly boundaries | PASS (`DomainAssemblyTests`) |
| Golden seed=42 | PASS (`Sprint1GoldenTest`) |
| Integration invariants + determinism | PASS (`Sprint1IntegrationTests`) |
| Soak 100 matches | PASS (`Sprint1StressTests`) |

## PlayMode

| Test | Result |
| --- | --- |
| `Bootstrap_OpensMainMenu_With120Cards` | PASS |
| `GameSession_CompletesFiveTurnsWithoutHud` | PASS |
| `GreyBattleTable_FullFlowToTurn5` | PASS |
| `RivalHand_NeverVisible_DuringFullFlow` | PASS |
| `PlayerAndPhaseIndicator_MatchGameState_DuringFiveTurns` | PASS |
| `IllegalButtons_AreDisabled_InWrongPhase` | PASS |
| `UI_IsLegible_AtMinimumResolution` (1366×768) | PASS |

## Soak (100 matches)

Config: `BaseSeed=1000`, `MatchCount=100`, `MaxTurns=20`, mulligan `(seed & 1)`, abort on first failure.

| Metric | Value |
| --- | ---: |
| MatchesStarted | 100 |
| MatchesCompleted | 100 |
| CommandsAccepted | 2300 |
| UnexpectedRejections | 0 |
| InvariantBreaks | 0 |
| Stalls | 0 |
| FirstFailedSeed | none |
| ElapsedMilliseconds | ~5666 (wall clock; not part of hash) |
| Second run identical hashes | True |

Soak is an engine resistance check, **not** a balance report.

## Golden fixture

| Field | Value |
| --- | --- |
| Seed | 42 |
| contentVersion | WZ1-v0.1 |
| ruleVersion | 0.1.0 |
| Final hash | `64dd38329bc1485db3a05380afc4f5d52d1adcc5aacfde72b49c78abca081ff4` |
| Spec file | `Assets/StreamingAssets/Tests/Sprint1GoldenFixture.json` |

## Content validation

| Check | Result |
| --- | --- |
| Card count | 120 |
| IDs | WZ1-001 … WZ1-120 |
| Identities | 17 (11 Héroe + 6 Clase) |
| Main-deck cards | 103 |
| Catalog SHA-256 | `12c7ffece2850f50cf8682909ff024b41935219d82c5254d68d93145be135029` matches manifest |
| Source XLSX SHA-256 | `0a2e5f7eca1d0071156c591122e11b238872ec7d4b31e5c52fdd9b19022586be` (unchanged vs Sprint 0) |
| Source DOCX SHA-256 | `d11e6eb91d9dbceeb2d9b1cf8c06de7e841388314e45780f7be9d2d199bd8c4b` (unchanged vs Sprint 0) |

## Console / scenes

| Check | Result |
| --- | --- |
| Project compile errors (post-tests) | 0 |
| Play Mode Bootstrap→MainMenu→Battle→5 EndTurns console errors | 0 |
| Play Mode warnings (WARPAZ) | 0 observed |
| Missing Script on Bootstrap / MainMenu / Battle | 0 |
| Build settings index 0 | Bootstrap → MainMenu → DeckBuilder → Battle → Laboratory |

## UX resolution evidence

| Resolution | Evidence |
| --- | --- |
| 1920×1080 | Battle `CanvasScaler` referenceResolution (1920,1080); capture `mainmenu-1920x1080.png` |
| 1366×768 | PlayMode `UI_IsLegible_AtMinimumResolution` PASS; capture `mainmenu-1366x768.png` |
| 16:10 (e.g. 1680×1050) | `CanvasScaler` ScaleWithScreenSize, matchWidthOrHeight=0.5 (aspect-safe); editor `Screen` stays at Game View size so SetResolution is not authoritative in Edit Mode |

## Captures

- `Assets/Warpaz/Presentation/Captures/GreyBattleTable_Mulligan.png`
- `Assets/Warpaz/Presentation/Captures/GreyBattleTable_Main.png`
