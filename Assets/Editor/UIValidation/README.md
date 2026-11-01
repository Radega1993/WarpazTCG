# Editor UI Validation (S2-UI-VALIDATION-01)

Editor-only validation helpers for Battle UI audits.

- **Not** included in player/production builds (`includePlatforms: Editor`).
- Does **not** modify Domain, Application, Battle scene, or production prefabs.
- Writes evidence under `Artifacts/UIValidation/` (outside `Assets/`).
- Uses public `GameSession` APIs and existing UI Find-by-name contracts only.
- Must **not** call Domain internals such as `EngineActions.OpenInterceptDecision`.

Menu: `Warpaz/UI Validation/Open Artifacts Folder`
