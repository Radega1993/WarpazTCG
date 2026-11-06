using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>Identity of a card in a match view. No zone lists or RNG.</summary>
    public sealed class CardView
    {
        public CardView(CardInstanceId instanceId, CardId cardId, int powerModifier = 0)
        {
            InstanceId = instanceId;
            CardId = cardId;
            PowerModifier = powerModifier;
        }

        public CardInstanceId InstanceId { get; }

        public CardId CardId { get; }
        public int PowerModifier { get; }
    }
}
