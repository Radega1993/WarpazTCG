# Sprint 0 — Review

- Date: 2026-09-03
- Editor: Unity **6000.3.20f1** (`c9ba695d4f07`)
- Recommendation: **GO** for Sprint 1

This close audited and corrected the implemented foundation. It did not add product features.

## Result

Sprint 0 is **closed**. Compile is clean, EditMode **60/60**, PlayMode **2/2**, catalog **120/120** with a reproducible hash, Bootstrap opens MainMenu without red errors.

## Tests

| Suite | Total | Passed | Failed | Skipped / ignored |
| --- | ---: | ---: | ---: | ---: |
| EditMode | 60 | 60 | 0 | 0 |
| PlayMode | 2 | 2 | 0 | 0 |
| **Sum** | **62** | **62** | **0** | **0** |

PlayMode `Bootstrap_OpensMainMenu_With120Cards` logged `120/120, 0 errors` and reached MainMenu with 120 catalog entries.

No `[Ignore]` tests exist in `Assets/Warpaz`.

## Errors and warnings

| Source | Count | Notes |
| --- | ---: | --- |
| Compile errors (project) | 0 | Console after compile: idle, no CS errors |
| Failed tests | 0 | |
| Project CS warnings | 0 | Obsolete `TMP_Text.enableWordWrapping` in `WarpazShellSceneBuilder` was fixed (`textWrappingMode`) |
| Package / tooling warnings | 1 observed | `MCP-FOR-UNITY` WebSocket “not initialised” — `com.coplaydev.unity-mcp`, not WARPAZ code |

MCP Play Mode console after Bootstrap → MainMenu: **0 errors, 0 warnings**. Catalog load was confirmed in the PlayMode test output and by `AppCompositionRoot.Runtime.Catalog.Count == 120`.

## Content

| Check | Result |
| --- | --- |
| Card count | 120 |
| IDs | WZ1-001 … WZ1-120, correlative, unique |
| Duplicates | 0 |
| Identities (Héroe + Clase) | 17 (11 + 6) |
| Main-deck cards | 103 |
| Types | Personaje 60, Acción 20, Equipo 13, Héroe 11, Reacción 10, Clase 6 |
| Catalog SHA-256 | `12c7ffece2850f50cf8682909ff024b41935219d82c5254d68d93145be135029` matches `content_manifest.v0.1.json` |
| Source workbook | `Docs/Source/warpaz_tcg_base_set_wz1_v0_1.xlsx` — not edited by tooling (SHA-256 `0a2e5f7eca1d0071156c591122e11b238872ec7d4b31e5c52fdd9b19022586be`) |
| Source spec | `Docs/Source/warpaz_tcg_unity_alpha_especificacion_v0_1.docx` (SHA-256 `d11e6eb91d9dbceeb2d9b1cf8c06de7e841388314e45780f7be9d2d199bd8c4b`) |

Build settings (index 0 = Bootstrap):

0. `Assets/Warpaz/Presentation/Scenes/Bootstrap.unity`
1. `MainMenu.unity`
2. `DeckBuilder.unity`
3. `Battle.unity`
4. `Laboratory.unity`

## Architecture checks

| Check | Verdict |
| --- | --- |
| `Warpaz.Domain` has no `UnityEngine` | **PASS** (`noEngineReferences: true`, assembly test) |
| `Warpaz.Application` has no `UnityEngine` | **PASS** (same) |
| `Warpaz.Content` / `Warpaz.Infrastructure` have no `UnityEngine` | **PASS** (hardened this close; tests added) |
| Circular assembly references | **PASS** (Domain ← Application, Content, Infrastructure; Presentation → all; Content does not reference Infrastructure) |
| Presentation does not decide legality | **PASS** (menus navigate; `MatchDebugHud` submits `Command`s) |
| No MonoBehaviour per card | **PASS** |
| Names are not catalog keys | **PASS** (index by `CardId` only) |
| No `UnityEngine.Random` in Domain | **PASS** (`SeededRng` / `IDeterministicRandom`) |
| No network, backend, accounts, store, missions, locations | **PASS** (`CardZone` “location” is a pile, not a board Location) |

## Files created (this close)

- `Docs/Sprints/SPRINT-0-REVIEW.md`
- `Docs/Sprints/SPRINT-0-RETROSPECTIVE.md`
- `scripts/test-editmode.sh`
- `scripts/test-playmode.sh`

