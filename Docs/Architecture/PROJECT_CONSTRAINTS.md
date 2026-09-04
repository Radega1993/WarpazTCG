# WARPAZ TCG — Project constraints

Source of truth for product limits: specification v0.1 (`Docs/Source/warpaz_tcg_unity_alpha_especificacion_v0_1.docx`) and Base Set WZ1 (`Docs/Source/warpaz_tcg_base_set_wz1_v0_1.xlsx`, sheets Resumen, Cartas, Razas, Clases, Glosario). Those files are editorial; do not edit them from tooling.

## Editor

- Frozen editor: **Unity 6000.3.20f1** (see `Docs/decisions/0001-unity-editor-version.md`).
- An early briefing named 6.1.0f1. That pin is discarded: the spec asks for Unity 2D on PC, not a 6.1 patch, and the project already opened on 6.3.20f1.

## Product

- Local alpha on PC: hot-seat, then a legal bot. No backend, accounts, matchmaking, store, packs, or online multiplayer.
- Goal: playtest rules and the 120-card set, not a commercial live-service.
- Unity presents `GameState` and submits `Command`s. The rules engine is C# and does not depend on scenes.

## Content

- Set WZ1: 120 cards, stable IDs **WZ1-001 … WZ1-120** (103 deck cards + 17 identities: 11 racial heroes + 6 class paths).
- `cardId` never changes when a name or text is still provisional.
- No Locations. No Missions that alter matches.
- Dice are not part of the base system. They may run only when a card effect orders them.
- Excel remains editorial. Runtime catalog is derived UTF-8 (CSV/JSON), not the workbook inside `Assets/`.

## Architecture (must hold)

- Domain is pure C# (no `UnityEngine`).
- Presentation never decides legality.
- State changes only via `Command`. Observable outcomes only via `GameEvent`.
- Names are never lookup keys.
- Effects are data. No C# type or MonoBehaviour per card.
- No unseeded randomness. No `UnityEngine.Random` in Domain.
- Every feature ships with tests. Compilation errors block the next slice.

## Explicitly out of alpha

Campaign/RPG world, alternative victory conditions tied to locations, 3D cinematics, final art for all 120 cards, competitive AI, NestJS/WebSocket, economy.
