# Application

Use cases: `GameSession`, `CreateMatchUseCase`, `SubmitCommandUseCase`, later bot, replay, telemetry.

**May reference:** `Warpaz.Domain` only. No `UnityEngine` (`noEngineReferences`).

**Must not contain:** visuals, MonoBehaviours, or legality checks that belong in Domain.

## Sprint 1 session

- `GameSession` owns the current `GameState` (`get; private set;`). Rejected commands do not replace it. There is no disk save and no replay player; `AcceptedCommands` is only an in-memory log.
- Create and submit go through the use cases, which call Domain `RulesEngine.Submit`. Hash is `GameSession.CurrentHash`.
- `LegalActionQuery` forwards `LegalActions.For` from Domain. Presentation must not re-encode phase conditionals.

Prompt names vs seats (same as ADR 0002):

- MulliganP1 / Player 1 → `MatchPhase.MulliganP0` / `PlayerId.Player0`
- MulliganP2 / Player 2 → `MatchPhase.MulliganP1` / `PlayerId.Player1`

## Views

- `GameStateViewProjector` / `EventLogProjector` build `MatchView` for a `viewerPlayerId`.
- Default `MatchViewMode.Normal`: own hand, opponent `handCount`, public discard, no deck order, no RNG, OwnerOnly events filtered with `GameEvent.IsVisibleTo`.
- `MatchViewMode.Debug` must be passed explicitly. The view is tagged `IsDebug` and `Debug` is non-null.
