using NUnit.Framework;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class MatchViewProjectorTests
    {
        [Test]
        public void OwnView_ContainsHand_OpponentView_OnlyHandCount()
        {
            var session = MatchFixtures.Started(5UL);
            var own = session.Project(PlayerId.Player0);
            var rival = session.Project(PlayerId.Player1);

            Assert.AreEqual(5, own.Player0.HandCount);
            Assert.AreEqual(5, own.Player0.VisibleHand.Count);
            Assert.AreEqual(5, own.Player1.HandCount);
            Assert.AreEqual(0, own.Player1.VisibleHand.Count);

            Assert.AreEqual(5, rival.Player1.VisibleHand.Count);
            Assert.AreEqual(0, rival.Player0.VisibleHand.Count);
            Assert.AreEqual(own.Player0.HandCount, rival.Player0.HandCount);
        }

        [Test]
        public void NormalView_FiltersOwnerOnlyEvents()
        {
            var session = MatchFixtures.Started(5UL);
            var view = GameStateViewProjector.Project(session.State, PlayerId.Player0);

            var ownerDraws = 0;
            var opponentDraws = 0;
            for (var i = 0; i < view.Events.Count; i++)
            {
                var evt = view.Events[i];
                if (evt.Type != GameEventType.CardDrawn)
                {
                    continue;
                }

                Assert.AreEqual(GameEventVisibility.OwnerOnly, evt.Visibility);
                Assert.AreEqual(PlayerId.Player0, evt.ActorId);
                ownerDraws++;
                if (evt.ActorId == PlayerId.Player1)
                {
                    opponentDraws++;
                }
            }

            Assert.AreEqual(5, ownerDraws);
            Assert.AreEqual(0, opponentDraws);
        }

        [Test]
        public void NormalView_HasNoDeckOrderOrRng_AndIsNotDebug()
        {
            var session = MatchFixtures.Started(5UL);
            var view = session.Project(0);

            Assert.IsFalse(view.IsDebug);
            Assert.IsNull(view.Debug);
            Assert.AreEqual(session.CurrentHash, view.Hash);
            Assert.Greater(view.Player0.DeckCount, 0);
            Assert.AreEqual(typeof(PlayerMatchView).GetProperty("DeckOrder"), null);
            Assert.AreEqual(typeof(MatchView).GetProperty("RngState"), null);
        }

        [Test]
        public void DebugView_RequiresExplicitMode()
        {
            var session = MatchFixtures.Started(5UL);
            var normal = session.Project(PlayerId.Player0, MatchViewMode.Normal);
            var debug = session.Project(PlayerId.Player0, MatchViewMode.Debug);

            Assert.IsFalse(normal.IsDebug);
            Assert.IsNull(normal.Debug);

            Assert.IsTrue(debug.IsDebug);
            Assert.IsNotNull(debug.Debug);
            Assert.IsFalse(string.IsNullOrEmpty(debug.Debug.RngState));
            Assert.AreEqual(session.State.GetPlayer(PlayerId.Player0).DeckInstanceIds.Count, debug.Debug.Player0DeckOrder.Count);
            Assert.AreEqual(5, debug.Debug.Player0Hand.Count);
            Assert.AreEqual(5, debug.Debug.Player1Hand.Count);
            Assert.Greater(debug.Debug.UnfilteredEvents.Count, normal.Events.Count);
        }

        [Test]
        public void Discard_IsPublicToBothViewers()
        {
            var session = MatchFixtures.Started(5UL);
            RulesEngine.MoveDeckToDiscard(session.State, PlayerId.Player0);
            var discarded = session.State.GetPlayer(PlayerId.Player0).DiscardInstanceIds.Count;

            var own = session.Project(PlayerId.Player0);
            var rival = session.Project(PlayerId.Player1);
            Assert.AreEqual(discarded, own.Player0.Discard.Count);
            Assert.AreEqual(discarded, rival.Player0.Discard.Count);
            Assert.Greater(discarded, 0);
        }

        [Test]
        public void BoardSlots_AreProjectedForBothPlayers()
        {
            var session = MatchFixtures.Started(5UL);
            var view = session.Project(PlayerId.Player0);

            Assert.AreEqual(4, view.Player0.BoardSlots.Count);
            Assert.AreEqual(4, view.Player1.BoardSlots.Count);
            Assert.AreEqual(BoardRow.Vanguard, view.Player0.BoardSlots[0].Row);
            Assert.AreEqual(0, view.Player0.BoardSlots[0].Index);
            Assert.IsFalse(view.Player0.BoardSlots[0].IsOccupied);
        }

        [Test]
        public void EventLogProjector_DebugIncludesHiddenDraws()
        {
            var session = MatchFixtures.Started(5UL);
            var normal = EventLogProjector.Project(session.State, PlayerId.Player0, MatchViewMode.Normal);
            var debug = EventLogProjector.Project(session.State, PlayerId.Player0, MatchViewMode.Debug);

            Assert.Greater(debug.Count, normal.Count);
            var hidden = 0;
            for (var i = 0; i < debug.Count; i++)
            {
                if (debug[i].Type == GameEventType.CardDrawn && debug[i].ActorId == PlayerId.Player1)
                {
                    hidden++;
                }
            }

            Assert.AreEqual(5, hidden);
        }
    }
}
