# ADR 0005 — Preview de mazos, Único e intercepción

Estado: propuesta implementada para playtest; pendiente aprobación y verificación Unity.
Fecha: 2026-11-05. Base: 423121491c30c5f1dd74c491dac796be025e54bb.

## Reglas versionadas

`0.1.0` y el tutorial conservan su comportamiento y golden. El gestor inicia partidas
con `0.2.0-preview`. No se cambia el catálogo ni se interpreta texto natural como código.

- Un héroe solo es atacable si su Vanguardia está vacía.
- Un personaje preparado con la palabra clave exacta `Guardián` en Retaguardia
  puede interceptar ese ataque. Reciente no impide interceptar. El defensor decide;
  interceptar agota al guardián, que mantiene su fila si sobrevive. Declinar resuelve contra el héroe.
- `Único`: un mismo CardId único por controlador en el campo. Reemplazar esa copia
  al jugar otra sigue permitido cuando la regla existente de campo lleno lo permite.
  El límite de copias del mazo sigue siendo tres.
- `position` limita la entrada al campo, no la reorganización posterior.
- Preview solo admite Personajes en el mazo principal. Acciones, Reacciones y Equipos
  pueden guardarse en borradores, pero no iniciar partida hasta implementar sus efectos.

## Mazos

Biblioteca en `Application.persistentDataPath/Decks/library-v1.json`, esquema 1.
Tres starters en la primera apertura; una biblioteca vacía deliberadamente no se repuebla.
Guardado explícito, temporal + reemplazo con backup `.bak`, rechazo de datos dañados,
formatos futuros y cambios externos. Los IDs desconocidos se conservan y bloquean jugar.
No hay economía/propiedad de cartas: todo el catálogo está desbloqueado para pruebas.

## Riesgos

No soluciona por sí solo las cartas de Poder alto, mazos de Poder <5 ni efectos faltantes.
Las simulaciones son heurísticas, no una certificación de balance o extensibilidad de expansiones.
