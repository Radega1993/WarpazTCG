# ADR 0006 — Motor de efectos: primer bloque

Fecha: 2026-11-06. Estado: implementado para playtest; pendiente validación Unity.
Base: d5548f7d81977c52ff0d5b89f8084406d4dfe579 (PR #1).

## Alcance real

Reglas `0.3.0-effects`, conservando `0.1.0` y `0.2.0-preview`.
Programas inmutables, síncronos, de hasta 16 instrucciones con un único objetivo
Personaje. Tres operaciones: modificar Poder, impedir interceptar e impedir preparar
por efectos. Todas duran hasta final del turno actual. Se validan programa completo,
objetivo, fase, actor y coste antes de mutar; objetivo inválido no paga ni descarta.

Las Acciones se juegan desde la mano, pagan PA y van al descarte. Se ejecutan en orden.
Se reutiliza el payload Play con primary=Acción, secondary=objetivo, sin casilla;
`Command.PlayAction` y `GameSession.PlayAction` exponen la intención sin cambiar el wire format.
El tipo de carta determina si secondary es objetivo de Acción o reemplazo de Personaje.

Los modificadores se acumulan; Poder efectivo mínimo 0. No hay K.O. automático por
Poder 0 (la regla anterior solo resuelve K.O. por combate). Se borran al salir del
campo y antes de comenzar el turno siguiente; reorganizar conserva modificadores.
Restricciones y modificadores forman parte del hash canónico únicamente bajo v0.3.
La expiración ordena por InstanceId, sin depender del orden del diccionario.
Límite de acumulación ±1000; comandos que lo excedan se rechazan antes de pagar.

## Contenido

Bindings revisados en `Wz1EffectBindings`, con CardId y texto exacto como guardia de
compatibilidad. No se interpreta texto natural. Los JSON/XLSX originales no cambian;
su hash continúa validándose. El loader añade los programas compilados al catálogo.
La semántica de esos bindings pertenece a RuleVersion 0.3.0-effects: cualquier cambio
funcional requiere nueva versión de reglas. No es aún un editor de efectos JSON.

| Carta | Programa completo soportado |
|---|---|
| WZ1-078 Golpe Coordinado | +2 Poder a Personaje propio en Vanguardia |
| WZ1-079 Onda de Choque | −2 Poder e impedir interceptar a enemigo en Vanguardia |
| WZ1-082 Electrocutar | −2 Poder e impedir preparar por efectos a Personaje enemigo |
| WZ1-086 Acorde Inspirador | +2 Poder a cualquier Personaje |

El gestor permite estas cuatro Acciones con v0.3. Las demás Acciones, Reacciones y
Equipos siguen rechazados al iniciar partida. Personajes siguen en modo básico con
Único y Guardián; no se afirma soporte de sus habilidades impresas.

## No incluido

Pila/ventanas de Reacción, disparadores Despliegue/K.O./Crítico, robo-descarta y selección
oculta encadenada, equipos persistentes, auras, duraciones hasta próximo turno,
pasivas, ascensión, congelación y demás keywords. Requieren otra entrega y pruebas
propias; no se habilitan cartas parcialmente implementadas. No es el motor completo WZ1.
