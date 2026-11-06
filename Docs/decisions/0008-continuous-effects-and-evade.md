# ADR 0008 — Poder continuo, auras y evasión

Estado: implementado para pruebas. Regla `0.5.0-expanded`, sobre PR 3 / `8803f11`.

## Poder

Los bonos condicionados se calculan sobre el tablero actual, sin modificar las estadísticas
impresas ni almacenar auras en cada receptor. Al salir una fuente o cambiar de fila, el
bono cambia automáticamente. El Poder impreso y el coste impreso se consultan en el
catálogo, no en los valores modificados del combate.

Las auras «otros» excluyen la fuente, pero dos fuentes diferentes podrían beneficiarse
entre sí cuando la regla de unicidad permita tenerlas. Los bonos se suman antes de
limitar el Poder efectivo a cero. Se calculan ambos valores antes de resolver cualquier
K.O.; no se recalcula el segundo combatiente después de retirar la primera aura.
Los bonos que necesitan un contrario se consultan solo para los combatientes actuales.

## Movimientos y equipos

`CanMoveByEffect` distingue control del efecto y del Personaje. La protección de 027 solo
rige en Vanguardia; 060 y 114 protegen en ambas filas. No impide reorganización propia,
evasión propia, sacrificio, sustitución o K.O. Los futuros efectos enemigos que muevan
cartas deberán pasar por este contrato antes de mutar el estado. No se afirma cobertura
de interacciones enemigas aún no implementadas.

114 aporta +2 al defender, no al atacar. Los demás Equipos conservan las políticas de
ADR 0007 y se descartan al salir su portador.

## Reacciones nuevas

- 099 Contraataque: +2 al defensor. Si el atacante pierde, se intenta aplicar el −1
  posterior solo si sigue en juego. Con la política actual el atacante perdedor queda
  K.O.; esa cláusula no genera valor. Se conserva el texto fuente y se registra la
  incoherencia para una decisión de diseño, sin cambiar automáticamente las reglas.
- 104 Esquiva Élfica: +2 al defensor durante el combate. Después, si sobrevive en
  Vanguardia y hay espacio en Retaguardia, su controlador puede moverlo o declinar.
  Se usa la primera casilla libre en orden de índice; actualmente no hay adyacencias
  ni diferencias reglamentarias entre casillas de la misma fila. Si se añaden, habrá
  que convertirla en selección explícita de casilla.
  No consume la reorganización gratuita. Sin superviviente o sin espacio se omite la
  elección. Si el combate fue cancelado por Desvanecerse, no se ejecuta esta continuación.

## Contenido nuevo

| Tipo | IDs WZ1 |
| --- | --- |
| Pasivas de Personajes | 019, 027, 034, 035, 037, 040, 045, 050, 054, 056, 058, 059, 060, 073 |
| Equipo | 114 |
| Reacciones | 099, 104 |

Son 17 programas nuevos de texto, no 17 cartas certificadas en todas sus keywords.
En particular 040 conserva «A distancia» impreso, pero esa regla todavía no está activa.
Los vínculos exigen ID y texto exactos. Los nuevos no-Personajes no son legales en v0.4.
Los nuevos campos de continuación y combate solo se serializan en v0.5, conservando
los hashes históricos de v0.4. El catálogo JSON y el XLSX fuente no se han cambiado.
