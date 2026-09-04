# ADR 0001 — Unity editor version for the alpha

- Status: Accepted
- Date: 2026-09-03

## Decision

The WARPAZ TCG alpha is frozen on **Unity 6000.3.20f1** (revision `c9ba695d4f07`).

The project was created and opened with this editor. A downgrade to 6.1.0f1 is not required: specification v0.1 asks for Unity 2D on PC, not a specific 6.1 patch.

## Consequences

- All developers and CI must use `6000.3.20f1`.
- `ProjectSettings/ProjectVersion.txt` is the source of truth.
- Do not upgrade the editor during the alpha without a new ADR.
