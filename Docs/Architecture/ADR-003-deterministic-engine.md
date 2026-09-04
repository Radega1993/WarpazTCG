# ADR-003 — Deterministic engine

- Status: Accepted
- Date: 2026-09-03
- Spec: v0.1 §1.2, §3.1, §5.7
- Runtime pin: `Docs/decisions/0002-sprint1-turn-engine.md`

## Context

Playtests must reproduce bugs from seed + command list + `GameState` hash. Unity’s RNG and unseeded `System.Random` are not stable across editor versions. Dice in the base rules would add hidden entropy.

## Decision

- Every random choice consumes a **recorded** match seed through Domain SplitMix64 + Fisher–Yates. Same seed + same accepted commands → same final hash.
- Domain must not call `UnityEngine.Random` or unseeded `System.Random`.
- First player, shuffles, and future coin-flips go through that stream. Stream order for setup is defined in decision 0002.
- `GameState` hashes a canonical UTF-8 document (SHA-256, lowercase hex). See decision 0002 for the field list.
- Dice are **not** a base system. They execute only as an explicit card operation (seeded). There are no Locations or Missions that inject extra RNG into a match.

## Consequences

- Replays are the accepted command log plus versions (`ruleVersion`, `contentVersion`, seed).
- A command with a stale `expectedEventIndex` is rejected and must not mutate state.
- Presentation may animate; animation must not call Domain RNG or block engine resolution.

## Discarded alternatives

- **`UnityEngine.Random` in Domain.** Non-portable; depends on engine internals.
- **Dice as a turn/setup ritual.** Spec forbids visible rolls as part of the core game.
- **Replay from snapshots only, without seed.** Cannot regenerate hidden zone order.
- **Hashing RNG internal state.** Spec hashes visible `GameState`; identical command streams already converge.
