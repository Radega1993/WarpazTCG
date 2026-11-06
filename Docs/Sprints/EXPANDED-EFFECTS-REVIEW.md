# Revisión v0.5 — Auras y efectos condicionados

Base: PR 3, commit `8803f1157942923022620270e810c0e232740690`.
Esta entrega depende de esa PR; no sustituye su validación pendiente en Unity.

## Entrega

17 programas de texto nuevos: 14 pasivas de Personajes, Armadura 114 y Reacciones
099/104. Incluye cálculo vivo de auras/condiciones, consulta de protección frente a
movimientos enemigos y elección opcional tras sobrevivir al combate. El menú inicia
v0.5; v0.4 conserva su comportamiento. Ver ADR 0008 para límites y políticas.

La auditoría de las 120 cartas y la lista de cierre V1 separan programas de texto,
keywords, integración visual y equilibrio. No se presenta esta entrega como V1 completa.

## Pruebas ejecutadas

- 166/166 NUnitLite: 22 casos nuevos, cero fallos y cero skips.
- Domain/Application en ensamblados separados, .NET Standard 2.1 / C# 9:
  cero errores y cero warnings.
- v0.5: 100 partidas completas y 100 repeticiones idénticas; 14 415 comandos,
  cero rechazos inesperados e invariantes rotas. Rango 17–67 turnos, media 31,09.
- Uso: 436 Acciones, 214 Reacciones, 1 425 Respuestas, 187 Equipos y 427 elecciones.
- Primer jugador: 54 victorias. Presión 32/68, Bastión 40/66, Reserva 28/66.
  Es una señal para playtesting, no evidencia suficiente de equilibrio.
- Intercepciones y disparadores de eventos en el soak v0.5: **0**. Se mantienen fixtures
  específicos anteriores. Las auras son consultas, no eventos TriggerQueued.
- Regresión v0.4: otras 100 partidas + repeticiones; los 100 registros de matches.csv,
  incluidas métricas y hashes, coinciden con los publicados en PR 3.
- Soak legacy y diagnósticos adversariales adicionales pasan.
- JSON de contenido sin cambios, SHA-256:
  `12c7ffece2850f50cf8682909ff024b41935219d82c5254d68d93145be135029`.

## Reproducción

```bash
dotnet build Tools/AssemblyCheck/Application/Application.csproj
dotnet run --project Tools/DomainTests -- --result=Artifacts/ExpandedEffects/tests.xml
dotnet run --project Tools/BalanceLab -- . Artifacts/ExpandedEffects --expanded
dotnet run --project Tools/BalanceLab -- . Artifacts/RegressionV04 --advanced
```

El harness admite `-p:NUnitOfflineDir=/ruta/a/dlls` si se dispone de las DLL NUnit y
NUnitLite 3.14.0 de una ejecución anterior. Durante la última recompilación se perdió
la caché NuGet del entorno y se reutilizaron esas DLL locales; no se sustituyeron tests
ni código del motor. Sin esa opción se restaura normalmente desde PackageReference.

## Pendiente de validar en Unity

No se ha ejecutado Editor, compilación de Presentation, PlayMode completo ni capturas.
En el editor hay que comprobar: Armadura en mano/tablero; Poder mostrado durante
combate con aura; Esquiva con aceptar/declinar y retaguardia llena; privacidad al pasar
el dispositivo y carga de los mazos existentes. Mantener la PR en borrador hasta ello.

Dos problemas de diseño permanecen explícitos: la cláusula secundaria de Contraataque
carece de valor con K.O. del atacante perdedor, y Monstruo no tiene clasificación en el
catálogo aunque hay cartas que lo buscan. No se alteró el set para ocultar esos problemas.
