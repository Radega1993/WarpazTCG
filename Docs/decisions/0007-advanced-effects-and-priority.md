# ADR 0007 — Efectos avanzados y prioridad

Estado: implementado para pruebas, pendiente de aceptación visual en Unity.
Reglas nuevas: `0.4.0-advanced`. Las versiones 0.1, 0.2 y 0.3 no activan estos mecanismos.

## Contrato de combate

La intercepción se decide antes de la ventana de modificación, conforme a la especificación.
Una vez fijado el defensor, este recibe la primera prioridad. Cada Reacción o Respuesta
cede prioridad al rival y reinicia la cuenta de pases. Dos pases consecutivos resuelven
una Reacción, empezando por la última jugada. Tras resolver, el defensor recupera prioridad.
Dos pases con la pila vacía resuelven el combate. Ningún turno puede terminar entre medias.

Las Reacciones pagan su coste PA al jugarse y van al descarte; la pila guarda su instancia
y el combate guarda sus objetivos fijados. Esta representación no convierte las cartas
en objetivos legales del descarte durante la ventana: Main queda bloqueada.
Los efectos se evalúan al resolverse, no al pagar. Desvanecerse termina el combate y
cancela las reacciones aún pendientes; no devuelve costes ni cartas ya gastadas.

Respuesta descarta una carta con valor positivo sin pagar PA y aplica ese valor al
combatiente de su propietario. Cada jugador puede usar una sola Respuesta por combate.
Usar Respuesta impide usar Reacciones, y viceversa. Las Reacciones compiladas en esta
entrega son defensivas; el atacante puede responder o pasar, no usar efectos defensivos.
El valor de Respuesta cero no genera una acción legal sin beneficio.

## Equipos

Un Equipo ocupa `CardZone.Equipment` y un único Personaje propio lo referencia.
Se juega en Main, paga PA y reemplaza el Equipo anterior enviándolo al descarte.
Reorganizar conserva el Equipo. K.O., sustitución, sacrificio y devolución a mano lo
descartan. Las invariantes cuentan las cartas equipadas en las 40 y detectan huérfanos,
propietarios incorrectos y referencias duplicadas. Los bonos se consultan en combate:
no se suman permanentemente al Poder ni se pierden al cambiar de fila.

## Disparadores y elecciones

Los comandos capturan los Personajes presentes antes de actuar. Los eventos se recorren
en orden, actualizando esa presencia: un Mecánico que sale junto a su Equipo sí presencia
el descarte; uno que entra después no lo presencia retroactivamente. Los observadores
se ordenan por instanceId. El límite «una vez por turno» se guarda por instancia.
Es una política determinista fija: no se implementa todavía orden de disparadores elegido
por el jugador.

El efecto actual termina antes de los disparadores que haya generado. Una cola FIFO de
pasos conserva actor, fuente, tipo, cantidad y progreso. Los pasos automáticos avanzan
hasta una elección. Sin candidatos se omite el paso; no se inventa un objetivo. Plan
Calculado ordena hasta cuatro cartas eligiéndolas de arriba hacia abajo, después roba,
después solicita una carta de la mano para el fondo. Las cartas permanecen en su zona
durante la selección; el orden se aplica al completar la permutación.

Las elecciones de mano/mazo se proyectan exclusivamente al jugador con prioridad.
Los eventos con identificadores ocultos son OwnerOnly. La UI recibe opciones del motor,
no accede al mazo para decidirlas. El pase de dispositivo oculta también este panel.
Despliegue se dispara por jugar un Personaje, nunca por moverlo mediante un efecto.
Sacrificar no emite K.O. El robo de efecto aplica la penalización por robo fallido;
GameOver elimina todas las continuaciones y no procesa nuevos disparadores.

## Determinismo y límites

La pila, bonos de combate, uso de Respuesta/Reacción, pases, contador de disparador y
progreso completo de las elecciones forman parte del hash canónico de v0.4.
No se añade RNG. Las elecciones inválidas se rechazan antes de cualquier mutación.
Los programas compilados son cerrados y finitos: como máximo cuatro elecciones de orden,
un robo de dos cartas y una elección de descarte por programa. El disparador de Equipo
no descarta Equipos en juego ni puede activarse de nuevo en el mismo turno. Cada Reacción
consume una carta de mano. No hay callbacks arbitrarios, bucles de instrucciones ni
disparadores recursivos habilitados; nuevas expansiones necesitan ampliar y probar este
contrato antes de aceptar programas que puedan producir ciclos.

## Alcance de contenido

La vinculación exige ID y texto impreso exactos. Cambiar el texto deshabilita el programa.
No se modifica el XLSX ni el JSON canónico. El catálogo mantiene `WZ1-v0.1` y su SHA.

| Mecanismo | Cartas nuevas compiladas |
| --- | --- |
| Despliegue | 018, 028, 069 |
| Intercepción | 024, 036, 055 |
| Descarte de Equipo | 039 |
| Acciones encadenadas | 084, 096 |
| Reacciones | 098, 100, 102, 106, 107 |
| Equipos | 108, 109, 119 |

Se conservan las Acciones 078, 079, 082 y 086. Total: 21 definiciones con efecto compilado,
de las que 14 son no-Personajes. Los otros Personajes siguen disponibles como vanilla
con las reglas previas de Único/Guardián; la UI advierte de las habilidades pendientes.
Los no-Personajes sin programa completo se rechazan al validar un mazo.

Esto NO cierra todo WZ1: faltan otras Reacciones, Equipos y Acciones, pasivas raciales/de
clase, Crítico, estados/keywords adicionales, Ascenso y otros disparadores (p. ej. Último
Aliento). No debe utilizarse la simulación parcial como validación del equilibrio final.
