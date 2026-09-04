using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>
    /// Two legal WZ1 seats ready for <see cref="MatchFactory"/>. No CardDefinition copies.
    /// </summary>
    public sealed class Sprint1MatchFixture
    {
        public Sprint1MatchFixture(
            ICardCatalog catalog,
            Sprint1MatchFixtureOptions options,
            PlayerSetup player0,
            PlayerSetup player1)
        {
            Catalog = catalog;
            Options = options;
            Player0 = player0;
            Player1 = player1;
        }

        public ICardCatalog Catalog { get; }

        public Sprint1MatchFixtureOptions Options { get; }

        public ulong Seed => Options.Seed;

        public string Player1Name => Options.Player1Name;

        public string Player2Name => Options.Player2Name;

        public MulliganDecision MulliganDecision => Options.MulliganDecision;

        public bool DebugVisibility => Options.DebugVisibility;

        public PlayerSetup Player0 { get; }

        public PlayerSetup Player1 { get; }

        public MatchSetup ToMatchSetup()
        {
            return new MatchSetup(
                Options.Seed,
                Options.Rules,
                Catalog.ContentVersion,
                Player0,
                Player1);
        }

        /// <summary>Every identity and main-deck slot the fixture referenced, in order.</summary>
        public IReadOnlyList<CardId> ReferencedCardIds()
        {
            var ids = new List<CardId>(4 + Player0.MainDeckCardIds.Count + Player1.MainDeckCardIds.Count);
            AddPlayer(ids, Player0);
            AddPlayer(ids, Player1);
            return ids;
        }

        private static void AddPlayer(List<CardId> ids, PlayerSetup player)
        {
            ids.Add(player.HeroCardId);
            ids.Add(player.ClassCardId);
            var deck = player.MainDeckCardIds;
            for (var i = 0; i < deck.Count; i++)
            {
                ids.Add(deck[i]);
            }
        }
    }
}
