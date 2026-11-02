using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Auto-plays the tutor seat with legal GameSession commands only.
    /// Deploys and occasionally trades, but never wipes the learner's last scripted piece.
    /// </summary>
    public static class TutorialOpponentController
    {
        /// <summary>Pieces the guided script still needs on the learner board.</summary>
        private static readonly string[] ProtectedLearnerCards =
        {
            "WZ1-018", // Iniciado
            "WZ1-019", // Custodio
            "WZ1-024"  // Guardián
        };

        private static int _playsThisTurn;
        private static int _attacksThisTurn;
        private static int _lastTurnSeen = -1;

        public static void ResetScript()
        {
            _playsThisTurn = 0;
            _attacksThisTurn = 0;
            _lastTurnSeen = -1;
        }

        public static bool TryAct(GameSession session, PlayerId tutorSeat)
        {
            if (session == null || session.State == null)
            {
                return false;
            }

            var state = session.State;
            if (state.PriorityPlayerId != tutorSeat)
            {
                return false;
            }

            if (state.Phase == MatchPhase.MulliganP0 || state.Phase == MatchPhase.MulliganP1)
            {
                var mull = session.ConfirmMulligan(tutorSeat, MulliganDecision.KeepHand);
                return mull.Accepted;
            }

            if (state.Phase == MatchPhase.PendingChoice)
            {
                if (TryIntercept(session, tutorSeat))
                {
                    return true;
                }

                var decline = session.DeclineInterception(tutorSeat);
                return decline.Accepted;
            }

            if (state.Phase != MatchPhase.Main || state.ActivePlayerId != tutorSeat)
            {
                return false;
            }

            if (state.Turn != _lastTurnSeen)
            {
                _lastTurnSeen = state.Turn;
                _playsThisTurn = 0;
                _attacksThisTurn = 0;
            }

            // Keep at most ONE character — a full Tutor board blocks AttackHero forever.
            const int maxTutorBoard = 1;
            if (CountBoard(state, tutorSeat) < maxTutorBoard &&
                _playsThisTurn < 1 &&
                TryPlayBest(session, tutorSeat))
            {
                _playsThisTurn++;
                return true;
            }

            // One careful trade per turn — never delete the last learner piece / script keys.
            if (_attacksThisTurn < 1 && TryAttackCharacter(session, tutorSeat))
            {
                _attacksThisTurn++;
                return true;
            }

            var end = session.EndTurn(tutorSeat);
            return end.Accepted;
        }

        private static bool TryIntercept(GameSession session, PlayerId tutorSeat)
        {
            var actions = LegalActionQuery.For(session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.ActorId != tutorSeat || a.Type != CommandType.ChooseTargets)
                {
                    continue;
                }

                if (a.PrimaryInstanceId.IsNone)
                {
                    continue;
                }

                var result = session.ChooseInterceptor(tutorSeat, a.PrimaryInstanceId);
                if (result.Accepted)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryPlayBest(GameSession session, PlayerId tutorSeat)
        {
            var actions = LegalActionQuery.For(session);
            LegalAction best = null;
            var bestScore = int.MinValue;
            const string preferredId = "WZ1-050";

            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.ActorId != tutorSeat || a.Type != CommandType.Play || a.PrimaryInstanceId.IsNone)
                {
                    continue;
                }

                if (!session.State.TryGetCard(a.PrimaryInstanceId, out var card))
                {
                    continue;
                }

                var score = 10;
                if (card.DefinitionId.Value == preferredId)
                {
                    score += 100;
                }

                // Prefer a single Vanguard demo piece (WZ1-050), never flood Rearguard.
                if (a.TargetRow == BoardRow.Vanguard && a.TargetSlotIndex == 0)
                {
                    score += 40;
                }
                else if (a.TargetRow == BoardRow.Rearguard)
                {
                    score -= 50;
                }

                score += 40 - EstimatePlayOrder(card.DefinitionId.Value) % 40;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }

            if (best == null)
            {
                return false;
            }

            var result = session.Play(tutorSeat, best.PrimaryInstanceId, best.TargetRow, best.TargetSlotIndex);
            return result.Accepted;
        }

        private static bool TryAttackCharacter(GameSession session, PlayerId tutorSeat)
        {
            var learnerBoard = CountBoard(session.State, PlayerId.Player0);
            // Leave the learner with something on board so scripted AttackExact stays possible.
            if (learnerBoard <= 1)
            {
                return false;
            }

            var actions = LegalActionQuery.For(session);
            LegalAction best = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.ActorId != tutorSeat || a.Type != CommandType.Attack || a.TargetsHero)
                {
                    continue;
                }

                if (a.PrimaryInstanceId.IsNone || a.SecondaryInstanceId.IsNone)
                {
                    continue;
                }

                if (!session.State.TryGetCard(a.PrimaryInstanceId, out var attacker) ||
                    !session.State.TryGetCard(a.SecondaryInstanceId, out var target))
                {
                    continue;
                }

                var targetId = target.DefinitionId.Value;
                if (IsProtected(targetId))
                {
                    continue;
                }

                var atk = PowerOf(session.Catalog, attacker.DefinitionId);
                var def = PowerOf(session.Catalog, target.DefinitionId);
                // Prefer winning / even trades only — don't kamikaze into strong blockers.
                if (atk < def)
                {
                    continue;
                }

                var score = (atk - def) * 10 + atk;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }

            if (best == null)
            {
                return false;
            }

            var result = session.Attack(tutorSeat, best.PrimaryInstanceId, best.SecondaryInstanceId);
            return result.Accepted;
        }

        private static bool IsProtected(string cardId)
        {
            for (var i = 0; i < ProtectedLearnerCards.Length; i++)
            {
                if (ProtectedLearnerCards[i] == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountBoard(GameState state, PlayerId seat)
        {
            if (state == null)
            {
                return 0;
            }

            var player = state.GetPlayer(seat);
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

        private static int EstimatePlayOrder(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || cardId.Length < 7)
            {
                return 999;
            }

            int n;
            return int.TryParse(cardId.Substring(4), out n) ? n : 999;
        }

        private static int PowerOf(ICardCatalog catalog, CardId id)
        {
            if (catalog == null || !catalog.TryGet(id, out var def))
            {
                return 0;
            }

            return CombatPolicies.EffectivePower(def);
        }
    }
}
