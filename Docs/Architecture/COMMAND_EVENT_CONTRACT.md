# Sprint 1 — Command / Event Contract

- Date: 2026-09-04
- Scope: Commands and events legal in Sprint 1 (no Play / Attack / Equip)

## Principles

1. **All mutations** go through `Command` → `RulesEngine` / `CommandProcessor`.
2. **Rejected commands** return `CommandResult.Accepted == false`, leave `GameState` and RNG unchanged, and emit no new events.
3. **Accepted commands** may emit one or more `GameEvent`s; `EventIndex` equals `Events.Count`.
4. Presentation submits commands and reads `LegalActionQuery` / view projectors. It does not re-implement legality.
5. Card identity is always `CardId` (never display name).

## Commands (Sprint 1)

| CommandType | Actor | Expected phase | Payload |
| --- | --- | --- | --- |
| `CreateMatch` | Host | (null / new) | `MatchSetup` (seed, rules, contentVersion, two `PlayerSetup`) |
| `ConfirmMulligan` | Seat matching MulliganP0/P1 | MulliganP0 or MulliganP1 | `MulliganDecision` KeepHand \| ReplaceWholeHand |
| `EndTurn` | `ActivePlayerId` | Main | none |

Other `CommandType` values exist in the enum for later sprints and reject as unsupported / wrong phase in Sprint 1.

### Rejection (must not mutate)

Typical codes: `WrongActor`, `InvalidPhase`, `StaleState` (`expectedEventIndex`), `DuplicateCommand`, `MatchAlreadyFinished`, `CommandNotSupported`.

Application: `GameSession.Submit` / `ConfirmMulligan` / `EndTurn` only replace `State` when `result.Accepted`.

## Events (observable)

Public vs private:

| Visibility | Rule |
| --- | --- |
| `Public` | Safe for both players’ logs / UI |
| `OwnerOnly` | Filtered by `GameStateViewProjector` / `EventLogProjector` for non-owners (e.g. `CardDrawn` contents) |

Relevant Sprint 1 types include: match/setup/mulligan lifecycle, `TurnStarted`, `TurnEnded`, `ActivePlayerChanged`, `PreparationCompleted`, `PaMaxIncreased`, `PaRefilled`, `CardDrawn`, `DrawFailed`, `ResolutionRemoved`, `MatchEnded`.

RNG internal state is **never** part of the player-facing view or the canonical hash document.

## LegalActionQuery

`LegalActions.For(GameState)` (Domain) → `LegalActionQuery.For` (Application):

- Mulligan seats: KeepHand + ReplaceWholeHand for the mulligan actor.
- Main: single `EndTurn` for the active player.
- GameOver / other: empty list.

UI enables buttons only from this list.

## State hash

`GameStateHasher.Hash(state)` = SHA-256 (lowercase hex) of the canonical UTF-8 document (`CanonicalGameStateSerializer`). Same seed + same accepted command stream ⇒ same hash. Used by golden tests and the soak report.

## Application façade

`GameSession` owns current `GameState`, allocates command ids, exposes `Start`, `Submit`, `ConfirmMulligan`, `EndTurn`, `Project`, `LegalActions`, `CurrentHash`. It is **not** a MonoBehaviour.
