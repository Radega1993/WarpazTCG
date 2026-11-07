# Cierre de WARPAZ V1 local

Alcance de referencia: juego local hot-seat completo con las 120 cartas WZ1, edición y
guardado de mazos, reglas implementadas sin cartas vanilla provisionales, interfaz
utilizable y build distribuible. Online, cuentas, matchmaking, tienda y expansiones no
forman parte de este cierre. «100 %» significa cumplir los criterios de aceptación de
este documento, no garantizar ausencia absoluta de errores.

## Estado después de v0.5

Hay 38 definiciones con programa de texto compilado; 042 se resuelve por la regla de
Guardián y 074 no tiene habilidades. Esto NO significa 40 cartas completamente
certificadas: aún faltan keywords y combinaciones. La auditoría de las 120 cartas está
en `Artifacts/ExpandedEffects/card-coverage.md` y se genera con BalanceLab.

El motor básico, mazos persistentes, cartas ilustradas, ventanas de prioridad, equipos y
elecciones existen. Se han contado 120 PNG de ilustraciones en Git, sin certificar en
esta sesión su composición visual. El núcleo tiene pruebas automatizadas; Presentation,
EditMode/PlayMode completos y builds Unity siguen pendientes en este entorno.

## 1. P0 — Cerrar las decisiones de reglas y contenido

- [ ] Fijar el reglamento V1 y dejar de cambiar reglas sin nueva versión.
- [ ] Definir exactamente Crítico, su umbral, empates, protección, orden y cancelación.
- [ ] Definir taxonomía de Monstruo. El catálogo no contiene tipo ni subtipo Monstruo,
  pero 068, 076 y 117 lo mencionan. 076 no tiene actualmente un objetivo Monstruo
  explícito para su reanimación. Decidir etiquetas sobre cartas existentes o contenido
  nuevo; no inventar equivalencias entre Jefe/Bestia/Personaje y Monstruo.
- [ ] Definir Objeto de forma uniforme: no es CardType; 116 lo tiene como keyword.
  Validar búsquedas y descuentos que se refieren a Objetos.
- [ ] Resolver la cláusula sin valor práctico de 099 Contraataque y revisar 057,
  que también depende de «si el ataque falla».
- [ ] Documentar ventanas de efectos simultáneos, K.O. mutuo, reemplazo de K.O.,
  sacrificio frente a Último Aliento y prioridad de comprobación de victoria.

Aceptación: reglas y textos no se contradicen y cada condición tiene al menos un
escenario alcanzable, salvo que se documente explícitamente como preparación de expansión.

## 2. P0 — Completar capacidades del motor

- [x] A distancia y matriz de objetivos Vanguardia/Retaguardia (mínimo v0.6; ver ADR 0009).
- [x] Oculto, Detectado/Detectar, Emboscada, Desprotegido (mínimo v0.6; Rápido pendiente).
- [x] Congelado e Inmovilizado: aplicación, prohibiciones, eliminación y expiración (v0.6).
- [ ] Crítico y disparadores posteriores al combate; prevenir Crítico.
- [ ] Habilidades activadas: coste, agotamiento, objetivos y límites por turno.
- [ ] Ascenso: requisitos, coste, transformación/mejora y pasivas de Clase.
- [ ] Descuentos de coste: primera Acción/Equipo/Canción/Objeto, suelo de coste y
  consumo del descuento solamente al aceptar el comando.
- [ ] Duraciones «próximo ataque», «hasta tu próximo turno» y «siguiente Preparación».
- [ ] Reorganizaciones adicionales sin gastar ni restaurar indebidamente la gratuita.
- [ ] Búsqueda/revelación con filtros, orden arbitrario del resto y recuperación del descarte.
- [ ] Reanimar sin Despliegue, destierro al salir, reemplazos de K.O. y Último Aliento.
- [ ] Disparadores de Héroe/Clase, canción, sacrificio, pérdida de Oculto, K.O. aliado
  y comienzo/final de turno; límites por instancia y por jugador.
- [ ] Límites explícitos para nuevos ciclos de disparadores/elecciones cuando se
  habiliten programas recursivos. Los actuales son cerrados y finitos.
- [ ] Rendición mediante comando validado; existe el enum, no su handler.

Aceptación: acciones legales y Submit coinciden, rechazos no mutan estado/RNG/eventos,
las duraciones expiran exactamente una vez y no hay estados pendientes sin salida legal.

## 3. P0 — Completar WZ1 carta a carta

| Grupo | Textos pendientes | IDs WZ1 |
| --- | ---: | --- |
| Héroes | 11 | 001–011 |
| Clases | 6 | 012–017 |
| Personajes | 37 | 020–023, 025–026, 029–033, 038, 041, 043–044, 046–049, 051–053, 057, 061–068, 070–072, 075–077 |
| Acciones | 14 | 080–081, 083, 085, 087–095, 097 |
| Reacciones | 3 | 101, 103, 105 |
| Equipos | 9 | 110–113, 115–118, 120 |

