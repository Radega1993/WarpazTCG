# Sprint 1 — Review

- Date: 2026-09-04
- Editor: Unity **6000.3.20f1**
- Recommendation: **GO** for Sprint 2

This close audited Sprint 1 (match create, mulligan, turn machine, grey table, tests, soak). It did **not** add Sprint 2 product features.

## Result

Sprint 1 is **closed**. EditMode **126/126**, PlayMode **7/7**, soak **100/100**, WZ1 catalog **120/120** with matching manifest hash, Bootstrap→MainMenu→Battle five-turn UI flow with **0** console errors and **0** Missing Scripts on shell scenes.

## Tests

| Suite | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| EditMode | 126 | 126 | 0 | 0 |
| PlayMode | 7 | 7 | 0 | 0 |
| **Sum** | **133** | **133** | **0** | **0** |

Soak (`Sprint1StressRunner`, BaseSeed 1000): 100 started, 100 completed, 0 unexpected rejections, 0 invariant breaks, 0 stalls, identical hashes on second run. Details: [`SPRINT-1-TEST-REPORT.md`](SPRINT-1-TEST-REPORT.md).

## Definition of Done checklist

### Funcional

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| Partida con dos jugadores y dos mazos WZ1 válidos | **PASS** | `Sprint1MatchFixtureTests`, `Wz1CreateMatchTests`, Battle payload |
| Héroe, Clase, 40 cartas, 5 Resolución | **PASS** | Match create + `PlayerState` / RuleConfig InitialResolution=5 |
| Preparación determinista | **PASS** | Same seed → same hash (integration + stress) |
| Keep / ReplaceWholeHand | **PASS** | `MulliganTests`, LegalActions, stress mulligan by seed parity |
| Tras ambos mulligans → Main automático | **PASS** | `TurnPhaseTests`, Battle / stress runners |
| Primer turno propio → 1 PA máx | **PASS** | `TurnPhaseTests` + MCP audit `paMax=1` |
| Segundo turno propio → 2 PA máx | **PASS** | `TurnPhaseTests` + MCP `ownTurn2PaMax=2` |
| Cada turno roba una carta | **PASS** | Draw phase / hand growth across EndTurns |
| Solo activo puede EndTurn | **PASS** | `LegalActionQueryTests` |
| Al menos cinco turnos | **PASS** | Integration, PlayMode, MCP UI flow Turn→6 |
| Mesa gris muestra estado correcto | **PASS** | `MatchStatusView` / PlayMode indicator test |
| Nunca ambas manos a la vez | **PASS** | Projector + `RivalHand_NeverVisible` + pass-device hide |
| Robo fallido retira Resolución | **PASS** | `TurnPhaseTests` / `TurnCycle.RunDraw` |
| Resolución 0 → GameOver | **PASS** | `TurnPhaseTests` ResolutionDepleted |

### Arquitectura

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| Domain sin UnityEngine | **PASS** | asmdef `noEngineReferences` + `DomainAssemblyTests` |
| Application sin Presentation | **PASS** | `Application_DoesNotReferencePresentation` |
| Presentation no decide legalidad | **PASS** | `BattlePresenter` → `LegalActionQuery` |
| Mutación solo por Commands | **PASS** | Command pipeline / GameSession |
| Cambios relevantes → GameEvents | **PASS** | Handlers + event log |
| Rechazo no muta estado ni RNG | **PASS** | `GameSessionTests` / CommandPipeline |
| No UnityEngine.Random | **PASS** | `SeededRng` (comments forbid it; no usages) |
| No scripts por carta | **PASS** | Shared `CardPlaceholderView` |
| IDs únicas claves | **PASS** | CardId catalog / fixtures |
| Sin backend/red/cuentas/economía/Misiones/Ubicaciones | **PASS** | Out of scope; no such modules |

### Contenido

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| 120 cartas | **PASS** | Loader + tests |
| 120 IDs únicos WZ1-001…120 | **PASS** | Catalog tests |
| 17 identidades / 103 mazo | **PASS** | Manifest + filter counts |
| Hash contenido válido | **PASS** | SHA-256 matches manifest |
| XLSX/DOCX fuente no modificados | **PASS** | SHA matches Sprint 0 review |

