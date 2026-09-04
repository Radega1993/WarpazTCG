using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.PlayMode
{
    public sealed class FiveTurnSessionTests
    {
        [Test]
        public void GameSession_CompletesFiveTurnsWithoutHud()
        {
            var session = new GameSession(MinimalCatalog.Create());
            var setup = new MatchSetup(
                21UL,
                RuleConfig.AlphaDefaults(),
                MinimalCatalog.ContentVersionValue,
                new PlayerSetup(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, MinimalCatalog.StandardDeck40()),
                new PlayerSetup(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, MinimalCatalog.StandardDeck40()));

            Assert.IsTrue(session.Start(setup).Accepted);
            Assert.IsTrue(session.ConfirmMulligan(0, true).Accepted);
            Assert.IsTrue(session.ConfirmMulligan(1, true).Accepted);

            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(session.EndTurn(session.State.ActivePlayerId).Accepted);
            }

            Assert.AreEqual(MatchPhase.Main, session.State.Phase);
            Assert.AreEqual(6, session.State.Turn);
        }
    }
}
