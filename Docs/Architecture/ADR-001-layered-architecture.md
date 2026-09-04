# ADR-001 — Layered architecture

- Status: Accepted
- Date: 2026-09-03
- Spec: v0.1 §5.1–5.2

## Context

WARPAZ must be a deterministic TCG engine that tests can drive without a scene, while Unity still shows a table and accepts input. If rules live in MonoBehaviours, hashes, replays, and bots inherit animation and frame order.

## Decision

Five assemblies under `Assets/Warpaz/`:

| Layer | Responsibility | Must not know |
| --- | --- | --- |
| Domain | `GameState`, rules, `Command`, `GameEvent`, legal targets, hashes | `UnityEngine`, scenes, sprites, files |
| Application | Match session, bot, replay, telemetry | How something looks |
| Content | `CardDefinition`, `RuleConfig`, importer, validators | Mutable match state |
| Infrastructure | JSON/CSV, paths, logs | Combat decisions |
| Presentation | Views, input, animation, audio | Rule implementation |

Rules:

- The UI never decides if an action is legal. It submits a `Command` and shows `CommandResult`.
- Only an accepted `Command` may mutate `GameState`.
- Every observable engine result is a `GameEvent` (or a rejection reason that does not mutate).

## Consequences

- Domain tests run in EditMode without Play Mode.
- Presentation can be replaced (debug HUD → table) without rewriting rules.
- Cross-layer leaks (e.g. `UnityEngine.Random` in Domain) are defects, not style nits.

## Discarded alternatives

- **Logic on MonoBehaviours.** Couples rules to the player loop; blocks headless determinism.
- **Visual Scripting / Timeline for rules.** Graph assets are not a replayable command log.
- **Single assembly.** Would let Presentation call Domain internals and skip the command boundary.
- **Domain referencing UnityEngine “just for Vector2.”** The spec forbids it; keep math in POCOs.
