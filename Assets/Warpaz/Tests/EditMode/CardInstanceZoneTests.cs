using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class CardInstanceZoneTests
    {
        [Test]
        public void CardInstance_OccupiesExactlyOneZone()
        {
            var card = new CardInstance(
                new CardInstanceId(1),
                new CardId("WZ1-001"),
                PlayerId.Player0,
                PlayerId.Player0,
                CardZone.Hand);

            Assert.AreEqual(CardZone.Hand, card.Zone);
            Assert.IsFalse(card.HasBoardSlot);

            card.SetZone(CardZone.Deck);
            Assert.AreEqual(CardZone.Deck, card.Zone);
            Assert.AreNotEqual(CardZone.Hand, card.Zone);

            card.SetZone(CardZone.Vanguard, BoardSlot.Vanguard(0, card.InstanceId));
            Assert.AreEqual(CardZone.Vanguard, card.Zone);
            Assert.IsTrue(card.HasBoardSlot);
            Assert.AreEqual(BoardRow.Vanguard, card.Slot.Row);
        }

        [Test]
        public void CreateMatch_EachInstanceHasASingleZoneMembership()
        {
            var state = new RulesEngine(MatchFixtures.Catalog)
                .Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(2UL)))
                .State;

            foreach (var card in state.Cards.Values)
            {
                var memberships = 0;
                foreach (var player in state.Players)
                {
                    if (Contains(player.Deck, card.InstanceId))
                    {
                        memberships++;
                        Assert.AreEqual(CardZone.Deck, card.Zone);
                    }

                    if (Contains(player.Hand, card.InstanceId))
                    {
                        memberships++;
                        Assert.AreEqual(CardZone.Hand, card.Zone);
                    }

                    if (Contains(player.Discard, card.InstanceId))
                    {
                        memberships++;
                        Assert.AreEqual(CardZone.Discard, card.Zone);
                    }
                }

                if (card.Zone == CardZone.Hero || card.Zone == CardZone.ClassPath)
                {
                    Assert.AreEqual(0, memberships, card.DefinitionId + " identity must not also sit in a pile.");
                }
                else
                {
                    Assert.AreEqual(1, memberships, card.InstanceId + " must occupy exactly one pile.");
                }
            }

            Assert.IsNull(GameStateInvariants.FindViolation(state));
        }

        [Test]
        public void Invariants_DetectTheSameInstanceInTwoPiles()
        {
            var state = new RulesEngine(MatchFixtures.Catalog)
                .Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(2UL)))
                .State;
            var duplicate = state.Players[0].Hand[0];
            state.Players[0].Deck.Add(duplicate);

            StringAssert.Contains("more than one zone", GameStateInvariants.FindViolation(state));
        }

        private static bool Contains(List<CardInstanceId> ids, CardInstanceId target)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
