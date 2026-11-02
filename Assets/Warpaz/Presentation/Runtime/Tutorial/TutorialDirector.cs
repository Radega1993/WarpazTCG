using UnityEngine;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Strict coach + input gate + auto tutor. Presentation only.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour, ITutorialInputGate
    {
        private static readonly PlayerId Learner = PlayerId.Player0;
        private static readonly PlayerId Tutor = PlayerId.Player1;

        private GameSession _session;
        private BattlePresenter _presenter;
        private TutorialCoachView _coach;
        private ISceneFlow _flow;
        private int _step;
        private bool _actionUnlocked;
        private float _tutorDelay;
        private int _eventIndexAtArm;
        private int _boardCountAtArm;
        private bool _finished;
        private bool _tutorTurnSeen;
        /// <summary>Scripted piece is gone and the step cannot be completed — Continuar skips it.</summary>
        private bool _scriptBypass;

        public int CurrentStepIndex => _step;

        public TutorialGoal CurrentGoalPublic => CurrentGoal();

        public bool IsActionUnlocked => _actionUnlocked;

        public bool IsFinished => _finished;

        public void Bind(
            GameSession session,
            BattlePresenter presenter,
            TutorialCoachView coach,
            ISceneFlow flow)
        {
            _session = session;
            _presenter = presenter;
            _coach = coach;
            _flow = flow;
            _step = 0;
            _actionUnlocked = false;
            _finished = false;
            _tutorTurnSeen = false;
            _scriptBypass = false;
            TutorialOpponentController.ResetScript();
            _coach?.Bind(OnPrimary, OnHelp, OnAbortTutorial);
            _presenter?.SetTutorialGate(this);
            EnterStep(0);
        }

        /// <summary>Test / capture harness: press coach primary.</summary>
        public void DebugClickPrimary()
        {
            OnPrimary();
        }

        private void OnAbortTutorial()
        {
            _finished = true;
            _presenter?.SetTutorialGate(null);
            _flow?.OpenMainMenu();
        }

        private void Update()
        {
            if (_finished || _session == null || _session.State == null || _presenter == null)
            {
                return;
            }

            // ObserveTutor / WaitVictory never unlock an action — still must poll.
            var goal = CurrentGoal();
            if (_actionUnlocked ||
                goal == TutorialGoal.ObserveTutor ||
                goal == TutorialGoal.WaitVictory ||
                goal == TutorialGoal.AttackExact ||
                goal == TutorialGoal.AttackHeroExact ||
                goal == TutorialGoal.ReorganizeExact)
            {
                TryAdvanceByGoal();
                RefreshScriptAdaptation();
            }

            TryTutorAct();
        }

        private void OnPrimary()
        {
            if (_finished || _step >= TutorialScript.Steps.Length)
            {
                _flow?.OpenMainMenu();
                return;
            }

            var step = TutorialScript.Steps[_step];
            if (step.Goal == TutorialGoal.Finish)
            {
                _finished = true;
                _flow?.OpenMainMenu();
                return;
            }

            if (_scriptBypass)
            {
                Advance();
                return;
            }

            if (step.Goal == TutorialGoal.ReadAndContinue ||
                step.Goal == TutorialGoal.ObserveTutor ||
                step.Goal == TutorialGoal.WaitVictory)
            {
                if (step.Goal == TutorialGoal.ObserveTutor && !_tutorTurnSeen)
                {
                    return;
                }

                Advance();
                return;
            }

            // Action steps: Listo → unlock+collapse; if already unlocked (Ayuda open), collapse again.
            if (step.IsActionGoal)
            {
                if (!_actionUnlocked)
                {
                    UnlockAction();
                }
                else
                {
                    ShowCurrent(expanded: false, forcePrimaryForAction: false);
                }
            }
        }

        private void OnHelp()
        {
            if (_finished)
            {
                return;
            }

            // Re-expand explanation; keep action unlocked.
            ShowCurrent(expanded: true, forcePrimaryForAction: true);
        }

        private void EnterStep(int index)
        {
            _step = index;
            _actionUnlocked = false;
            if (_step >= TutorialScript.Steps.Length)
            {
                _step = TutorialScript.Steps.Length - 1;
                _finished = true;
                ShowCurrent(expanded: true, forcePrimaryForAction: false);
                return;
            }

            var step = TutorialScript.Steps[_step];
            _eventIndexAtArm = _session.State != null ? _session.State.EventIndex : 0;
            _boardCountAtArm = CountBoard(Learner);
            _tutorDelay = 0.45f;
            _scriptBypass = false;

            if (step.Goal == TutorialGoal.ObserveTutor)
            {
                _tutorTurnSeen = false;
            }

            // Auto-unlock pure action goals after a short read? Prefer explicit "Listo, actuar".
            // MulliganKeep unlocks immediately (panel is the action).
            if (step.Goal == TutorialGoal.MulliganKeep)
            {
                UnlockAction();
                return;
            }

            ShowCurrent(expanded: true, forcePrimaryForAction: step.IsActionGoal);
            RefreshScriptAdaptation();
        }

        private void UnlockAction()
        {
            _actionUnlocked = true;
            _eventIndexAtArm = _session.State != null ? _session.State.EventIndex : 0;
            _boardCountAtArm = CountBoard(Learner);
            ShowCurrent(expanded: false, forcePrimaryForAction: false);
            _presenter?.Render();
        }

        private void TryAdvanceByGoal()
        {
            var step = TutorialScript.Steps[_step];
            var state = _session.State;
            switch (step.Goal)
            {
                case TutorialGoal.MulliganKeep:
                    // Wait until both seats finished mulligan (P0 keep moves to MulliganP1).
                    if (state.Phase != MatchPhase.MulliganP0 &&
                        state.Phase != MatchPhase.MulliganP1)
                    {
                        Advance();
                    }

                    break;
                case TutorialGoal.PlayExact:
                    if (CountBoard(Learner) > _boardCountAtArm)
                    {
                        Advance();
                    }

                    break;
                case TutorialGoal.EndTurn:
                    if (LearnerEndedTurnSinceArm())
                    {
                        Advance();
                    }

                    break;
                case TutorialGoal.ObserveTutor:
                    if (!_tutorTurnSeen &&
                        state.PriorityPlayerId == Learner &&
                        state.Phase == MatchPhase.Main)
                    {
                        _tutorTurnSeen = true;
                        // Expand coach with "Seguir" so the learner can leave this step.
                        ShowCurrent(expanded: true, forcePrimaryForAction: false);
                        _presenter?.Render();
                    }

                    break;
                case TutorialGoal.ReorganizeExact:
                    if (HasCardOnSlot(Learner, step.RequiredBoardCardId, step.RequiredRow, step.RequiredSlotIndex))
                    {
                        Advance();
                    }

                    break;
                case TutorialGoal.AttackExact:
                    if (LearnerDeclaredAttackSinceArm())
                    {
                        Advance();
                    }

                    break;
                case TutorialGoal.AttackHeroExact:
                    // Clearing blockers must NOT complete this step — only a hero swing does.
                    if (LearnerDeclaredHeroAttackSinceArm())
                    {
                        Advance();
                    }
                    else if (state.Result != null && state.Result.IsFinished)
                    {
                        Advance();
                    }

                    break;
                case TutorialGoal.WaitVictory:
                    if (state.Result != null && state.Result.IsFinished)
                    {
                        Advance();
                    }
                    else if (!_actionUnlocked)
                    {
                        // Never leave the player staring at a stuck "resolving" panel.
                        _actionUnlocked = true;
                        ShowCurrent(expanded: true, forcePrimaryForAction: false);
                    }

                    break;
            }
        }

        private void TryTutorAct()
        {
            var state = _session.State;
            if (state.PriorityPlayerId != Tutor)
            {
                return;
            }

            _tutorDelay -= Time.unscaledDeltaTime;
            if (_tutorDelay > 0f)
            {
                return;
            }

            _tutorDelay = 0.55f;
            if (TutorialOpponentController.TryAct(_session, Tutor))
            {
                _presenter.NotifyExternalCommand(Learner);
            }
        }

        private void Advance()
        {
            EnterStep(_step + 1);
        }

        private void ShowCurrent(bool expanded, bool forcePrimaryForAction)
        {
            var steps = TutorialScript.Steps;
            if (_step < 0 || _step >= steps.Length)
            {
                return;
            }

            var s = steps[_step];
            var title = s.Title;
            var body = s.Body;
            var hint = s.CompactHint;
            var primaryVisible = s.Goal == TutorialGoal.ReadAndContinue ||
                                 s.Goal == TutorialGoal.ObserveTutor ||
                                 s.Goal == TutorialGoal.WaitVictory ||
                                 s.Goal == TutorialGoal.Finish ||
                                 forcePrimaryForAction;
            string label;
            if (s.Goal == TutorialGoal.Finish)
            {
                label = "Volver al menú";
            }
            else if (s.Goal == TutorialGoal.WaitVictory || string.IsNullOrEmpty(s.PrimaryLabel))
            {
                label = "Continuar";
            }
            else
            {
                label = s.PrimaryLabel;
            }

            if (_scriptBypass)
            {
                title = "El Tutor cambió el tablero";
                body = "Tu pieza del guion ya no está (el Tutor la eliminó o la cambió).\n" +
                       "Pulsa Continuar para seguir la partida — el Tutor seguirá jugando de verdad.";
                hint = "Pieza perdida — Continuar";
                label = "Continuar";
                primaryVisible = true;
                expanded = true;
            }
            else if (s.Goal == TutorialGoal.AttackHeroExact && TutorHasOccupiedVanguard())
            {
                title = "Limpia la Vanguardia";
                body = "No puedes golpear al Héroe mientras el Tutor tenga Personajes en Vanguardia.\n" +
                       "1) Ataca a un Personaje rival en Vanguardia.\n" +
                       "2) Si tu atacante queda Exhausted, Finaliza turno y vuelve a intentarlo.\n" +
                       "3) Con Vanguardia rival vacía, toca el Héroe del Tutor.";
                hint = "Limpia Vanguardia → Héroe";
            }
            else if (IsAttackStepWithAlternateBoard(s))
            {
                title = "Adapta el ataque";
                body = "El Tutor cambió el frente. Ataca con cualquier Personaje listo tuyo " +
                       "contra un objetivo legal del Tutor.";
                hint = "Ataca con lo que tengas listo";
            }

            if (s.Goal == TutorialGoal.ObserveTutor && !_tutorTurnSeen && !_scriptBypass)
            {
                primaryVisible = false;
                expanded = false;
            }

            _coach?.Render(
                _step,
                steps.Length,
                title,
                body,
                hint,
                label,
                primaryVisible,
                expanded);
        }

        private void RefreshScriptAdaptation()
        {
            var step = TutorialScript.Steps[_step];
            if (step.Goal != TutorialGoal.AttackExact &&
                step.Goal != TutorialGoal.AttackHeroExact &&
                step.Goal != TutorialGoal.ReorganizeExact)
            {
                if (_scriptBypass)
                {
                    _scriptBypass = false;
                }

                return;
            }

            // AttackHero blocked by Tutor Vanguard — keep acting, refresh coach copy.
            if (step.Goal == TutorialGoal.AttackHeroExact &&
                TutorHasOccupiedVanguard() &&
                CountBoard(Learner) > 0)
            {
                if (_scriptBypass)
                {
                    _scriptBypass = false;
                }

                if (!_actionUnlocked)
                {
                    UnlockAction();
                }
                else
                {
                    ShowCurrent(expanded: false, forcePrimaryForAction: false);
                }

                return;
            }

            if (string.IsNullOrEmpty(step.RequiredBoardCardId) || LearnerStillHas(step.RequiredBoardCardId))
            {
                if (_scriptBypass)
                {
                    _scriptBypass = false;
                    ShowCurrent(expanded: false, forcePrimaryForAction: false);
                }

                return;
            }

            // Required piece is gone.
            if (step.Goal == TutorialGoal.ReorganizeExact || CountBoard(Learner) == 0)
            {
                if (!_scriptBypass)
                {
                    _scriptBypass = true;
                    _actionUnlocked = false;
                    ShowCurrent(expanded: true, forcePrimaryForAction: false);
                    _presenter?.Render();
                }

                return;
            }

            // Attack still possible with another board piece — keep gate soft, refresh copy.
            if (_scriptBypass)
            {
                _scriptBypass = false;
            }

            if (!_actionUnlocked)
            {
                UnlockAction();
            }
            else
            {
                ShowCurrent(expanded: false, forcePrimaryForAction: false);
            }
        }

        private bool IsAttackStepWithAlternateBoard(TutorialScript.Step s)
        {
            if (s.Goal != TutorialGoal.AttackExact && s.Goal != TutorialGoal.AttackHeroExact)
            {
                return false;
            }

            if (string.IsNullOrEmpty(s.RequiredBoardCardId) || LearnerStillHas(s.RequiredBoardCardId))
            {
                return false;
            }

            return CountBoard(Learner) > 0;
        }

        // ---- ITutorialInputGate ----

        public bool AllowMulligan(MulliganDecision decision)
        {
            return CurrentGoal() == TutorialGoal.MulliganKeep &&
                   _actionUnlocked &&
                   decision == MulliganDecision.KeepHand;
        }

        public bool AllowSelectHand(CardInstanceId instanceId)
        {
            if (!_actionUnlocked || CurrentGoal() != TutorialGoal.PlayExact)
            {
                return false;
            }

            return CardIdEquals(instanceId, TutorialScript.Steps[_step].RequiredCardId);
        }

        public bool AllowPlay(CardInstanceId instanceId, BoardRow row, int slotIndex)
        {
            if (!_actionUnlocked || CurrentGoal() != TutorialGoal.PlayExact)
            {
                return false;
            }

            var s = TutorialScript.Steps[_step];
            return CardIdEquals(instanceId, s.RequiredCardId) &&
                   row == s.RequiredRow &&
                   slotIndex == s.RequiredSlotIndex;
        }

        public bool AllowSelectOwnBoard(CardInstanceId instanceId)
        {
            if (!_actionUnlocked)
            {
                return false;
            }

            var goal = CurrentGoal();
            if (goal == TutorialGoal.ReorganizeExact)
            {
                return CardIdEquals(instanceId, TutorialScript.Steps[_step].RequiredBoardCardId);
            }

            if (goal == TutorialGoal.AttackHeroExact)
            {
                // Clearing Tutor Vanguard: any ready Character. Hero swing: prefer scripted piece.
                if (TutorHasOccupiedVanguard())
                {
                    return true;
                }

                var requiredHero = TutorialScript.Steps[_step].RequiredBoardCardId;
                return CardIdEquals(instanceId, requiredHero) ||
                       !LearnerStillHas(requiredHero);
            }

            if (goal == TutorialGoal.AttackExact)
            {
                var required = TutorialScript.Steps[_step].RequiredBoardCardId;
                if (CardIdEquals(instanceId, required))
                {
                    return true;
                }

                // Tutor may have traded the scripted attacker — any own board card is fine.
                return !LearnerStillHas(required);
            }

            return false;
        }

        private bool LearnerStillHas(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _session.State == null)
            {
                return false;
            }

            var player = _session.State.GetPlayer(Learner);
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (slot.IsOccupied && CardIdEquals(slot.Occupant, cardId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool AllowReorganize(CardInstanceId fromId, BoardRow row, int slotIndex)
        {
            if (!_actionUnlocked || CurrentGoal() != TutorialGoal.ReorganizeExact)
            {
                return false;
            }

            var s = TutorialScript.Steps[_step];
            return CardIdEquals(fromId, s.RequiredBoardCardId) &&
                   row == s.RequiredRow &&
                   slotIndex == s.RequiredSlotIndex;
        }

        public bool AllowAttack(CardInstanceId attackerId, CardInstanceId targetId)
        {
            if (!_actionUnlocked)
            {
                return false;
            }

            var goal = CurrentGoal();
            var s = TutorialScript.Steps[_step];

            // During the hero finish, allow clearing Tutor Vanguard first.
            if (goal == TutorialGoal.AttackHeroExact)
            {
                if (!TutorHasOccupiedVanguard())
                {
                    return false;
                }

                return CardIdEquals(attackerId, s.RequiredBoardCardId) ||
                       !LearnerStillHas(s.RequiredBoardCardId) ||
                       CountBoard(Learner) > 0;
            }

            if (goal != TutorialGoal.AttackExact)
            {
                return false;
            }

            var attackerOk = CardIdEquals(attackerId, s.RequiredBoardCardId) ||
                             !LearnerStillHas(s.RequiredBoardCardId);
            if (!attackerOk)
            {
                return false;
            }

            // Prefer the scripted target; if the Tutor already traded it away, any legal target works.
            if (CardIdEquals(targetId, s.RequiredTargetCardId))
            {
                return true;
            }

            return !OpponentStillHas(s.RequiredTargetCardId);
        }

        private bool OpponentStillHas(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _session.State == null)
            {
                return false;
            }

            var player = _session.State.GetPlayer(Tutor);
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (slot.IsOccupied && CardIdEquals(slot.Occupant, cardId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool AllowAttackHero(CardInstanceId attackerId)
        {
            if (!_actionUnlocked || CurrentGoal() != TutorialGoal.AttackHeroExact)
            {
                return false;
            }

            if (TutorHasOccupiedVanguard())
            {
                return false;
            }

            var required = TutorialScript.Steps[_step].RequiredBoardCardId;
            return CardIdEquals(attackerId, required) ||
                   !LearnerStillHas(required) ||
                   CountBoard(Learner) > 0;
        }

        public bool AllowEndTurn()
        {
            if (!_actionUnlocked)
            {
                return false;
            }

            var goal = CurrentGoal();
            if (goal == TutorialGoal.EndTurn)
            {
                return true;
            }

            // After clearing a blocker the attacker is Exhausted — need another turn for the hero swing.
            if (goal == TutorialGoal.AttackHeroExact)
            {
                return TutorHasOccupiedVanguard() || !HasLegalHeroAttack();
            }

            return false;
        }

        private bool TutorHasOccupiedVanguard()
        {
            if (_session?.State == null)
            {
                return false;
            }

            var tutor = _session.State.GetPlayer(Tutor);
            for (var i = 0; i < tutor.BoardSlots.Count; i++)
            {
                var slot = tutor.BoardSlots[i];
                if (slot.IsVanguard && slot.IsOccupied)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasLegalHeroAttack()
        {
            if (_session == null || TutorHasOccupiedVanguard())
            {
                return false;
            }

            var actions = LegalActionQuery.For(_session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.ActorId == Learner && a.Type == CommandType.Attack && a.TargetsHero)
                {
                    return true;
                }
            }

            return false;
        }

        private TutorialGoal CurrentGoal()
        {
            if (_step < 0 || _step >= TutorialScript.Steps.Length)
            {
                return TutorialGoal.Finish;
            }

            return TutorialScript.Steps[_step].Goal;
        }

        private bool CardIdEquals(CardInstanceId instanceId, string cardId)
        {
            if (instanceId.IsNone || string.IsNullOrEmpty(cardId) || _session.State == null)
            {
                return false;
            }

            if (!_session.State.TryGetCard(instanceId, out var card))
            {
                return false;
            }

            return card.DefinitionId.Value == cardId;
        }

        private int CountBoard(PlayerId playerId)
        {
            var player = _session.State.GetPlayer(playerId);
            var n = 0;
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                if (player.BoardSlots[i].IsOccupied)
                {
                    n++;
                }
            }

            return n;
        }

        private bool HasCardOnSlot(PlayerId playerId, string cardId, BoardRow row, int slotIndex)
        {
            var player = _session.State.GetPlayer(playerId);
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (slot.Row != row || slot.Index != slotIndex || !slot.IsOccupied)
                {
                    continue;
                }

                return CardIdEquals(slot.Occupant, cardId);
            }

            return false;
        }

        private bool LearnerDeclaredAttackSinceArm()
        {
            var events = _session.State.EventLog;
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.EventIndex >= _eventIndexAtArm &&
                    e.Type == GameEventType.AttackDeclared &&
                    e.ActorId == Learner)
                {
                    return true;
                }
            }

            return false;
        }

        private bool LearnerDeclaredHeroAttackSinceArm()
        {
            var events = _session.State.EventLog;
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.EventIndex >= _eventIndexAtArm &&
                    e.Type == GameEventType.AttackDeclared &&
                    e.ActorId == Learner &&
                    e.Payload != null &&
                    e.Payload.IndexOf(":Hero:", System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool LearnerEndedTurnSinceArm()
        {
            var events = _session.State.EventLog;
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.EventIndex >= _eventIndexAtArm &&
                    e.Type == GameEventType.TurnEnded &&
                    e.ActorId == Learner)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
