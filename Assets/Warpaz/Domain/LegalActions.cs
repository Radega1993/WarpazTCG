using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Legal Sprint 1 actions for the current phase. Application only forwards this list.
    /// </summary>
    public static class LegalActions
    {
        public static IReadOnlyList<LegalAction> For(GameState state)
        {
            if (state == null)
            {
                return Array.Empty<LegalAction>();
            }

            switch (state.Phase)
            {
                case MatchPhase.MulliganP0:
                    return MulliganFor(PlayerId.Player0);
                case MatchPhase.MulliganP1:
                    return MulliganFor(PlayerId.Player1);
                case MatchPhase.Main:
                    return new[]
                    {
                        LegalAction.EndTurn(state.ActivePlayerId, "EndTurn")
                    };
                default:
                    return Array.Empty<LegalAction>();
            }
        }

        private static IReadOnlyList<LegalAction> MulliganFor(PlayerId actorId)
        {
            return new[]
            {
                LegalAction.ConfirmMulligan(actorId, MulliganDecision.KeepHand, "KeepHand"),
                LegalAction.ConfirmMulligan(actorId, MulliganDecision.ReplaceWholeHand, "ReplaceWholeHand")
            };
        }
    }
}
