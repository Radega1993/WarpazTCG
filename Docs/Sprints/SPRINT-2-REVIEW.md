# Sprint 2 — Review (Combate vanilla)

- Status: **Complete** (S2.0–S2.9)
- Date: 2026-09-04
- Policies: [ADR 0004](../decisions/0004-sprint2-combat-policies.md) (Accepted)

## Scope delivered

- Board placement helpers + slot invariants
- `Command.Play` / replace + PA + Recent
- Preparation: Ready + clear Recent (P-RecentClear)
- Free reorganize (move/swap)
- Attack matrix (P-AttackRow, P-HeroReach)
- PendingDecision interception (ChooseTargets / PassPriority)
- Combat resolve: Character KO, Hero Resolution, GameOver path
- Battle UI: board + hand clicks via LegalActions / session Commands
- Golden S2 + soak S2; Sprint 1 golden left pinned separately

## Out of scope (unchanged)

Locations, Missions, keywords Guardián / A distancia, effect text, Respuesta window, bot, network.

## Recommendation

**GO** for playtesting vanilla combat. Policy knobs remain in ADR 0004 if Hero reach/tie need tuning.
