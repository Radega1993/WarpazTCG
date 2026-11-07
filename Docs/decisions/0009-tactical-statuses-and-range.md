# ADR 0009 — Estados tácticos y A distancia

Estado: implementado para pruebas. Regla `0.6.0-tactical`, sobre v0.5 expanded.

## Alcance

Esta versión activa la matriz mínima de combate por filas y da comportamiento a
`StatusType.Frozen` / `Immobilized` / `Hidden` / `Detected`, más **A distancia** y
**Emboscada**. No cierra Crítico, Ascenso, descuentos, Último Aliento, Rápido
completo ni la taxonomía Monstruo/Objeto.

## Congelado e Inmovilizado

- **Congelado** impide Preparar y declarar ataque. Persiste hasta que un efecto lo
  elimine; no caduca solo al pasar el turno.
- **Inmovilizado** (y Congelado) impiden reorganizar y ser movidos por efectos,
  incluida la reorganización propia.
- Escarcha (083): agota un enemigo de Poder impreso ≤4; si ya estaba agotado,
  Congélalo. Invocador (067): Despliegue elige enemigo ≤3 e impone Congelado.
  Curandera (075): Despliegue elimina Congelado o Inmovilizado de un aliado; sin
  candidatos se omite.

## A distancia

Sin la keyword, el atacante debe estar en Vanguardia y el objetivo Personaje en
Vanguardia (o Héroe si no hay Vanguardia enemiga). Con **A distancia** en el
Personaje o en su Equipo:

- puede declarar ataque desde Retaguardia;
- puede elegir Personajes enemigos en Retaguardia.

110 aporta la keyword por Equipo y +1 Poder solo al atacar Retaguardia. 040 ya
tenía la keyword impresa; su bono de Equipo sigue siendo el programa v0.5.

## Oculto, Detectar y Emboscada

- Personajes con keyword **Oculto** entran Ocultos al jugarse en Retaguardia
  (064/065). Mientras estén Ocultos y no Detectados no son objetivos legales de
  ataque enemigo.
- **Detectar** quita Oculto y aplica Detectado (093; Despliegue 046 elige Ocultos
  o, si no hay, mira el tope del mazo).
- **Emboscada N** (en Personaje o Equipo, p. ej. 065/111) permite declarar ataque
  desde Retaguardia. Un ataque con Emboscada desde Retaguardia no abre
  intercepción al Héroe.
- **Desprotegido N** suma N al Poder del atacante contra ese Personaje (keyword;
  Crítico de 051 sigue pendiente).

## Determinismo

Los pasos `FreezeTarget` / `ClearCrowdControl` / `DetectTarget` usan la cola de
efectos v0.4 (opciones ordenadas por `instanceId`). Los hashes de partidas
0.1–0.5 no cambian: solo `0.6.0-tactical` serializa el uso de estos caminos.

## Contenido compilado

| ID | Programa |
| --- | --- |
| WZ1-083 | `FreezeExhaust` |
| WZ1-067 | `DeployFreeze` |
| WZ1-075 | `DeployClearCrowdControl` |
| WZ1-110 | `EquipmentAttackRearguard` |
| WZ1-093 | `DetectEnemy` |
| WZ1-046 | `DeployDetect` |

Texto exacto obligatorio. Fuera de alcance: Crítico, Ascenso, Rápido completo,
descuentos, duraciones «próximo turno», reanimación y Último Aliento.
