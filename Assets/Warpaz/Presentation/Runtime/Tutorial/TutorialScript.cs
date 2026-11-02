using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public enum TutorialGoal
    {
        ReadAndContinue = 0,
        MulliganKeep = 1,
        PlayExact = 2,
        EndTurn = 3,
        ObserveTutor = 4,
        ReorganizeExact = 5,
        AttackExact = 6,
        AttackHeroExact = 7,
        WaitVictory = 8,
        Finish = 9
    }

    /// <summary>
    /// Full guided curriculum: card types, timing, rows, then a short live match loop.
    /// Interactive plays stay Character-only (current engine).
    /// </summary>
    public static class TutorialScript
    {
        public sealed class Step
        {
            public Step(
                string title,
                string body,
                string compactHint,
                string primaryLabel,
                TutorialGoal goal,
                string requiredCardId = null,
                BoardRow requiredRow = default,
                int requiredSlotIndex = 0,
                string requiredBoardCardId = null,
                string requiredTargetCardId = null)
            {
                Title = title;
                Body = body;
                CompactHint = compactHint;
                PrimaryLabel = primaryLabel;
                Goal = goal;
                RequiredCardId = requiredCardId;
                RequiredRow = requiredRow;
                RequiredSlotIndex = requiredSlotIndex;
                RequiredBoardCardId = requiredBoardCardId;
                RequiredTargetCardId = requiredTargetCardId;
            }

            public string Title { get; }

            public string Body { get; }

            public string CompactHint { get; }

            public string PrimaryLabel { get; }

            public TutorialGoal Goal { get; }

            public string RequiredCardId { get; }

            public BoardRow RequiredRow { get; }

            public int RequiredSlotIndex { get; }

            public string RequiredBoardCardId { get; }

            public string RequiredTargetCardId { get; }

            public bool IsActionGoal =>
                Goal != TutorialGoal.ReadAndContinue &&
                Goal != TutorialGoal.ObserveTutor &&
                Goal != TutorialGoal.WaitVictory &&
                Goal != TutorialGoal.Finish;
        }

        // Opening learner hand: 018,042,026,062,030 — Tutor: 050,...
        public static readonly Step[] Steps =
        {
            new Step(
                "Bienvenido a WARPAZ",
                "Esto no es una partida libre: es la escuela completa.\n" +
                "Aprenderás tipos de carta, cuándo se usan, Vanguardia/Retaguardia y el ciclo de turno.\n" +
                "Tú eres el Aprendiz. El Tutor juega solo. En cada paso solo se permite UNA acción.",
                "Escuela WARPAZ",
                "Empezar",
                TutorialGoal.ReadAndContinue),

            new Step(
                "La mesa",
                "Cada jugador tiene:\n" +
                "• Héroe + Clase (identidad, no se juegan desde la mano)\n" +
                "• Mazo, descarte y mano privada\n" +
                "• Mesa con filas: Vanguardia (frente) y Retaguardia (atrás)\n" +
                "La casilla vacía muestra el nombre de la fila. Ahí van los Personajes.",
                "Mira Vanguardia / Retaguardia",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Vanguardia",
                "Vanguardia = primera línea.\n" +
                "• Solo desde Vanguardia puedes declarar ataques (Personaje listo y sin Recent).\n" +
                "• Si tienes alguien en Vanguardia, el rival normalmente debe atacar ahí (no a tu Héroe).\n" +
                "• Es la fila de presión… y también la que más riesgo corre.",
                "Vanguardia = atacar / ser golpeado",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Retaguardia",
                "Retaguardia = segunda línea.\n" +
                "• No declara ataques normales.\n" +
                "• Sirve para preparar relevos, proteger piezas o guardar un Personaje que aún no quieres exponer.\n" +
                "• Con 1 reorganización gratuita por turno puedes mover entre filas (casilla vacía u otras reglas de relevo).",
                "Retaguardia = preparar / proteger",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Tipos de carta",
                "En WZ1 hay 6 tipos:\n" +
                "1) Héroe — tu identidad (empieza en juego)\n" +
                "2) Clase — camino de la escuela (empieza en juego)\n" +
                "3) Personaje — se juega a la mesa pagando PA\n" +
                "4) Equipo — se adjunta a un Personaje (en tu Main)\n" +
                "5) Acción — efecto en tu turno, suele ir al descarte\n" +
                "6) Reacción — responde a una ventana (no es un «juguete» libre en Main)",
                "6 tipos WZ1",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Héroe y Clase",
                "No se roban ni se «juegan» desde la mano.\n" +
                "Definen quién eres y qué escuela sigues. El Héroe puede recibir daño de Resolución si el rival abre tu Vanguardia.\n" +
                "En el mazo de 40 van Personajes, Equipos, Acciones y Reacciones (con límites de copias).",
                "Identidad ≠ mano",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Personaje — cuándo sí",
                "Se juega en tu fase Main, si:\n" +
                "• Tienes PA suficiente (coste en la gema)\n" +
                "• Hay casilla legal (o reemplazo según reglas)\n" +
                "Entra con Recent: este turno NO puede atacar.\n" +
                "En Preparación del siguiente turno se limpia Recent y se alistan agotados.",
                "Personaje: Main + PA + casilla",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Personaje — cuándo no",
                "NO puedes jugarlo si:\n" +
                "• No es tu Main / no tienes prioridad\n" +
                "• No te alcanza el PA\n" +
                "• La casilla no es legal\n" +
                "• Estás en Mulligan, esperando al rival, o en ventana de reacción/combate\n" +
                "Atacar tampoco: si está en Retaguardia, Exhausted o aún lleva Recent.",
                "Personaje: restricciones",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Equipo",
                "Equipo = mejora de un Personaje tuyo en mesa.\n" +
                "• Cuándo sí: tu Main, pagando PA, con un ancla legal (límite por personaje).\n" +
                "• Cuándo no: sin Personaje válido, sin PA, fuera de tu Main, o si ya está al máximo de equipos.\n" +
                "Si el Personaje cae, el Equipo suele irse con él (según carta).",
                "Equipo → ancla en mesa",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Acción",
                "Acción = efecto puntual en tu turno.\n" +
                "• Cuándo sí: tu Main (salvo texto que diga otra cosa), pagando PA.\n" +
                "• Cuándo no: turno del rival, sin PA, o si el texto pide un objetivo que no existe.\n" +
                "Tras resolver, normalmente va al descarte. No ocupa casilla de mesa.",
                "Acción: Main + objetivo legal",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Reacción",
                "Reacción = respuesta, no «jugada libre».\n" +
                "• Cuándo sí: solo cuando el motor abre una ventana (p. ej. tras declarar ataque) y la carta encaja.\n" +
                "• Cuándo no: en un Main tranquilo «porque quiero»; sin ventana no hay Reacción legal.\n" +
                "Sprint actual: el combate vanilla no abre aún ventanas de Reacción — las aprenderás al ampliar el motor.",
                "Reacción ≠ turno libre",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Mulligan",
                "Antes del primer turno miras tu mano de 5.\n" +
                "• Conservar: te quedas con esas 5.\n" +
                "• Reemplazar: descartas toda la mano y robas 5 nuevas (una sola vez).\n" +
                "Aquí la mano está fijada para el guion: pulsa CONSERVAR. Reemplazar está bloqueado.",
                "Pulsa Conservar",
                string.Empty,
                TutorialGoal.MulliganKeep),

            new Step(
                "Estructura del turno",
                "Cada turno automático:\n" +
                "1) Preparación — alista / limpia Recent\n" +
                "2) PA — sube tu máximo y rellena PA\n" +
                "3) Robo — 1 carta (si el mazo falla, pierdes Resolución)\n" +
                "4) Main — juegas, reorganizas, atacas, pasas\n" +
                "Luego Finalizar turno → cierra y empieza el rival.",
                "Prep → PA → Robo → Main",
                "Entendido",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Juega a Vanguardia",
                "Práctica: Personaje barato al frente.\n" +
                "1) Toca «Iniciado del Cristal» (PA 1).\n" +
                "2) Toca la casilla Vanguardia izquierda.\n" +
                "Entra con Recent: no atacará este turno. Mira cómo la casilla deja de decir solo «Vanguardia».",
                "Iniciado → Vanguardia izq.",
                "Listo, actuar",
                TutorialGoal.PlayExact,
                requiredCardId: "WZ1-018",
                requiredRow: BoardRow.Vanguard,
                requiredSlotIndex: 0),

            new Step(
                "Recent",
                "Recent = «recién llegado».\n" +
                "Impide atacar este turno. No te impide estar en Vanguardia ni (en reglas vanilla) interceptar en algunos casos.\n" +
                "Desaparece en tu próxima Preparación. Por eso a veces despliegas un turno y golpeas al siguiente.",
                "Recent = no atacar aún",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Finalizar turno",
                "No hagas más acciones. Pulsa Finalizar turno.\n" +
                "El Tutor jugará solo: despliegue y/o ataque legal, luego pasa.",
                "Pulsa Finalizar turno",
                "Listo, actuar",
                TutorialGoal.EndTurn),

            new Step(
                "Turno del Tutor",
                "Observa la mesa: el Tutor también respeta filas y PA.\n" +
                "Tu mano sigue privada. Cuando recuperes la prioridad en Main, continúa.",
                "Espera al Tutor…",
                "Seguir",
                TutorialGoal.ObserveTutor),

            new Step(
                "Por qué importa la fila",
                "Con Personajes en Vanguardia, el frente es el escudo del Héroe.\n" +
                "Si tu Vanguardia queda vacía, el rival puede ir a por Resolución del Héroe.\n" +
                "Retaguardia no ataca: úsala para el siguiente golpe o para no regalar objetivos baratos.",
                "Filas = amenaza y escudo",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Juega a Retaguardia",
                "1) Toca «Recluta de la Guardia» (PA 1).\n" +
                "2) Toca Retaguardia izquierda.\n" +
                "Queda detrás: no ataca aún, pero puedes reorganizarlo al frente cuando quieras gastar la reorg gratuita.",
                "Recluta → Retaguardia izq.",
                "Listo, actuar",
                TutorialGoal.PlayExact,
                requiredCardId: "WZ1-042",
                requiredRow: BoardRow.Rearguard,
                requiredSlotIndex: 0),

            new Step(
                "Reorganizar",
                "1 reorganización gratuita por turno (mover a casilla vacía de la otra fila o relevos permitidos).\n" +
                "1) Toca tu Recluta en Retaguardia.\n" +
                "2) Toca Vanguardia derecha (vacía).\n" +
                "Así el Recluta pasa a poder amenazar… tras perder Recent en un futuro turno si aplica.",
                "Recluta → Vanguardia der.",
                "Listo, actuar",
                TutorialGoal.ReorganizeExact,
                requiredBoardCardId: "WZ1-042",
                requiredRow: BoardRow.Vanguard,
                requiredSlotIndex: 1),

            new Step(
                "Combate",
                "Para declarar ataque:\n" +
                "• Atacante en Vanguardia, Ready, sin Recent\n" +
                "• Objetivo: Personaje rival en Vanguardia (o Héroe si no tiene Vanguardia)\n" +
                "Mayor Poder gana; empate entre Personajes → ambos K.O.\n" +
                "Tras atacar, el atacante queda Exhausted (no vuelve a atacar este turno).",
                "Ataque: Vanguardia lista",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Ataca",
                "Tu Iniciado ya no debería llevar Recent (pasó un turno).\n" +
                "1) Tócalo en Vanguardia.\n" +
                "2) Toca el Personaje del Tutor en Vanguardia.\n" +
                "Poder 2 vs 3: tu Iniciado cae. El combate también enseña a perder piezas.",
                "Iniciado ataca al Tutor",
                "Listo, actuar",
                TutorialGoal.AttackExact,
                requiredBoardCardId: "WZ1-018",
                requiredTargetCardId: "WZ1-050"),

            new Step(
                "Resolución",
                "La Resolución es la vida de escuela (aquí el Tutor tiene 1 para cerrar el guion).\n" +
                "A 0 = derrota. Golpear al Héroe solo es legal si su Vanguardia está vacía.\n" +
                "Vamos a limpiar el frente del Tutor y cerrar la partida.",
                "Siguiente: limpiar y ganar",
                "Siguiente",
                TutorialGoal.ReadAndContinue),

            new Step(
                "Pasar el turno",
                "Pulsa Finalizar turno. El Tutor jugará y puede atacar o defender — observa su turno.",
                "Pulsa Finalizar turno",
                "Listo, actuar",
                TutorialGoal.EndTurn),

            new Step(
                "Turno del Tutor",
                "El Tutor despliega y combate de verdad. Tú sigues abajo; él arriba — sin voltear la mesa.",
                "Espera al Tutor…",
                "Seguir",
                TutorialGoal.ObserveTutor),

            new Step(
                "Despliega al Custodio",
                "Juega «Custodio de Runas» (PA 2, Poder 3) a Vanguardia izquierda.\n" +
                "Entra con Recent: atacará el turno siguiente.",
                "Custodio → Vanguardia",
                "Listo, actuar",
                TutorialGoal.PlayExact,
                requiredCardId: "WZ1-019",
                requiredRow: BoardRow.Vanguard,
                requiredSlotIndex: 0),

            new Step(
                "Espera el Recent",
                "Pasa el turno. El Custodio perderá Recent en Preparación.",
                "Pulsa Finalizar turno",
                "Listo, actuar",
                TutorialGoal.EndTurn),

            new Step(
                "Respuesta del Tutor",
                "Si puede, el Tutor ataca o refuerza. Si cambió el tablero, adapta el siguiente golpe.",
                "Espera al Tutor…",
                "Seguir",
                TutorialGoal.ObserveTutor),

            new Step(
                "Empate decisivo",
                "1) Toca el Custodio (ya sin Recent).\n" +
                "2) Toca el Bruto del Tutor.\n" +
                "3 vs 3: caen los dos. La Vanguardia rival queda vacía.",
                "Custodio ataca al Bruto",
                "Listo, actuar",
                TutorialGoal.AttackExact,
                requiredBoardCardId: "WZ1-019",
                requiredTargetCardId: "WZ1-050"),

            new Step(
                "Acumula PA",
                "Necesitas PA 4 para el Guardián. Finaliza el turno.",
                "Pulsa Finalizar turno",
                "Listo, actuar",
                TutorialGoal.EndTurn),

            new Step(
                "Última respuesta",
                "El Tutor sigue luchando. Cuando recuperes la prioridad, cierra con el Guardián.",
                "Espera al Tutor…",
                "Seguir",
                TutorialGoal.ObserveTutor),

            new Step(
                "Golpeador en juego",
                "Juega «Guardián del Aliento» (PA 4, Poder 6) a Vanguardia.\n" +
                "Recent este turno — el golpe al Héroe será el siguiente.",
                "Guardián → Vanguardia",
                "Listo, actuar",
                TutorialGoal.PlayExact,
                requiredCardId: "WZ1-024",
                requiredRow: BoardRow.Vanguard,
                requiredSlotIndex: 0),

            new Step(
                "Prepara el remate",
                "Finaliza el turno para limpiar Recent del Guardián.",
                "Pulsa Finalizar turno",
                "Listo, actuar",
                TutorialGoal.EndTurn),

            new Step(
                "Última espera",
                "Cuando el Tutor termine, mira su Vanguardia. Si está vacía, el Héroe es legal.",
                "Espera al Tutor…",
                "Seguir",
                TutorialGoal.ObserveTutor),

            new Step(
                "Ataque al Héroe",
                "1) Toca tu Guardián (o el Personaje listo de mayor Poder).\n" +
                "2) Toca el Héroe del Tutor (carta grande arriba a la izquierda).\n" +
                "Con Resolución de escuela (1): si tu Poder supera al del Héroe, ganas.",
                "Personaje → Héroe Tutor",
                "Listo, actuar",
                TutorialGoal.AttackHeroExact,
                requiredBoardCardId: "WZ1-024"),

            new Step(
                "Victoria",
                "Si el motor ya cerró la partida, sigue. Si no, pulsa Continuar para no quedarte bloqueado.",
                "Cierre de partida",
                "Continuar",
                TutorialGoal.WaitVictory),

            new Step(
                "¡Has ganado!",
                "Completaste la escuela: tipos, filas, timing y una partida hasta la victoria.\n" +
                "Tu Héroe (Aedran) estuvo visible abajo a la izquierda en todo momento.\n" +
                "Vuelve al menú: Jugar libre, Cartas, o repite el Tutorial.",
                "Victoria del Aprendiz",
                "Volver al menú",
                TutorialGoal.Finish)
        };
    }
}
