# ADR 0002 — Sprint 1 turn engine: PA, RNG, mulligan, turn count

- Status: Accepted
- Date: 2026-09-03

## Context

Specification v0.1 §3.3 says PA maximum grows by 1 at the start of each own turn, up to 8, then current PA refills to maximum. `RuleConfig.StartingPaMax` is 1. Sprint 1 also needs a deterministic RNG and a definition of “five turns”.

## Decision

### PA

- After `CreateMatch`, both players have `paMax = 0` and `paCurrent = 0`.
- PA phase of the active player: `paMax = min(MaxPa, paMax + PaGainPerTurn)`, then `paCurrent = paMax`.
- The first own turn therefore yields 1 PA. The fifth own turn yields 5 PA. `paMax` never exceeds `RuleConfig.MaxPa` (8).
- Only the active player gains PA. The inactive player stays at their last values (0/0 until their first own turn).

### RNG

- Domain uses SplitMix64 plus Fisher–Yates. Not `UnityEngine.Random` and not `System.Random`.
- Setup stream order: instantiate player 0 then player 1 (hero, class, deck list order) → shuffle player 0 → shuffle player 1 → `firstPlayerId = rng.Next(0, 2)` → draw opening hands player 0 then player 1.

### Mulligan

- Phases `MulliganP0` then `MulliganP1` follow `playerId`, not first-player order. Prompt wording “MulliganP1/P2” maps to these seats (`Player0` then `Player1`). Prompt “Player 2” is seat `Player1`.
- Keep the whole hand (`MulliganDecision.KeepHand`) or return the whole hand (`ReplaceWholeHand`). Returned cards are shuffled into the deck, then `InitialHandSize` is drawn. Instance ids are preserved. Public events never include the new hand’s instance ids.
- After `ConfirmMulligan` for `Player1`, the engine starts the turn cycle with `FirstPlayerId` (chosen at `CreateMatch`).

### Turn index and phases

- `GameState.Turn` is the **global** turn index, not a per-player count. It is not a `MatchPhase`.
- `StartTurn` is not a `MatchPhase`. The engine emits `TurnStarted` and then walks `Preparation` → `Pa` → `Draw` → `Main`. `EndTurn` in Main emits `TurnEnded`, enters `Closing`, and starts the opponent’s turn. `GameOver` stops the cycle.
- Turn 1 is the first `BeginTurn` after both mulligans. After **five accepted `EndTurn` commands**, `Turn == 6` because the sixth `BeginTurn` has already run. That is the Sprint 1 meaning of “five turns”.
- Automatic phases run in `TurnCycle.AdvanceUntilDecision` (no coroutines / `Update`). It stops on `Main`, `PendingChoice`, `ReactionWindow`, or `GameOver`. Each automatic phase counts 1 against `RuleConfig.MaximumEventsPerTurn` (256). Exceeding the cap throws `InvariantViolationException` as a diagnostic, not a command rejection.
- Sprint 1 Preparation does not ready Characters. It emits `PreparationCompleted` and continues.
- Draw is one OwnerOnly card. An empty deck emits `DrawFailed` then `ResolutionRemoved` (`FailedDrawResolutionLoss`, clamped at 0). If the active player’s Resolution is then 0, the match ends (`MatchEndCause.ResolutionDepleted`, winner = opponent) and does **not** enter Main or start the opponent’s turn.
- Closing resets `FreeReorganizationUsed` on the player whose turn just ended.

### Event names

Existing names are kept; they are not duplicated under prompt aliases.

| Prompt / spec | Engine |
| --- | --- |
| PAMaxIncreased | `PaMaxIncreased` |
| PA refill | `PaRefilled` |
| ResolutionLost | `ResolutionRemoved` |
| PreparationCompleted | `PreparationCompleted` (added) |
| DrawFailed | `DrawFailed` (added) |
| ActivePlayerChanged | `ActivePlayerChanged` (added; emitted when the active seat actually changes) |
| CardDrawn | `CardDrawn` (OwnerOnly) |

### Hash

- Canonical UTF-8 document (seed, versions, turn, phase, active/priority player, each player’s identities, PA, Resolution, zone instance-id lists in order) hashed with SHA-256, lowercase hex.
- RNG internal state is not part of the document; identical command streams still converge.

## Consequences

- Tests assert PA 1..5 across a player’s first five own turns, not 2..6.
- Tests that play five `EndTurn`s after mulligan assert `Turn == 6`.
- Replays in later sprints must use this SplitMix64 implementation.
