using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class GameStateCanonicalTests
    {
        [Test]
        public void EquivalentStates_ProduceTheSameHash()
        {
            var first = CreateMatch(7UL);
            var second = CreateMatch(7UL);
            Assert.AreEqual(GameStateHasher.Hash(first), GameStateHasher.Hash(second));
            Assert.AreEqual(
                CanonicalGameStateSerializer.ToString(first),
                CanonicalGameStateSerializer.ToString(second));
        }

        [Test]
        public void RelevantMutation_ChangesTheHash()
        {
            var state = CreateMatch(8UL);
            var before = GameStateHasher.Hash(state);
            var resolutionBefore = state.Players[0].Resolution;

            state.Players[0].Resolution = resolutionBefore - 1;

            var after = GameStateHasher.Hash(state);
            Assert.AreNotEqual(before, after);

            state.Players[0].Resolution = resolutionBefore;
            Assert.AreEqual(before, GameStateHasher.Hash(state));
        }

        [Test]
        public void Hash_IsStableUnderEsEsAndEnUs()
        {
            var state = CreateMatch(11UL);
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("es-ES");
                var es = GameStateHasher.Hash(state);

                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var en = GameStateHasher.Hash(state);

                Assert.AreEqual(es, en);
                Assert.AreEqual(64, es.Length);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUi;
            }
        }

        [Test]
        public void CanonicalDocument_RegistersVersionsSeedAndEventIndex()
        {
            var state = CreateMatch(13UL);
            var document = CanonicalGameStateSerializer.ToString(state);
            var lines = document.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Assert.AreEqual("schemaVersion=" + state.SchemaVersion, lines[0]);
            Assert.AreEqual("contentVersion=" + state.ContentVersion, lines[1]);
            Assert.AreEqual("ruleVersion=" + state.RuleVersion, lines[2]);
            Assert.AreEqual("seed=" + state.Seed.ToString(CultureInfo.InvariantCulture), lines[3]);
            Assert.AreEqual("eventIndex=" + state.EventIndex.ToString(CultureInfo.InvariantCulture), lines[4]);
            StringAssert.DoesNotContain("\r", document);
        }

        [Test]
        public void CanonicalDocument_ListsCardsSortedByInstanceId()
        {
            var state = CreateMatch(4UL);
            var previous = 0;
            var seen = 0;
            var lines = CanonicalGameStateSerializer.ToString(state).Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                const string prefix = "card=";
                if (!lines[i].StartsWith(prefix))
                {
                    continue;
                }

                var comma = lines[i].IndexOf(',', prefix.Length);
                var id = int.Parse(lines[i].Substring(prefix.Length, comma - prefix.Length), CultureInfo.InvariantCulture);
                Assert.Greater(id, previous);
                previous = id;
                seen++;
            }

            Assert.AreEqual(state.Cards.Count, seen);
        }

        private static GameState CreateMatch(ulong seed)
        {
            var engine = new RulesEngine(MatchFixtures.Catalog);
            var result = engine.Submit(null, Command.CreateMatch("c1", MatchFixtures.Standard(seed)));
            Assert.IsTrue(result.Accepted, result.RejectionReason);
            return result.State;
        }
    }
}
