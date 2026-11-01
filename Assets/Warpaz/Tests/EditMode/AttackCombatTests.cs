using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class AttackCombatTests
    {
        [Test]
        public void Attack_Character_LowerPowerIsKnockedOut()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 61UL);

            var attackerId = PutDefinitionOnVanguard(state, state.ActivePlayerId, CombatCatalog.StrongChar, 0);
            var defenderId = PutDefinitionOnVanguard(state, state.ActivePlayerId.Opponent, CombatCatalog.WeakChar, 0);

            var result = engine.Submit(
                state,
                Command.Attack("atk", state.ActivePlayerId, state.EventIndex, attackerId, defenderId));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            state = result.State;

            Assert.AreEqual(CardZone.Vanguard, state.Cards[attackerId].Zone);
            Assert.AreEqual(ReadyState.Exhausted, state.Cards[attackerId].ReadyState);
            Assert.AreEqual(CardZone.Discard, state.Cards[defenderId].Zone);
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.AttackDeclared));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.CombatResolved));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.CharacterKnockedOut));
        }

        [Test]
        public void Attack_Character_Tie_BothKnockedOut()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 62UL);

            var attackerId = PutDefinitionOnVanguard(state, state.ActivePlayerId, CombatCatalog.WeakChar, 0);
            var defenderId = PutDefinitionOnVanguard(state, state.ActivePlayerId.Opponent, CombatCatalog.WeakChar, 0);

            state = engine.Submit(
                state,
                Command.Attack("atk", state.ActivePlayerId, state.EventIndex, attackerId, defenderId)).State;

            Assert.AreEqual(CardZone.Discard, state.Cards[attackerId].Zone);
            Assert.AreEqual(CardZone.Discard, state.Cards[defenderId].Zone);
        }

        [Test]
        public void Attack_Hero_WhenNoVanguard_AttackerHigher_RemovesResolution()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 63UL);
            var actor = state.ActivePlayerId;
            var defender = actor.Opponent;

            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.StrongChar, 0);
            Assert.IsTrue(CombatPolicies.CanTargetHero(state.GetPlayer(defender)));
            var before = state.GetPlayer(defender).Resolution;

            state = engine.Submit(
                state,
                Command.AttackHero("atk", actor, state.EventIndex, attackerId)).State;

            Assert.AreEqual(before - 1, state.GetPlayer(defender).Resolution);
            Assert.AreEqual(CardZone.Vanguard, state.Cards[attackerId].Zone);
            Assert.AreEqual(ReadyState.Exhausted, state.Cards[attackerId].ReadyState);
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.IsFalse(state.PendingDecision.IsPending);
        }

        [Test]
        public void Attack_Hero_HeroHigher_AttackerKnockedOut()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 64UL);
            var actor = state.ActivePlayerId;
            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.WeakChar, 0);
            var before = state.GetPlayer(actor.Opponent).Resolution;

            state = engine.Submit(
                state,
                Command.AttackHero("atk", actor, state.EventIndex, attackerId)).State;

            Assert.AreEqual(before, state.GetPlayer(actor.Opponent).Resolution);
            Assert.AreEqual(CardZone.Discard, state.Cards[attackerId].Zone);
        }

        [Test]
        public void Attack_Hero_Tie_AttackerKoAndResolutionLoss()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 65UL);
            var actor = state.ActivePlayerId;
            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.MidChar, 0);
            var before = state.GetPlayer(actor.Opponent).Resolution;

            state = engine.Submit(
                state,
                Command.AttackHero("atk", actor, state.EventIndex, attackerId)).State;

            Assert.AreEqual(before - 1, state.GetPlayer(actor.Opponent).Resolution);
            Assert.AreEqual(CardZone.Discard, state.Cards[attackerId].Zone);
        }

        [Test]
        public void Attack_Hero_ResolutionZero_EndsMatch()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 66UL);
            var actor = state.ActivePlayerId;
            state.GetPlayer(actor.Opponent).Resolution = 1;
            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.StrongChar, 0);

            state = engine.Submit(
                state,
                Command.AttackHero("atk", actor, state.EventIndex, attackerId)).State;

            Assert.AreEqual(MatchPhase.GameOver, state.Phase);
            Assert.AreEqual(MatchEndCause.ResolutionDepleted, state.Result.Cause);
            Assert.AreEqual(actor, state.Result.Winner);
        }

        [Test]
        public void Attack_Hero_Rejected_WhenEnemyVanguardExists()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 67UL);
            var actor = state.ActivePlayerId;
            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.StrongChar, 0);
            PutDefinitionOnVanguard(state, actor.Opponent, CombatCatalog.WeakChar, 0);
            var hash = GameStateHasher.Hash(state);

            var rejected = engine.Submit(
                state,
                Command.AttackHero("atk", actor, state.EventIndex, attackerId));
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(hash, GameStateHasher.Hash(rejected.State));
        }

        [Test]
        public void Attack_Rejected_WhenAttackerRecentOrRearguard()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 68UL);
            var actor = state.ActivePlayerId;

            var recentId = PutDefinitionOnVanguard(state, actor, CombatCatalog.StrongChar, 0);
            state.Cards[recentId].AddStatus(StatusType.Recent);
            PutDefinitionOnVanguard(state, actor.Opponent, CombatCatalog.WeakChar, 0);
            var hash = GameStateHasher.Hash(state);
            var rejectedRecent = engine.Submit(
                state,
                Command.Attack(
                    "a1",
                    actor,
                    state.EventIndex,
                    recentId,
                    CombatTestHelpers.Occupant(state, actor.Opponent, BoardRow.Vanguard, 0)));
            Assert.IsFalse(rejectedRecent.Accepted);
            Assert.AreEqual(hash, GameStateHasher.Hash(rejectedRecent.State));

            state.Cards[recentId].RemoveStatus(StatusType.Recent);
            var rearId = EnsureDefinition(state, actor, CombatCatalog.RearChar);
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(state, actor, rearId, 1);
            // move to rearguard
            var player = state.GetPlayer(actor);
            player.SetBoardSlot(BoardSlot.Empty(BoardRow.Vanguard, 1));
            var rearSlot = BoardSlot.Rearguard(0, rearId);
            state.Cards[rearId].SetZone(CardZone.Rearguard, rearSlot);
            player.SetBoardSlot(rearSlot);

            hash = GameStateHasher.Hash(state);
            var rejectedRear = engine.Submit(
                state,
                Command.Attack(
                    "a2",
                    actor,
                    state.EventIndex,
                    rearId,
                    CombatTestHelpers.Occupant(state, actor.Opponent, BoardRow.Vanguard, 0)));
            Assert.IsFalse(rejectedRear.Accepted);
            Assert.AreEqual(hash, GameStateHasher.Hash(rejectedRear.State));
        }

        [Test]
        public void Intercept_Accept_ResolvesVsInterceptor()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 69UL);
            var actor = state.ActivePlayerId;
            var defender = actor.Opponent;

            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.StrongChar, 0);
            EngineActions.Exhaust(state, state.Cards[attackerId]);
            var interceptorId = PutDefinitionOnVanguard(state, defender, CombatCatalog.WeakChar, 0);
            EngineActions.OpenInterceptDecision(
                state,
                defender,
                attackerId,
                CardInstanceId.None,
                true,
                new List<CardInstanceId> { interceptorId });

            Assert.AreEqual(MatchPhase.PendingChoice, state.Phase);
            var actions = LegalActions.For(state, catalog);
            Assert.GreaterOrEqual(actions.Count, 2);

            var chosen = engine.Submit(
                state,
                Command.ChooseInterceptor("pick", defender, state.EventIndex, interceptorId));
            Assert.IsTrue(chosen.Accepted, chosen.RejectionReason);
            state = chosen.State;

            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.IsFalse(state.PendingDecision.IsPending);
            Assert.AreEqual(CardZone.Discard, state.Cards[interceptorId].Zone);
            Assert.AreEqual(CardZone.Vanguard, state.Cards[attackerId].Zone);
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.AttackIntercepted));
        }

        [Test]
        public void Intercept_Decline_ResolvesVsHero()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 70UL);
            var actor = state.ActivePlayerId;
            var defender = actor.Opponent;

            var attackerId = PutDefinitionOnVanguard(state, actor, CombatCatalog.StrongChar, 0);
            EngineActions.Exhaust(state, state.Cards[attackerId]);
            var interceptorId = PutDefinitionOnVanguard(state, defender, CombatCatalog.WeakChar, 0);
            EngineActions.OpenInterceptDecision(
                state,
                defender,
                attackerId,
                CardInstanceId.None,
                true,
                new List<CardInstanceId> { interceptorId });

            var before = state.GetPlayer(defender).Resolution;
            var declined = engine.Submit(
                state,
                Command.DeclineInterception("pass", defender, state.EventIndex));
            Assert.IsTrue(declined.Accepted, declined.RejectionReason);
            state = declined.State;

            Assert.AreEqual(MatchPhase.Main, state.Phase);
            Assert.AreEqual(before - 1, state.GetPlayer(defender).Resolution);
            Assert.AreEqual(CardZone.Vanguard, state.Cards[interceptorId].Zone);
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.InterceptionDeclined));
        }

        [Test]
        public void Preparation_ReadiesAndRemovesRecent()
        {
            var catalog = CombatCatalog.Create();
            var engine = new RulesEngine(catalog);
            var state = StartCombatMatch(engine, catalog, 71UL);
            var actor = state.ActivePlayerId;
            var cardId = PutDefinitionOnVanguard(state, actor, CombatCatalog.WeakChar, 0);
            state.Cards[cardId].ReadyState = ReadyState.Exhausted;
            state.Cards[cardId].AddStatus(StatusType.Recent);

            state = CombatTestHelpers.ClearRecentByPassingTurns(engine, state, actor);

            Assert.AreEqual(ReadyState.Ready, state.Cards[cardId].ReadyState);
            Assert.IsFalse(CombatPolicies.HasRecent(state.Cards[cardId]));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.CharacterReadied));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.RecentRemoved));
        }

        private static GameState StartCombatMatch(RulesEngine engine, CombatCatalog catalog, ulong seed)
        {
            var deck0 = CombatCatalog.Deck40();
            var deck1 = CombatCatalog.Deck40();
            var setup = new MatchSetup(
                seed,
                RuleConfig.AlphaDefaults(),
                catalog.ContentVersion,
                new PlayerSetup(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, deck0),
                new PlayerSetup(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, deck1));

            var state = engine.Submit(null, Command.CreateMatch("c1", setup)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c2", 0, state.EventIndex, true)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c3", 1, state.EventIndex, true)).State;
            Assert.AreEqual(MatchPhase.Main, state.Phase);
            return state;
        }

        private static CardInstanceId PutDefinitionOnVanguard(
            GameState state,
            PlayerId playerId,
            CardId definitionId,
            int slotIndex)
        {
            var instanceId = EnsureDefinition(state, playerId, definitionId);
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(state, playerId, instanceId, slotIndex);
            GameStateInvariants.AssertValid(state);
            return instanceId;
        }

        private static CardInstanceId EnsureDefinition(GameState state, PlayerId playerId, CardId definitionId)
        {
            var player = state.GetPlayer(playerId);
            foreach (var zone in new[] { player.Hand, player.Deck, player.Discard })
            {
                for (var i = 0; i < zone.Count; i++)
                {
                    if (state.Cards[zone[i]].DefinitionId == definitionId)
                    {
                        return zone[i];
                    }
                }
            }

            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var occ = player.BoardSlots[i].Occupant;
                if (!occ.IsNone && state.Cards[occ].DefinitionId == definitionId)
                {
                    return occ;
                }
            }

            throw new System.InvalidOperationException("Definition " + definitionId + " not found for " + playerId);
        }
    }
}
