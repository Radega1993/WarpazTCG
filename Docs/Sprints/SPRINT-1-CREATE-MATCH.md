# Sprint 1 — CreateMatch (parte 1)

Slice de preparación determinista. No implementa mulligan ni el ciclo de turno (ya existen y no se reescriben).

## Desviaciones respecto al prompt de implementación

El prompt se subordina a Sprint 0 y a los ADR aceptados.

1. **Fase final de CreateMatch: `MulliganP0`, no `MulliganP1`.** [0002-sprint1-turn-engine.md](../decisions/0002-sprint1-turn-engine.md) define mulligan por `playerId` (`MulliganP0` luego `MulliganP1`), no por jugador inicial. El prompt pedía terminar en `MulliganP1`; se ignora.
2. **Un solo motor.** `MatchFactory.TryCreate` extrae la construcción de estado. `RulesEngine.Submit` sigue siendo la única entrada de comandos.
3. **Un solo stream RNG.** `SeededRng(matchSeed)` una vez. Orden ADR: instanciar P0, P1 → barajar P0, P1 → `firstPlayerId = rng.Next(0, 2)` → robar P0, P1. No hay un RNG por jugador (compartirían la secuencia).
4. **Sin sinergia raza/clase.** `DeckValidator` comprueba tipos Héroe/Clase y que las identidades no vayan al mazo principal. No hay más compatibilidad.
5. **Seed.** `ulong`; `0` es válido. El modelo no impone más restricciones.
6. **`deckId` y `displayName`** viven en `DeckList` / `PlayerSetup` / `MatchSetupRequest`. No entran en `GameState` ni en el hash canónico.
7. **Nombres de asiento.** Domain usa `Player0`/`Player1`. `MatchSetupRequest.Player1`/`Player2` se sientan por `PlayerSetup.PlayerId`.

## Mulligan (Sprint 1)

Prompt “Player 1 / MulliganP1” = asiento `Player0` / fase `MulliganP0`. “Player 2 / MulliganP2” = `Player1` / `MulliganP1`. Tras el segundo confirm, `StartTurn(FirstPlayerId)` — el inicial lo eligió `MatchFactory`.

No hay `ConfirmMulliganCommand`: el payload es `Command.Decision` (`KeepHand` | `ReplaceWholeHand`). `KeepHand` bool se deriva.

Eventos (nombres existentes):

- MulliganConfirmed → `MulliganKept` o `MulliganReturned` (público, sin instance ids)
- CardsReturnedToDeck → `MulliganReturned` (público, solo conteo)
- `DeckShuffled` público, sin orden
- InitialHandDrawn → `InitialHandDrawn` público, solo conteo
- Robos → `CardDrawn` **OwnerOnly**
- `PhaseChanged` público

## Modelos

- `DeckList` — metadatos + héroe + clase + lista principal (la longitud 40 la exige el validador).
- `PlayerSetup` — sustituye `PlayerMatchSetup`. No coexisten.
- `MatchSetup` — payload de `Command.CreateMatch`.
- `MatchSetupRequest` (Application) — `ruleVersion` string + dos `PlayerSetup` + seed + `contentVersion`.
- `MatchFactory` — valida y construye, o rechaza con `CommandResult.Reject(null, …)`.

## Versiones de contenido

`ICardCatalog.ContentVersion` compara la petición con el catálogo cargado. WZ1 usa `WZ1-v0.1`. `MinimalCatalog` usa `WZ0-test` para no fingir el sello WZ1.
