# Motor de efectos — primera entrega

Base PR #1; rama `codex/effects-engine-v1`. Fecha: 2026-11-06.

## Entregado

- Instrucciones tipadas y programas acotados; validación antes de ejecutar.
- Modificadores temporales, restricciones, expiración y hash determinista.
- Acciones 078/079/082/086 con su efecto completo. Resto no habilitado.
- LegalActions genera objetivos; GameSession ofrece PlayAction.
- UI: seleccionar Acción en mano → clic en Personaje objetivo (propio o enemigo),
  resaltado legal y Poder efectivo mostrado en el tablero.
- Gestor inicia con reglas 0.3.0-effects; tutorial permanece legacy.

## Verificación

121 pruebas NUnitLite PASS (0 skip), incluida regresión golden Sprint 2.
Solo Domain/Application/Content y tests independientes de Unity; **NO** equivale a
compilar Presentation ni a ejecutar toda la suite EditMode/PlayMode.
Los nuevos tests cubren los cuatro programas, costes, descarte, objetivos inválidos,
duplicados, PA insuficientes, acumulación, hash, expiración, salida del campo y prevención.
Los tests focalizados preparan el estado; las simulaciones usan CreateMatch y comandos legales.

100 partidas con 32 Personajes + 8 Acciones (2 de cada soportada) por mazo, derivadas
de los tres starters sin modificar sus archivos originales. Repetición idéntica
de las 100 configuraciones. 13.047 comandos, 0 rechazos inesperados / invariantes,
17–76 turnos (media 41,46), 51 victorias del primer jugador, 93 finales por combate,
7 por fallo de robo. 1.001 Acciones resueltas. No hubo intercepciones en esta muestra; las cubre la suite anterior.
Los bots son heurísticos; no prueban equilibrio ni representan estrategias humanas.

## Reproducir

Desde la raíz, .NET 8:

```
dotnet run --project Tools/DomainTests -- --result=Artifacts/EffectsFoundation/tests.xml
dotnet run --project Tools/BalanceLab -- . Artifacts/EffectsFoundation --effects
```

## Antes de fusionar

Abrir Bootstrap en Unity, ejecutar EditMode/PlayMode completos, crear mazo con las
cuatro Acciones, seleccionar objetivos, comprobar coste/descarte/Poder y fin de turno.
Probar objetivo propio/enemigo incorrecto, PA insuficientes, cambio de dispositivo y
regresión de tutorial/gestor. Capturas reales a 1280×720, 1600×900 y 1920×1080 pendientes.
Mantener PR en borrador hasta esa validación. Ver ADR 0006 para límites y próximos bloques.
