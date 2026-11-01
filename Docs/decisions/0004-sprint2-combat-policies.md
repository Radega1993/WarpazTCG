# ADR 0004 — Sprint 2 combat policies (vanilla)

- Status: **Accepted** (Sprint 2 defaults)
- Date: 2026-09-04
- Spec: v0.1 §3.4–3.7 (policies explicitly deferred by the spec)

## Context

Spec §3.6 states that the exact relationship between attacking the Hero, ties, and Resolution loss must be implemented as **engine policies** so playtests can adjust without rewriting cards or UI. Sprint 2 implements vanilla Character combat only (no A distancia, Guardián keyword, Respuesta, or effect text).

## Decision

| ID | Policy |
| --- | --- |
| **P-HeroReach** | The defending Hero is a legal attack target **iff** the defender has **zero** Characters in Vanguard. If any Vanguard Character exists, only those Characters are legal targets for a normal attacker. |
| **P-AttackRow** | A vanilla Character may declare an attack only while in **Vanguard**, Ready, and without `Recent`. |
| **P-HeroPower** | Hero combat uses `CardDefinition.Power` of the Hero identity (missing/null treated as `0` only if catalog lacks a value). |
| **P-HeroTie** | On Power **tie** vs Hero: attacker is K.O. **and** defender loses **1** Resolution. |
| **P-HeroWin** | If Hero Power is strictly greater: attacker is K.O.; Resolution unchanged. |
| **P-HeroLose** | If attacker Power is strictly greater: Hero does not go to discard; defender loses **1** Resolution; attacker remains (already Exhausted from declaring). |
| **P-RecentClear** | During **Preparation** of the active player: Ready all Exhausted Characters they control on board, then remove `Recent` from those Characters. |
| **P-ReplaceSlot** | Full-board substitution: `Play` with `replaceInstanceId` sends that unit to Discard (not Sacrifice / no Last Breath) and occupies **the same slot**. |
| **P-InterceptWindow** | No modification / Respuesta / Reaction window in Sprint 2. After ChooseInterceptor or Decline, combat resolves immediately. |
| **P-Interceptors** | Sprint 2 interceptors: Ready Characters in defender **Vanguard** only (Guardián from Rearguard is out of scope). `Recent` **may** intercept. Intercepting Exhausts the interceptor. |

Character vs Character: lower Power is K.O.; tie → both K.O. (spec §3.6).

## Consequences

- Policies live in Domain (`CombatPolicies` / RuleConfig knobs later if needed). Changing them requires an ADR amendment and golden/soak updates.
- Sprint 2.5+ combat code must call these policies; Presentation must not reimplement them.
