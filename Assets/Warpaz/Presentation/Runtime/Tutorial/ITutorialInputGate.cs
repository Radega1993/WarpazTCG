using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Presentation gate: further restricts already-legal Domain actions for the guided tutorial.
    /// Never invents legality — only denies extras.
    /// </summary>
    public interface ITutorialInputGate
    {
        bool AllowMulligan(MulliganDecision decision);

        bool AllowSelectHand(CardInstanceId instanceId);

        bool AllowPlay(CardInstanceId instanceId, BoardRow row, int slotIndex);

        bool AllowSelectOwnBoard(CardInstanceId instanceId);

        bool AllowReorganize(CardInstanceId fromId, BoardRow row, int slotIndex);

        bool AllowAttack(CardInstanceId attackerId, CardInstanceId targetId);

        bool AllowAttackHero(CardInstanceId attackerId);

        bool AllowEndTurn();
    }
}
