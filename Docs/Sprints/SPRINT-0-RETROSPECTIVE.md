# Sprint 0 — Retrospective

- Date: 2026-09-03
- Closed with: **GO** for Sprint 1

## What went well

- Architecture constraints were enforced in asmdefs (`noEngineReferences`) and tests, not only in ADRs.
- WZ1 stayed editorial: Excel was not rewritten; runtime is hashed UTF-8 JSON.
- Catalog load/validation keyed only by `CardId`, with structured errors (`cardId` + field).
- Bootstrap composition is explicit. Menus do not resolve a global service locator.
- PlayMode covers the real player path Bootstrap → MainMenu (120 cards), not only a headless session.
- Official Newtonsoft avoided a custom JSON parser after `JsonUtility` proved insufficient.

## What was painful

- `Application` inside `Warpaz.Presentation` binds to the **namespace** `Warpaz.Application`, so UnityEngine APIs must be fully qualified. Easy to miss until compile.
- TMP Essential Resources are a Unity package import into `Assets/TextMesh Pro/`, not a one-line asmdef reference.
- MCP scene tools are weak for dense uGUI; the shell scenes were built through an Editor menu invoked by MCP, not YAML edits.
- `manage_editor stop` was blocked as if it quit the editor; Play Mode had to be ended via `EditorApplication.isPlaying`.
- Rule JSON uses `contentVersion: 0.1.0` while the catalog uses `WZ1-v0.1`. Two different stamps, easy to mix in Bootstrap.

## What we would do differently

- Qualify `UnityEngine.Application` from the first Presentation script.
- Import TMP essentials before the first uGUI scene, not after the first missing-font NRE.
- Name the RuleConfig field `ruleContentVersion` (or drop it) in the next content schema so it cannot collide with WZ1.
- Keep SampleScene out of the repo once the build list points at Presentation scenes.

## Actions for Sprint 1

1. Wire **Jugar** to a real `GameSession` using the loaded WZ1 catalog (or a documented subset), not the Battle stub alone.
2. Remove or quarantine `MatchDebugHud` / `SampleScene` so there is one table UI.
3. Keep Domain free of UnityEngine; do not relax `noEngineReferences` to “fix” Presentation naming clashes.
4. Do not interpret `effectText`. Add declarative `effects` only with tests.
5. Run `scripts/test-editmode.sh` and `scripts/test-playmode.sh` before merging slices (`WARPAZ_UNITY_EDITOR` required).

## Keep / drop

| Keep | Drop / postpone |
| --- | --- |
| `cardId` as the only identity | Name-based lookups |
| Seeded RNG, hashed `GameState` | `UnityEngine.Random` in Domain |
| StreamingAssets JSON + manifest hash | Loading `.xlsx` at runtime |
| Injected `AppRuntime` | Service locator / static `Instance` |
| Empty `effects` until a grammar exists | Per-card MonoBehaviours |
