using System;
using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Presentation-facing legal-action list. All legality comes from Domain.
    /// </summary>
    public static class LegalActionQuery
    {
        public static IReadOnlyList<LegalAction> For(GameState state)
        {
            return LegalActions.For(state);
        }

        public static IReadOnlyList<LegalAction> For(GameSession session)
        {
            if (session == null)
            {
                return Array.Empty<LegalAction>();
            }

            return LegalActions.For(session.State);
        }
    }
}