### Pruebas

| Criterion | Verdict |
| --- | --- |
| EditMode PASS | **PASS** 126/126 |
| PlayMode PASS | **PASS** 7/7 |
| Golden PASS | **PASS** |
| Flujo 5 turnos determinista PASS | **PASS** |
| Soak 100 PASS | **PASS** |
| 0 errores rojos consola (flujo auditado) | **PASS** |
| 0 Missing references (Bootstrap/MainMenu/Battle) | **PASS** |
| Build/Play abre Bootstrap, MainMenu, Battle | **PASS** (build list + MCP Play from Bootstrap → SimulatePlay → Battle) |

### UX

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| 1920×1080 | **PASS** | CanvasScaler ref 1920×1080; menu capture |
| 1366×768 | **PASS** | PlayMode legibility test |
| 16:10 | **PASS** | ScaleWithScreenSize match=0.5 (aspect-safe); no Missing UI |
| Mulligan comprensible | **PASS** | Keep/Replace + “solo una vez” copy |
| Cambio de jugador protege mano | **PASS** | PassDeviceOverlay + hand hide |
| Fase, turno, PA, Resolución visibles | **PASS** | MatchStatusView / PlayerAreaView |
| Rechazo muestra motivo | **PASS** | RejectionText / EventLog UI |

## Implementation summary

Sprint 1 delivered:

- Domain turn engine (mulligan → Main → EndTurn cycle, PA growth, draw, failed-draw Resolution loss, GameOver).
- Application `GameSession`, legal-action query, privacy-aware projectors.
- WZ1 fixture factory for two legal seats without a deck builder.
- Grey Battle uGUI (status, hands, mulligan, pass-device, end turn, event log).
- EditMode integration, golden, soak; PlayMode grey-table and privacy/UX checks.
- Architecture docs: state machine + command/event contract.

## Recommended commits (repo had **no commits** on `master` at close)

Suggested first history (do not auto-commit unless asked):

1. **chore:** Unity project baseline (ProjectSettings, Packages, .gitignore, Tools/scripts)
2. **feat(domain):** Sprint 1 match/turn engine + hashing + invariants
3. **feat(application):** GameSession, projectors, LegalActionQuery
4. **feat(content):** WZ1 loaders, rule config, Sprint1 fixture
5. **feat(presentation):** Bootstrap/MainMenu/Battle grey table
6. **test:** EditMode + PlayMode + golden + soak
7. **docs:** Sprint 1 review / retrospective / test report + architecture state machine & contract
8. **assets:** StreamingAssets catalog/manifest (not Source xlsx edits)

Keep `Docs/Source/*.xlsx|docx` read-only. Keep `Library/`, `Logs/`, `Temp/`, `Builds/`, `TestResults/` ignored. Grey table screenshots live under `Assets/Warpaz/Presentation/Captures/`.

## Errors and warnings

| Source | Count | Notes |
| --- | ---: | --- |
| Compile errors | 0 | |
| Failed tests | 0 | |
| Play Mode console errors (audit flow) | 0 | |
| Missing Scripts (3 scenes) | 0 | |
| Tooling | — | MCP WebSocket noise may appear; not WARPAZ gameplay code |

## Technical debt

- `BattleCompositionRoot._session` accessed via reflection in PlayMode tests.
- Grey layout partially hard-coded in view `Awake` positions (not a full layout system).
- `MatchDebugHud` may still exist alongside the grey table — quarantine/remove in a cleanup commit.
- DeckBuilder / Laboratory scenes remain stubs in build settings.
- No git history yet — risk until first commit baseline.
- Editor Play starts open scene; manual audits must open Bootstrap first (or use PlayMode tests).

## GO / NO-GO

**GO** for Sprint 2.

All P0 Definition-of-Done rows above are **PASS**. No Sprint 2 features were implemented in this close.
