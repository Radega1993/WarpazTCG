# Content / Runtime

Player-facing catalog types (`MinimalCatalog`, manifests). Loaded in editor and player.

`Sprint1MatchFixtureFactory` builds two legal WZ1 seats from a catalog by `cardId` / `CardType` (no names, no `CardDefinition` copies, no persistence). Dev options: seed, player display names, recorded mulligan decision (not auto-submitted), debug visibility off by default.
