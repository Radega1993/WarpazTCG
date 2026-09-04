# ADR-002 — Data-driven cards

- Status: Accepted
- Date: 2026-09-03
- Spec: v0.1 §5.5–5.6; WZ1 workbook (Cartas, Razas, Clases)

## Context

WZ1 has 120 stable IDs. Names and text will change during balance. Interpreting Spanish `effectText` at runtime is unreliable. One C# class per card would freeze IDs to types and explode merge cost.

## Decision

- The only identity of a card is `cardId` (`WZ1-001` … `WZ1-120`). Names, numbers, and flavour are display fields.
- Those 120 IDs are **immutable**. A provisional name does not get a new ID.
- The Excel workbook is editorial. A reproducible export produces UTF-8 CSV/JSON. Unity loads definitions, not `.xlsx`.
- Effects are declarative `EffectDefinition` / EffectBindings: trigger, costs, query, operations, duration. Operations are reused across cards.
- Cards that cannot be expressed as operations register `ICardEffectHandler` **by `cardId`**. Never look up by name. Never attach a MonoBehaviour per card.

## Consequences

- Catalog validators key on ID: 120 unique values, no duplicates, missing IDs fail the import.
- Replays bind to `contentVersion` + catalog hash, not to printed names.
- New keywords require an operation or an explicit handler, not a new subclass.

## Discarded alternatives

- **One C# script per card.** Forbidden by spec; unmaintainable for 120 cards.
- **Name as dictionary key.** Balance renames would break saves and tests.
- **Parse `effectText` in the engine.** Human text is not a grammar; bindings are.
- **ScriptableObject as the only source of truth.** SOs may cache imported data; the authored source stays the versioned JSON/CSV derived from Excel.
