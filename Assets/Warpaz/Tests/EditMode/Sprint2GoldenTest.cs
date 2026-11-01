using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Golden Sprint 2: seed + attack/hero combat sequence with pinned final hash.
    /// Keeps Sprint 1 golden fixture untouched.
    /// </summary>
    public sealed class Sprint2GoldenTest
    {
        private const ulong Seed = 4202UL;
        private const string ContentVersion = "WZ0-combat";
        private const string RuleVersion = "0.1.0";

        // Captured from engine after Sprint 2 combat sequence; regenerate if policies/engine change.
        private const string ExpectedFinalHash = "6c9020d410a7a93884d3f44947af431968516d2cc8c9740c3ca13d7f77312b6c";

        [Test]
        public void Sprint2_GoldenFixture_ProducesExpectedOutput()
        {
            var catalog = CombatCatalog.Create();
            Assert.AreEqual(ContentVersion, catalog.ContentVersion);
            Assert.AreEqual(RuleVersion, RuleConfig.AlphaRuleVersion);

            var setup = new MatchSetup(
                Seed,
                RuleConfig.AlphaDefaults(),
                catalog.ContentVersion,
                new PlayerSetup(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, CombatCatalog.Deck40()),
                new PlayerSetup(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, CombatCatalog.Deck40()));

            var engine = new RulesEngine(catalog);
            var state = engine.Submit(null, Command.CreateMatch("c1", setup)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c2", 0, state.EventIndex, true)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c3", 1, state.EventIndex, true)).State;
            Assert.AreEqual(MatchPhase.Main, state.Phase);

            var actor = state.ActivePlayerId;
            var defender = actor.Opponent;

            var strong = PutReady(state, actor, CombatCatalog.StrongChar, 0);
            var weak = PutReady(state, defender, CombatCatalog.WeakChar, 0);

            var atk = engine.Submit(
                state,
                Command.Attack("atk1", actor, state.EventIndex, strong, weak));
            Assert.IsTrue(atk.Accepted, atk.RejectionReason);
            state = atk.State;
            Assert.AreEqual(CardZone.Discard, state.Cards[weak].Zone);
            Assert.AreEqual(CardZone.Vanguard, state.Cards[strong].Zone);
            Assert.AreEqual(ReadyState.Exhausted, state.Cards[strong].ReadyState);

            state = CombatTestHelpers.ClearRecentByPassingTurns(engine, state, actor);
            Assert.AreEqual(ReadyState.Ready, state.Cards[strong].ReadyState);
            Assert.IsTrue(CombatPolicies.CanTargetHero(state.GetPlayer(defender)));

            var resBefore = state.GetPlayer(defender).Resolution;
            var heroAtk = engine.Submit(
                state,
                Command.AttackHero("atk2", actor, state.EventIndex, strong));
            Assert.IsTrue(heroAtk.Accepted, heroAtk.RejectionReason);
            state = heroAtk.State;
            Assert.AreEqual(resBefore - 1, state.GetPlayer(defender).Resolution);
            Assert.AreEqual(MatchPhase.Main, state.Phase);

            // Optional reorg after hero strike (free reorg available on this Main).
            var reorg = engine.Submit(
                state,
                Command.ReorganizeMove("r1", actor, state.EventIndex, strong, BoardRow.Rearguard, 0));
            Assert.IsTrue(reorg.Accepted, reorg.RejectionReason);
            state = reorg.State;
            Assert.AreEqual(CardZone.Rearguard, state.Cards[strong].Zone);

            var finalHash = GameStateHasher.Hash(state);
            Assert.AreEqual(ExpectedFinalHash, finalHash,
                "Final hash does not match pinned Sprint 2 golden — engine/policy changed");
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.AttackDeclared));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.CombatResolved));
            Assert.IsTrue(CombatTestHelpers.HasEvent(state, GameEventType.ResolutionRemoved));
        }

        private static CardInstanceId PutReady(GameState state, PlayerId playerId, CardId definitionId, int slot)
        {
            var instanceId = FindDefinition(state, playerId, definitionId);
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(state, playerId, instanceId, slot);
            GameStateInvariants.AssertValid(state);
            return instanceId;
        }

        private static CardInstanceId FindDefinition(GameState state, PlayerId playerId, CardId definitionId)
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

            throw new System.InvalidOperationException("Missing " + definitionId);
        }
    }
}
