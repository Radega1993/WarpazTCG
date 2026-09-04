# ADR 0003 — Command pipeline: rejection codes and duplicate commandId

- Status: Accepted
- Date: 2026-09-03
- Spec: v0.1 §5.3, §5.7

## Context

Every player action must enter as a `Command`. S1.1 already had `Command`, `GameEvent`, `CommandResult`, and `RulesEngine.Submit`. This decision records the shared gates and the duplicate-`commandId` policy so Application and Presentation never invent a second protocol.

## Decision

- `CommandProcessor` is the Domain gate. `RulesEngine.Submit` delegates to it. Handlers implement `ICommandHandler`.
- Rejected commands do not mutate `GameState`, `eventIndex`, RNG, or the event log. They return `CommandRejectionCode` plus a stable message. Player errors are not exceptions.
- Accepted commands emit one or more `GameEvent`s with dense indexes and a `StateHash`.
- **Duplicate `commandId` is rejected with `DuplicateCommand`.** It is not idempotent: the engine does not replay the cached result. A corrupt or retried log must not silently skip a second apply.
- Accepted ids live on `GameState` but **outside** the canonical hash (ADR 0002). Two equivalent matches with different command id strings still hash equal.

### Deviations from the slice prompt

1. `CreateMatch` remains a legal host command. Without it there is no match.
2. `ConfirmMulligan` and `EndTurn` keep the S1 behaviour already covered by tests. This slice wraps them; it does not stub them.
3. Payload stays typed (`MatchSetup`, `KeepHand`). There is no parallel `object Payload`.
4. There is no `GameState` clone. Atomicity is validate-then-mutate. `InvariantViolationException` remains a programming error.

## Consequences

- Tests assert `Code` as well as message text.
- Replay must assign unique `commandId`s per accepted command.
- `Play` / `Attack` / other types stay `CommandNotSupported` until later slices.
