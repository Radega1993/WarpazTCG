# Content

Card definitions, `RuleConfig` data, catalog access, importers, validators.

**May reference:** `Warpaz.Domain`.

**Must not contain:** mutable match state, commands that change `GameState`, or UI.

`Sprint1MatchFixtureFactory` (Runtime) is the Sprint 1 development seat builder: legal WZ1 ids, configurable names/seed/mulligan/debug, no deck picker and no save.
