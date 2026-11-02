# BLOCKED: 14_debug_hud

- **Estado intentado:** MatchDebugHud visible then hidden on Battle product path
- **Comandos / UI:** MatchDebugHud is not present on Battle.unity; only SampleScene. enableDebugHud defaults false.
- **Resultado:** No PNG on Battle Game View without modifying production scene/prefab
- **Motivo:** Product Battle path intentionally excludes MatchDebugHud (PRE-SPRINT-2 / S2.0 debt isolation)
- **Evidencia:** battle_hierarchy.txt MatchDebugHudInScene=0; MatchDebugHud.cs comments