Son 80 textos sin programa completo. Además hay que certificar las keywords y sus
interacciones en las cartas cuyo texto ya está compilado. No utilizar 38/120 como
porcentaje global de desarrollo.

- [ ] Un test de efecto positivo y uno de restricciones por carta.
- [ ] Pruebas de texto/datos: ningún vínculo silenciosamente deshabilitado por cambios.
- [ ] Todas las cartas seleccionables en un mazo legal ejecutan el texto completo.
- [ ] Eliminar el modo vanilla provisional y los avisos de «efectos pendientes» solo
  después de cumplir estos criterios, no antes.

## 4. P0 — Validación real en Unity

- [ ] Compilar todas las asmdef en Unity 6000.3.20f1; sin errores, referencias rotas
  ni Missing Scripts. El harness .NET no reemplaza esta comprobación.
- [ ] EditMode y PlayMode completos, incluyendo UI y pruebas nuevas.
- [ ] Recorrer Bootstrap → menú → mazos → mulligan → partida → victoria/derrota → menú.
- [ ] Probar ventanas alternas, paginación de decisiones, selección de cartas y pase
  de dispositivo sin revelar mano, orden del mazo ni elecciones privadas.
- [ ] Capturas y revisión de cartas/mesa a 1280×720, 1600×900 y 1920×1080.
- [ ] Verificar recorte de ilustraciones, tipografía, textos largos, Equipos, estados,
  Poder temporal, costes descontados y detalle ampliado de carta.

## 5. P1 — Terminar la experiencia de partida

- [ ] Pantalla final clara con ganador, causa, revancha y vuelta a mazos/menú.
- [ ] Rendición y salida de partida con confirmación y sin dejar sesión colgada.
- [ ] Indicadores de atacante/defensor, objetivos, pila pendiente y efectos aplicados.
- [ ] Desglose legible de Poder: impreso, aura, Equipo, Respuesta y modificadores.
- [ ] Selector de descarte/mazo y ordenación entendible para elecciones complejas.
- [ ] Tutorial actualizado para Respuesta/Reacción, filas, Equipos y Ascenso.
- [ ] Ajustes mínimos de ventana/pantalla y legibilidad; retirar controles de depuración
  de la build de juego o hacerlos explícitamente opt-in.

## 6. P1 — Certificar los mazos y guardados

- [ ] Crear, editar, duplicar, renombrar y borrar mazos en la build, no solo en tests.
- [ ] Validar identidad, tamaño, copias y restricciones nuevas de Clase/Equipo.
- [ ] Conservar borradores inválidos sin permitir iniciar partida con ellos.
- [ ] Probar backup, corrupción, escritura concurrente y recuperación tras cierre abrupto.
- [ ] Versionar/migrar mazos cuando cambien reglas o contenido, sin sustituir los del usuario.
- [ ] Publicar tres starters definitivos que usen las reglas completas, con estrategia,
  curva de costes y lista reproducible. Los mazos de simulación no son aún esos starters.

## 7. P1 — Balance y situaciones imposibles

- [ ] Repetir las simulaciones con todo el set activo y varios estilos de bot.
- [ ] Medir cada emparejamiento en ambos asientos, iniciativa, duración, fatiga,
  cartas muertas, PA sobrante y bloqueos de Vanguardia/Retaguardia.
- [ ] Forzar escenarios de intercepción y disparadores: las últimas 100 partidas
  v0.5 no ejercitaron esos mecanismos. Hay fixtures, pero falta cobertura representativa.
- [ ] Auditar cartas con ventaja excesiva, condición inalcanzable o efecto inútil;
  no ajustar automáticamente textos sin registrar la decisión y versionar contenido.
- [ ] Playtesting humano de reglas y UI; acordar umbrales de aceptación antes de
  interpretar porcentajes de victoria. Cien partidas de un único bot no prueban balance.

## 8. P1 — Publicación y mantenimiento

- [ ] Integrar las PR en orden, ejecutar regresiones y etiquetar release candidata.
- [ ] Generar build Linux para el equipo de desarrollo y las plataformas de distribución
  que se acuerden; instalar y jugar fuera del Editor.
- [ ] Verificar assets/reverso, licencias, textos, carga sin red y rutas de guardado.
- [ ] Documentar instalación, controles, reglas, mazos, versión y problemas conocidos.
- [ ] Automatizar compilación/tests compatibles en CI; Unity requiere runner/licencia
  configurados, no guardar credenciales en el repositorio.
- [ ] Fijar catálogo/manifesto y fixtures deterministas de la release, y probar que un
  set nuevo pueda añadir cartas sin romper partidas de la versión anterior.

## Criterio de cierre

Todos los P0/P1 anteriores resueltos o excluidos expresamente del alcance acordado;
120 cartas certificadas, partida completa en build desde un mazo guardado, cero errores
bloqueantes conocidos y resultados reproducibles. Online, economía de sobres, móvil,
IA rival de producto y nuevas expansiones requieren sus propios criterios si se añaden.
