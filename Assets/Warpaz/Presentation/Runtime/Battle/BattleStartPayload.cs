using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// DTO efímero de inicio de partida para la escena Battle.
    /// Contiene toda la info que UI necesita (nombres, decisión mulligan inicial y setup).
    /// </summary>
    public sealed class BattleStartPayload
    {
        public BattleStartPayload(
            MatchSetup matchSetup,
            MulliganDecision initialMulliganForPlayer0,
            bool debugVisibility,
            string player1Name,
            string player2Name)
        {
            MatchSetupValue = matchSetup;
            InitialMulliganForPlayer0 = initialMulliganForPlayer0;
            DebugVisibility = debugVisibility;
            Player1Name = player1Name ?? string.Empty;
            Player2Name = player2Name ?? string.Empty;
        }

        public MatchSetup MatchSetupValue { get; }

        /// <summary>
        /// Aplicado automáticamente cuando la fase actual es MulliganP0 (actor seat Player0).
        /// </summary>
        public MulliganDecision InitialMulliganForPlayer0 { get; }

        public bool DebugVisibility { get; }

        /// <summary>
        /// Nombre del prompt Player 1, que corresponde al seat Domain `PlayerId.Player0`.
        /// </summary>
        public string Player1Name { get; }

        /// <summary>
        /// Nombre del prompt Player 2, que corresponde al seat Domain `PlayerId.Player1`.
        /// </summary>
        public string Player2Name { get; }
    }
}
