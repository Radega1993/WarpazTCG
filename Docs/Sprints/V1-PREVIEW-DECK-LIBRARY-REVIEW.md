# Reconstrucción: gestor de mazos y correcciones preview

Fecha: 2026-11-05. Base: `feat/wz1-review-starters` / `4231214`.

## Entregado

- Mis mazos: crear, editar, renombrar, duplicar, eliminar con confirmación y guardar.
- Catálogo visual paginado con imágenes existentes, detalle, búsqueda nombre/ID,
  filtro de tipo, filtro de cartas del mazo, cantidades y curva de PA.
- Héroe/Clase seleccionables, borradores incompletos, validación 40 cartas / máximo 3 copias.
- Selección de dos mazos válidos guardados para jugar, con mulligan para ambos.
- Guardado persistente con backup y protección contra corrupción/conflictos.
- Intercepción de Guardián desde Retaguardia y unicidad, versionadas como preview.
- Mano paginada de 8 cartas, sin límite artificial de 12 cartas seleccionables.
- Privacidad al ocultar mano: limpieza de estado y cartas, controles desactivados.

## Pruebas realmente ejecutadas en esta reconstrucción

| Comprobación | Resultado |
|---|---|
| C# Domain/Application/Content (.NET 8) | Compila, 0 errores / 0 warnings |
| NUnitLite: suite seleccionada + nuevas pruebas | 110/110 PASS, 0 skip |
| Golden Sprint 2 / reglas legacy | PASS, hash original intacto |
| Simulación preview | 100/100 completas |
| Repetición de las mismas 100 configuraciones | Mismas métricas, hashes y trazas |
| Rechazos inesperados / invariantes | 0 / 0 |
| Soak legacy separado | 50/50 completas |
| Compilación Presentation, EditMode/PlayMode completos, capturas Unity | NO EJECUTADOS; falta Unity Editor |

Resultados nuevos en `Artifacts/BalanceLab/WZ1-preview-rebuilt/`.
16.228 comandos, 21–79 turnos (media 49,07), 50 victorias del primer jugador,
2.008 despliegues de Retaguardia, 1.829 reorganizaciones, 4 intercepciones.
79 finales por combate y 21 por fallo de robo. Presión 41/68, Bastión 32/66, Reserva 27/66.
La repetición no duplica la muestra. No se afirma que el juego esté equilibrado.
Los tests específicos de intercepción/Único usan un campo preparado y ejecutan el pipeline real;
las simulaciones parten de CreateMatch y acciones legales.

## Validación requerida antes de fusionar

1. Abrir Bootstrap con la versión de Unity de ProjectSettings/ProjectVersion.txt.
2. Ejecutar EditMode y PlayMode completos, incluyendo HandPaginationTests.
3. Jugar/Mazos → duplicar starter, editar cantidades, renombrar, guardar, reiniciar.
4. Guardar borrador de 39 cartas y mazo con Acción; no deben poder seleccionarse para jugar.
5. Probar crear, eliminar (doble confirmación), biblioteca vacía y salida sin guardar.
6. Seleccionar ambos mazos, mulligan, jugar Guardián en Retaguardia y probar interceptar/declinar.
7. Mano con 13+ cartas, clic en segunda página y cambio de dispositivo sin filtraciones.
8. Capturas reales a 1280×720, 1600×900 y 1920×1080 del gestor/editor/partida.

## Pendiente para V1

Motor de efectos de Acciones/Reacciones/Equipos, otras palabras clave, pasivas de identidad,
ascensión y respuestas a Poder alto. Mantener avisos visibles hasta implementarlos.
Validación humana y varios bots, balance y contenidos de futuras expansiones quedan pendientes.
Esta entrega es una preview y el PR debe permanecer en borrador hasta validar Unity.
