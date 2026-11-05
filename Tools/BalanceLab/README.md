# BalanceLab WZ1 alpha

Run from repository root with .NET SDK 8:

```sh
dotnet run --project Tools/BalanceLab -- .
```

Optional output directory is the second argument after `--`. Default output: `Artifacts/BalanceLab/WZ1-alpha-1` (existing output is replaced on rerun).

Compiles real Domain/Application/Content sources; uses the production catalog loader and starter loader. No game-rule port. SDK's bundled Newtonsoft avoids NuGet dependencies.

100 paired-seat WZ1 games + exact repeat of each. Limits fail instead of counting as completion. Then 50 legacy synthetic games and two adversarial games, separately labelled. Invalid-actor atomicity and turn-cap regression checked. Nonzero exit on failed assertions.

Bots have access only to own hand and public board for action selection. They are deterministic greedy heuristics, not strong search agents. Printed effects are NOT implemented by the current game. Read `Docs/Balance/WZ1_REVIEW_2026-11-05.md` before interpreting win rates.

Starters: `Assets/StreamingAssets/Data/starters_wz1_alpha.json`. 40 Characters plus Hero/Class each; no unsupported Action/Reaction/Equipment. Menu **Mazos** opens selection and starts a local game. Normal Jugar and tutorial remain unchanged. Not final retail decklists.

## Unity validation required

Use the pinned editor from ProjectSettings. Run all EditMode/PlayMode tests, including ModernCardLayoutTests. Verify starter selection, mulligan and full Battle flow. Capture real GameView at 1280x720, 1600x900, 1920x1080. Check aspect, text, hit targets and face-down privacy. The headless runner cannot certify any of these visual checks.
