using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class TurnPhaseTests
    {
        [Test]
        public void AfterBothMulligans_FirstPlayerIsMainWithPa1AndDrawnCard()
        {
            var state = PlayToMain();
            var active = state.ActivePlayerId;
            var opponent = active.Opponent;

            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(state.FirstPlayerId, active);
            Assert.AreEqual(1, state.Turn);
            Assert.AreEqual(1, state.Players[active.Index].PaMax);
            Assert.AreEqual(1, state.Players[active.Index].PaCurrent);
            Assert.AreEqual(0, state.Players[opponent.Index].PaMax);
            Assert.AreEqual(0, state.Players[opponent.Index].PaCurrent);
            Assert.AreEqual(6, state.Players[active.Index].Hand.Count);
            Assert.AreEqual(5, state.Players[opponent.Index].Hand.Count);
        }

        [Test]
        public void EndTurn_SecondPlayerReachesMainWithPa1_ThenFirstPlayerPa2()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = PlayToMain();
            var first = state.ActivePlayerId;

            state = EndTurn(engine, state, "e1");
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(first.Opponent, state.ActivePlayerId);
            Assert.AreEqual(1, state.Players[first.Opponent.Index].PaMax);
            Assert.AreEqual(1, state.Players[first.Opponent.Index].PaCurrent);
            Assert.AreEqual(1, state.Players[first.Index].PaMax);
            Assert.AreEqual(1, state.Players[first.Index].PaCurrent);

            state = EndTurn(engine, state, "e2");
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(first, state.ActivePlayerId);
            Assert.AreEqual(2, state.Players[first.Index].PaMax);
            Assert.AreEqual(2, state.Players[first.Index].PaCurrent);
        }

        [Test]
        public void PaPhase_FirstFiveOwnTurns_Are_1_2_3_4_5()
        {
            var state = PlayToMain();
            var trackedPlayer = state.ActivePlayerId;
            var pa = new List<int>();
            var serial = 0;

            while (pa.Count < 5)
            {
                if (state.Phase == MatchPhase.Main && state.ActivePlayerId == trackedPlayer)
                {
                    pa.Add(state.Players[trackedPlayer.Index].PaMax);
                }

                serial++;
                var result = new RulesEngine(MatchFixtures.Catalog).Submit(
                    state,
                    Command.EndTurn("e" + serial, state.ActivePlayerId, state.EventIndex));
                Assert.IsTrue(result.Accepted, result.RejectionReason);
                state = result.State;
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, pa);
        }

        [Test]
        public void PaPhase_CapsAt8()
        {
            var state = PlayToMain();
            var trackedPlayer = state.ActivePlayerId;
            var seenEight = 0;

            for (var i = 0; i < 30 && seenEight < 2; i++)
            {
                if (state.Phase == MatchPhase.Main && state.ActivePlayerId == trackedPlayer)
                {
                    if (state.Players[trackedPlayer.Index].PaMax == 8)
                    {
                        seenEight++;
                    }
                    else
                    {
                        Assert.Less(state.Players[trackedPlayer.Index].PaMax, 9);
                    }
                }

                state = new RulesEngine(MatchFixtures.Catalog).Submit(
                    state,
                    Command.EndTurn("e" + i, state.ActivePlayerId, state.EventIndex)).State;
            }

            Assert.AreEqual(2, seenEight);
            Assert.AreEqual(8, state.Players[trackedPlayer.Index].PaMax);
        }

        [Test]
        public void Draw_OneCardPerTurn_WhenDeckRemains()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = PlayToMain();
            var first = state.ActivePlayerId;
            Assert.AreEqual(6, state.Players[first.Index].Hand.Count);

            state = EndTurn(engine, state, "e1");
            Assert.AreEqual(6, state.Players[first.Index].Hand.Count);
            Assert.AreEqual(6, state.Players[first.Opponent.Index].Hand.Count);

            state = EndTurn(engine, state, "e2");
            Assert.AreEqual(first, state.ActivePlayerId);
            Assert.AreEqual(7, state.Players[first.Index].Hand.Count);
        }

        [Test]
        public void Draw_EmptyDeck_RemovesOneResolution_AndEmitsDrawFailed()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = PlayToMain();
            var miller = state.ActivePlayerId;
            RulesEngine.MoveDeckToDiscard(state, miller);
            Assert.AreEqual(0, state.Players[miller.Index].Deck.Count);

            var before = state.Players[miller.Index].Resolution;
            state = EndTurn(engine, state, "e1");
            state = EndTurn(engine, state, "e2");

            Assert.AreEqual(miller, state.ActivePlayerId);
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(before - 1, state.Players[miller.Index].Resolution);
            Assert.IsTrue(HasEvent(state, GameEventType.DrawFailed));
            Assert.IsTrue(HasEvent(state, GameEventType.ResolutionRemoved));
        }

        [Test]
        public void Draw_ResolutionReachesZero_EndsMatchForOpponent()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = PlayToMain();
            var miller = state.ActivePlayerId;
            RulesEngine.MoveDeckToDiscard(state, miller);
            state.Players[miller.Index].Resolution = 1;

            state = EndTurn(engine, state, "e1");
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(miller.Opponent, state.ActivePlayerId);

            var turnBeforeReturn = state.Turn;
            state = EndTurn(engine, state, "e2");

            Assert.AreEqual(MatchPhase.GameOver, state.Phase);
            Assert.IsTrue(state.Result.IsFinished);
            Assert.AreEqual(MatchEndCause.ResolutionDepleted, state.Result.Cause);
            Assert.AreEqual(miller.Opponent, state.Result.Winner);
            Assert.IsTrue(HasEvent(state, GameEventType.MatchEnded));
            Assert.AreEqual(turnBeforeReturn + 1, state.Turn);

            var hashBefore = GameStateHasher.Hash(state);
            var late = engine.Submit(
                state,
                Command.EndTurn("e3", miller, state.EventIndex));
            Assert.IsFalse(late.Accepted);
            Assert.AreEqual(CommandRejectionCode.MatchAlreadyFinished, late.Code);
            Assert.AreEqual(MatchPhase.GameOver, late.State.Phase);
            Assert.AreEqual(turnBeforeReturn + 1, late.State.Turn);
            Assert.AreEqual(hashBefore, GameStateHasher.Hash(late.State));
        }

        [Test]
        public void EndTurn_Rejected_WhenNotMainOrWrongActorOrStaleIndex()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var created = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(9UL))).State;

            var stale = engine.Submit(created, Command.EndTurn("bad", 0, 0));
            Assert.IsFalse(stale.Accepted);
            Assert.AreEqual(CommandRejectionCode.StaleState, stale.Code);
            StringAssert.Contains("expectedEventIndex", stale.RejectionReason);

            var notMain = engine.Submit(
                created,
                Command.EndTurn("bad2", created.ActivePlayerId, created.EventIndex));
            Assert.IsFalse(notMain.Accepted);
            Assert.AreEqual(CommandRejectionCode.InvalidPhase, notMain.Code);

            var state = PlayToMain(11UL);
            var wrongActor = engine.Submit(
                state,
                Command.EndTurn("bad3", state.ActivePlayerId.Opponent, state.EventIndex));
            Assert.IsFalse(wrongActor.Accepted);
            Assert.AreEqual(CommandRejectionCode.WrongActor, wrongActor.Code);
            Assert.AreEqual(state.EventIndex, wrongActor.State.EventIndex);

            var unsupported = engine.Submit(
                state,
                Command.Unsupported("bad4", state.ActivePlayerId, CommandType.Equip, state.EventIndex));
            Assert.IsFalse(unsupported.Accepted);
            Assert.AreEqual(CommandRejectionCode.CommandNotSupported, unsupported.Code);
        }

        [Test]
        public void SecondMulligan_ExceedsAutomaticPhaseLimit_Throws()
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(
                null,
                Command.CreateMatch("c1", MatchFixtures.Standard(8UL, WithMaximumEventsPerTurn(1)))).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c2", 0, state.EventIndex, true)).State;

            Assert.Throws<InvariantViolationException>(() =>
                engine.Submit(state, Command.ConfirmMulligan("c3", 1, state.EventIndex, true)));
        }

        [Test]
        public void FiveTurns_SameSeedAndCommands_SameFinalHash()
        {
            var a = PlayFiveTurns(13UL);
            var b = PlayFiveTurns(13UL);
            Assert.AreEqual(GameStateHasher.Hash(a), GameStateHasher.Hash(b));
            CollectionAssert.AreEqual(EventTypes(a), EventTypes(b));
            Assert.AreEqual(6, a.Turn);
            Assert.AreEqual(MatchPhase.Main, a.Phase);
        }

        [Test]
        public void FiveTurns_DifferentSeed_DifferentOpeningHands()
        {
            var a = new RulesEngine(MatchFixtures.Catalog)
                .Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(1UL))).State;
            var b = new RulesEngine(MatchFixtures.Catalog)
                .Submit(null, Command.CreateMatch("c2", MatchFixtures.Standard(99UL))).State;
            Assert.AreNotEqual(GameStateHasher.Hash(a), GameStateHasher.Hash(b));
        }

        private static GameState PlayToMain(ulong seed = 8UL)
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(seed))).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c2", 0, state.EventIndex, true)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c3", 1, state.EventIndex, true)).State;
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(1, state.Turn);
            return state;
        }

        private static GameState PlayFiveTurns(ulong seed)
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var state = PlayToMain(seed);
            for (var i = 0; i < 5; i++)
            {
                state = EndTurn(engine, state, "e" + i);
            }

            return state;
        }

        private static GameState EndTurn(RulesEngine engine, GameState state, string commandId)
        {
            var result = engine.Submit(
                state,
                Command.EndTurn(commandId, state.ActivePlayerId, state.EventIndex));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            return result.State;
        }

        private static bool HasEvent(GameState state, GameEventType type)
        {
            for (var i = 0; i < state.EventLog.Count; i++)
            {
                if (state.EventLog[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<GameEventType> EventTypes(GameState state)
        {
            var types = new List<GameEventType>(state.EventLog.Count);
            for (var i = 0; i < state.EventLog.Count; i++)
            {
                types.Add(state.EventLog[i].Type);
            }

            return types;
        }

        private static RuleConfig WithMaximumEventsPerTurn(int maximumEventsPerTurn)
        {
            var a = RuleConfig.AlphaDefaults();
            return new RuleConfig(
                a.RuleVersion,
                a.InitialHandSize,
                a.InitialResolution,
                a.DeckSize,
                a.MaxCopiesPerCard,
                a.InitialPaMax,
                a.PaIncreasePerTurn,
                a.PaLimit,
                a.AscensionRequiredPaMax,
                a.AscensionCost,
                a.MaximumEquipmentPerCharacter,
                a.FreeReorganizationsPerTurn,
                a.HandLimit,
                a.FailedDrawResolutionLoss,
                maximumEventsPerTurn,
                a.MaximumDecisionsPerTurn);
        }
    }
}
