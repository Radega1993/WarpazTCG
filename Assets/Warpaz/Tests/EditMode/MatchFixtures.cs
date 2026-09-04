using System.Collections.Generic;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    internal static class MatchFixtures
    {
        public static readonly MinimalCatalog Catalog = MinimalCatalog.Create();

        public static MatchSetup Standard(ulong seed, RuleConfig rules = null)
        {
            var resolved = rules ?? RuleConfig.AlphaDefaults();
            return new MatchSetup(
                seed,
                resolved,
                Catalog.ContentVersion,
                Player(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, MinimalCatalog.StandardDeck40()),
                Player(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, MinimalCatalog.StandardDeck40()));
        }

        public static PlayerSetup Player(
            PlayerId playerId,
            CardId heroId,
            CardId classCardId,
            IReadOnlyList<CardId> mainDeck)
        {
            return new PlayerSetup(playerId, heroId, classCardId, mainDeck);
        }

        public static GameSession Started(ulong seed = 1UL)
        {
            var session = new GameSession(Catalog);
            var result = session.Start(Standard(seed));
            if (!result.Accepted)
            {
                throw new InvariantViolationException(result.RejectionReason);
            }

            return session;
        }

        public static IReadOnlyList<CardId> DeckOfSize(int size)
        {
            var deck = new List<CardId>(size);
            var source = MinimalCatalog.StandardDeck40();
            for (var i = 0; i < size; i++)
            {
                deck.Add(source[i % source.Count]);
            }

            return deck;
        }
    }
}
