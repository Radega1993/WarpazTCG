using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class MulliganTests
    {
        [Test]
        public void Mulligan_Keep_LeavesSameHandInstanceIds()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(3UL))).State;
            var before = state.Players[0].Hand.ToArray();

            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, MulliganDecision.KeepHand));

            Assert.IsTrue(result.Accepted, result.RejectionReason);
            CollectionAssert.AreEqual(before, result.State.Players[0].Hand);
            Assert.AreEqual(MatchPhase.MulliganP1, result.State.Phase);
        }

        [Test]
        public void Mulligan_Replace_KeepsFortyInstancesAndFiveSevenSplit()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(4UL))).State;
            var before = state.Players[0].Hand.ToArray();

            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, MulliganDecision.ReplaceWholeHand));

            Assert.IsTrue(result.Accepted, result.RejectionReason);
            var player = result.State.Players[0];
            Assert.AreEqual(5, player.Hand.Count);
            Assert.AreEqual(35, player.Deck.Count);
            Assert.AreEqual(40, player.Deck.Count + player.Hand.Count + player.Discard.Count);
            CollectionAssert.AreNotEqual(before, player.Hand);
            Assert.IsTrue(result.Events.Any(e => e.Type == GameEventType.MulliganReturned));
            Assert.IsTrue(result.Events.Any(e => e.Type == GameEventType.DeckShuffled));
            Assert.IsTrue(result.Events.Any(e => e.Type == GameEventType.InitialHandDrawn));
            Assert.IsNull(GameStateInvariants.FindViolation(result.State));
        }

        [Test]
        public void Mulligan_SecondPlayer_CannotConfirmDuringMulliganP0()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(5UL))).State;
            var hashBefore = GameStateHasher.Hash(state);
            var rngBefore = state.Rng.Save();
            var indexBefore = state.EventIndex;

            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("p1", 1, state.EventIndex, MulliganDecision.KeepHand));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CommandRejectionCode.WrongActor, result.Code);
            Assert.AreEqual(MatchPhase.MulliganP0, state.Phase);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(state));
            Assert.AreEqual(rngBefore, state.Rng.Save());
            Assert.AreEqual(indexBefore, state.EventIndex);
        }

        [Test]
        public void Mulligan_Player0_CannotConfirmTwice()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(6UL))).State;
            var first = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, MulliganDecision.KeepHand));
            Assert.IsTrue(first.Accepted, first.RejectionReason);
            Assert.AreEqual(MatchPhase.MulliganP1, first.State.Phase);

            var rngBefore = first.State.Rng.Save();
            var hashBefore = GameStateHasher.Hash(first.State);
            var second = engine.Submit(
                first.State,
                Command.ConfirmMulligan("c3", 0, first.State.EventIndex, MulliganDecision.KeepHand));

            Assert.IsFalse(second.Accepted);
            Assert.AreEqual(CommandRejectionCode.WrongActor, second.Code);
            Assert.AreEqual(rngBefore, first.State.Rng.Save());
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(first.State));
        }

        [Test]
        public void Mulligan_AfterPlayer1_StartsFirstTurnOfDeterminedPlayer()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(7UL))).State;
            var firstPlayer = state.FirstPlayerId;
            state = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, MulliganDecision.KeepHand)).State;
            Assert.AreEqual(MatchPhase.MulliganP1, state.Phase);

            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("c3", 1, state.EventIndex, MulliganDecision.KeepHand));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(MatchPhase.Main, result.State.Phase);
            Assert.AreEqual(1, result.State.Turn);
            Assert.AreEqual(firstPlayer, result.State.ActivePlayerId);
        }

        [Test]
        public void Mulligan_SameSeedAndDecisions_SameHands()
        {
            var a = PlayMulligans(11UL, MulliganDecision.ReplaceWholeHand, MulliganDecision.KeepHand);
            var b = PlayMulligans(11UL, MulliganDecision.ReplaceWholeHand, MulliganDecision.KeepHand);
            Assert.AreEqual(GameStateHasher.Hash(a), GameStateHasher.Hash(b));
            CollectionAssert.AreEqual(a.Players[0].Hand, b.Players[0].Hand);
            CollectionAssert.AreEqual(a.Players[1].Hand, b.Players[1].Hand);
        }

        [Test]
        public void Mulligan_Replace_PublicEventsDoNotContainHandInstanceIds()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(12UL))).State;
            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, MulliganDecision.ReplaceWholeHand));
            Assert.IsTrue(result.Accepted, result.RejectionReason);

            var handIds = new List<CardInstanceId>(result.State.Players[0].Hand);
            foreach (var evt in result.Events)
            {
                if (evt.Visibility != GameEventVisibility.Public)
                {
                    continue;
                }

                Assert.IsTrue(evt.SourceInstanceId.IsNone, evt.Type + " public event leaked a source instance.");
                for (var i = 0; i < handIds.Count; i++)
                {
                    StringAssert.DoesNotContain(handIds[i].ToString(), evt.Payload);
                }
            }
        }

        [Test]
        public void Mulligan_Replace_OwnerOnlyDrawsAreHiddenFromOpponent()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(13UL))).State;
            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, MulliganDecision.ReplaceWholeHand));
            Assert.IsTrue(result.Accepted, result.RejectionReason);

            var ownerDraws = 0;
            foreach (var evt in result.Events)
            {
                if (evt.Type != GameEventType.CardDrawn)
                {
                    continue;
                }

                ownerDraws++;
                Assert.AreEqual(GameEventVisibility.OwnerOnly, evt.Visibility);
                Assert.IsTrue(evt.IsVisibleTo(PlayerId.Player0));
                Assert.IsFalse(evt.IsVisibleTo(PlayerId.Player1));
            }

            Assert.AreEqual(5, ownerDraws);
        }

        [Test]
        public void Mulligan_SecondPlayer_CannotSeeFirstPlayerHandInView()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(5UL))).State;
            var view = GameStateView.For(state, viewerId: 1);

            Assert.AreEqual(5, view.Player0.HandCount);
            Assert.AreEqual(0, view.Player0.VisibleHand.Count);
            Assert.AreEqual(5, view.Player1.VisibleHand.Count);
        }

        private static GameState PlayMulligans(ulong seed, MulliganDecision p0, MulliganDecision p1)
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(seed))).State;
            state = engine.Submit(
                state,
                Command.ConfirmMulligan("c2", 0, state.EventIndex, p0)).State;
            var result = engine.Submit(
                state,
                Command.ConfirmMulligan("c3", 1, state.EventIndex, p1));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            return result.State;
        }
    }
}