## Files modified (this close)

- `.gitignore` — ignore `TestResults/`
- `Assets/Warpaz/Content/Warpaz.Content.asmdef` — `noEngineReferences: true`
- `Assets/Warpaz/Infrastructure/Warpaz.Infrastructure.asmdef` — `noEngineReferences: true`
- `Assets/Warpaz/Tests/EditMode/Warpaz.Tests.EditMode.asmdef` — reference Infrastructure
- `Assets/Warpaz/Tests/EditMode/DomainAssemblyTests.cs` — cover Application, Content, Infrastructure
- `Assets/Warpaz/Presentation/Editor/WarpazShellSceneBuilder.cs` — TMP wrapping API
- `Assets/Warpaz/Tests/PlayMode/BootstrapFlowTests.cs` — `LogAssert.NoUnexpectedReceived()`

## Sprint 0 inventory (already implemented, not created in this close)

Layers under `Assets/Warpaz/` (Domain, Application, Content, Infrastructure, Presentation), WZ1 JSON in StreamingAssets, catalog loader/validator, RuleConfig loader, uGUI menus, TMP essentials under `Assets/TextMesh Pro/`, editor menu `WARPAZ → Content → Validate WZ1`, `Tools/ContentPipeline/export_wz1.py`.

## Decisions

1. Editor frozen at **6000.3.20f1** (ADR 0001).
2. Cards keyed only by `cardId`; Excel stays editorial (ADR 002).
3. Official Unity Newtonsoft package for catalog/RuleConfig JSON — no homemade parser.
4. Composition root injects `AppRuntime`; no global service locator.
5. uGUI + TextMeshPro; Input System UI module.
6. Content hash is SHA-256 of the exact catalog file bytes.
7. `RuleConfig` JSON `contentVersion` `0.1.0` is the **rules** stamp; catalog content version is `WZ1-v0.1`. They are not merged.

## Technical debt

- `MatchDebugHud` + `Assets/Scenes/SampleScene.unity` remain in the project but are **not** in the build list. Sprint 1 table UI should replace or isolate this HUD.
- Jugar opens a Battle **stub**; it does not start a `GameSession`.
- Mazos / Cartas / Ajustes are “Disponible próximamente”.
- `effects` stay empty; `effectText` is stored, not interpreted (intentional).
- `Utf8Files` is unused by the catalog loader (Content reads files directly).
- `MainMenuController.SimulatePlay` / `SimulateComingSoon` exist as PlayMode hooks.
- `Assets/Warpaz/Presentation/Captures/` holds verification screenshots, not product art.
- Repository has no commits yet; provenance of source files is the SHA-256 values above.

## Definition of Done — Sprint 0

| # | Criterion | Status |
| ---: | --- | --- |
| 1 | Unity 6000.3.20f1, no silent editor upgrade | **PASS** |
| 2 | Layered assemblies; Domain/Application/Content/Infrastructure without `UnityEngine` | **PASS** |
| 3 | No circular assembly dependencies | **PASS** |
| 4 | Presentation does not implement TCG legality | **PASS** |
| 5 | No MonoBehaviour-per-card; no name keys | **PASS** |
| 6 | No `UnityEngine.Random` in Domain; RNG is seeded | **PASS** |
| 7 | Out of scope: network, backend, accounts, store, missions, locations | **PASS** |
| 8 | WZ1: 120 unique correlative IDs, 17 identities, 103 main-deck | **PASS** |
| 9 | Reproducible catalog hash; source Excel/spec not edited | **PASS** |
| 10 | Bootstrap (build index 0) validates content and opens MainMenu | **PASS** |
| 11 | EditMode tests all pass; none ignored | **PASS** (60/60) |
| 12 | PlayMode tests all pass; none ignored | **PASS** (2/2) |
| 13 | MCP console: no project compile errors; no red Play Mode errors | **PASS** |
| 14 | Project compiler warnings addressed; package warnings labelled | **PASS** |
| 15 | Repeatable test scripts with `WARPAZ_UNITY_EDITOR`; `TestResults/` gitignored | **PASS** |

## Recommendation

**GO** for Sprint 1.

The foundation is playable as a shell (Bootstrap → MainMenu), the catalog is valid, and the command-driven engine already has a turn/mulligan slice under test. Sprint 1 should attach Battle to `GameSession` and retire the SampleScene debug HUD, not reopen architecture or WZ1 identity.
