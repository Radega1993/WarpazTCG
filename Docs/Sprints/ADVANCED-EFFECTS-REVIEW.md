# Revisión del motor avanzado v0.4

Base: `b84f73a3d2f87a29fe1f06f20cd4841e299d5b8d` (PR anteriores integradas).

Implementación: prioridad alterna, pila LIFO, Respuesta exclusiva, equipos persistentes,
disparadores deterministas y continuaciones con elecciones privadas. El menú de mazos
inicia v0.4. Battle añade un panel paginado para resolver elecciones y usar Respuesta o
Reacción, y muestra el Equipo del Personaje. Las reglas anteriores permanecen versionadas.

## Verificación ejecutada

- NUnitLite contra código real Domain/Application/Content: 144/144, cero fallos o skips.
  Incluye 23 casos nuevos y la regresión anterior, incluidos golden históricos.
- Domain y Application como ensamblados separados: .NET Standard 2.1 / C# 9,
  cero errores y cero warnings. Este chequeo evita que el harness combinado oculte
  accesos a miembros internos entre capas.
- BalanceLab: 100 partidas completas, cada una repetida con la misma semilla y traza/hash
  idénticos. 15 896 comandos aceptados, cero rechazos inesperados y cero invariantes rotas.
- Actividad: 519 Acciones, 140 Reacciones, 1 465 Respuestas, 175 Equipos, 588 elecciones,
  3 disparadores. Media 34,63 turnos; rango 15–73; primer jugador gana 54/100.
- Intercepciones en ese soak: **0**. La cobertura está en tres fixtures específicos
  (024/036/055), no en las 100 partidas. No confundir conteos de pases con intercepciones.
- Soak histórico adicional: 50 partidas completas. Diagnósticos de fatiga, límite de
  turnos y rechazo sin estado parcial pasan por separado.
- Catálogo SHA-256 sin cambios: `12c7ffece2850f50cf8682909ff024b41935219d82c5254d68d93145be135029`.

Los bots usan una heurística sencilla, no juego óptimo. Las versiones experimentales de
los tres starters sustituyen 14 Personajes por una copia de cada no-Personaje soportado;
no reemplazan los mazos guardados del usuario. No son una prueba de balance de todo WZ1.

## Reproducir

Desde la raíz, con .NET SDK 8:

```bash
dotnet build Tools/AssemblyCheck/Application/Application.csproj
dotnet run --project Tools/DomainTests -- --result=Artifacts/AdvancedEffects/tests.xml
dotnet run --project Tools/BalanceLab -- . Artifacts/AdvancedEffects --advanced
```

El generador produce también trazas completas y auditoría de cartas, no versionadas aquí.
Se adjuntan summary.json, matches.csv, diagnostics.json y tests.xml como evidencia.

## Gate Unity pendiente

No se ha ejecutado Unity Editor, PlayMode, captura visual ni compilación de Presentation.
No se declara la V1 terminada. Antes de integrar en una versión jugable:

1. Abrir Unity 6000.3.20f1 y comprobar compilación y EditMode/PlayMode completos.
2. Abrir Mazos, añadir 096/098/108 a un mazo válido e iniciar partida local.
3. Jugar 108 sobre un Personaje, reemplazarlo por 109 y comprobar su texto y descarte.
4. Atacar: comprobar el pase de dispositivo, Respuesta/Reacción, prioridad alterna,
   dos pases y retorno a Main. El panel muestra Poder de ambos, pila y pases.
5. Usar 096: ordenar cuatro cartas, robar y elegir una de mano para el fondo. Verificar
   privacidad al pasar dispositivo y paginación con más de seis opciones.
6. Comprobar Despliegue 018 y cadena de sacrificio 084 con Mecánico 039 equipado.
7. Capturar 1280×720, 1600×900 y 1920×1080, incluyendo ventana de reacción y elección.

El detalle de políticas y las cartas todavía pendientes están en ADR 0007.
