using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Read-only catalog keyed exclusively by <see cref="CardId"/>.
    /// Names are display-only and must never be used as keys.
    /// </summary>
    public interface ICardCatalog
    {
        string ContentVersion { get; }

        int Count { get; }

        IReadOnlyList<CardDefinition> All { get; }

        bool TryGet(CardId cardId, out CardDefinition definition);

        IReadOnlyList<CardDefinition> Filter(
            CardType? type = null,
            string race = null,
            string classPath = null,
            int? costPa = null,
            string rarity = null,
            string keyword = null);
    }
}
