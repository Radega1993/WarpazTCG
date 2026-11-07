using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class DeckLibraryPreviewTests
    {
        private CardCatalog Catalog()
        {
            var root = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8; i++)
            {
                var path = Path.Combine(root, "Assets", "StreamingAssets", "Data");
                if (Directory.Exists(path)) return CardCatalogLoader.LoadFromDirectory(path).Catalog;
                root = Path.GetDirectoryName(root);
            }
            throw new Exception("Run from project root.");
        }
        private DeckList Deck(ICardCatalog c) => new DeckList("test", "Test", c.All.First(x => x.Type == CardType.Hero).CardId,
            c.All.First(x => x.Type == CardType.ClassPath).CardId, c.All.Where(x => x.Type == CardType.Character).Take(14)
                .SelectMany((x, i) => Enumerable.Repeat(x.CardId, i < 12 ? 3 : 2)).ToArray());

        [Test] public void DraftLimitsAndDuplicateAreIndependent()
        {
            var c = Catalog(); var draft = new DeckDraft(Deck(c)); var card = draft.Cards[0];
            Assert.IsFalse(draft.Add(card, c, RuleConfig.PreviewDefaults()));
            var copy = draft.Duplicate(); copy.Remove(card); copy.Rename("Copy");
            Assert.AreEqual(40, draft.Cards.Count); Assert.AreEqual(39, copy.Cards.Count); Assert.AreNotEqual(draft.Id, copy.Id);
            Assert.Throws<ArgumentException>(() => copy.Rename(" "));
            Assert.IsFalse(copy.Add(copy.Hero, c, RuleConfig.PreviewDefaults()));
        }
        [Test] public void PersistenceSupportsEmptyLibraryAndRejectsConflictsAndCorruption()
        {
            var dir = Path.Combine(Path.GetTempPath(), "warpaz-test-" + Guid.NewGuid().ToString("N")); var path = Path.Combine(dir, "library.json");
            try
            {
                var c = Catalog(); var store = new DeckLibraryStore(path); var decks = store.Load(new[] { Deck(c) });
                decks[0].Rename("Saved"); store.Save(decks); Assert.IsTrue(File.Exists(path + ".bak"));
                var other = new DeckLibraryStore(path); Assert.AreEqual("Saved", other.Load(new[] { Deck(c) })[0].Name);
                decks.Clear(); store.Save(decks); Assert.AreEqual(0, new DeckLibraryStore(path).Load(new[] { Deck(c) }).Count);
                Assert.Throws<IOException>(() => other.Save(decks));
                File.WriteAllText(path, "broken");
                Assert.Throws(
                    Is.InstanceOf<Exception>().And.Not.InstanceOf<IOException>(),
                    () => new DeckLibraryStore(path).Load(new[] { Deck(c) }));
                Assert.AreEqual("broken", File.ReadAllText(path));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
        [Test] public void UnsupportedCardsRejectedBeforeMatchCreation()
        {
            var c = Catalog(); var d = Deck(c); var cards = d.MainDeckCardIds.ToArray(); cards[0] = c.All.First(x => x.Type == CardType.Action).CardId;
            var bad = new DeckList("bad", "Bad", d.HeroCardId, d.ClassCardId, cards);
            var result = new RulesEngine(c).Submit(null, Command.CreateMatch("bad", new MatchSetup(0, RuleConfig.PreviewDefaults(), c.ContentVersion,
                new PlayerSetup(PlayerId.Player0, bad), new PlayerSetup(PlayerId.Player1, d))));
            Assert.IsFalse(result.Accepted); Assert.IsNull(result.State);
            Assert.IsTrue(DeckValidator.Validate(bad, c, RuleConfig.AlphaDefaults()).IsValid);
        }
        [TestCase(false)] [TestCase(true)]
        public void UniqueIsConsistentWithLegalActionsAndRejectedAtomically(bool legacy)
        {
            var c = Catalog(); var d = Deck(c); var e = new RulesEngine(c);
            var s = e.Submit(null, Command.CreateMatch("create", new MatchSetup(0, legacy ? RuleConfig.AlphaDefaults() : RuleConfig.PreviewDefaults(), c.ContentVersion,
                new PlayerSetup(PlayerId.Player0, d), new PlayerSetup(PlayerId.Player1, d)))).State;
            s = e.Submit(s, Command.ConfirmMulligan("m0", 0, s.EventIndex, true)).State;
            s = e.Submit(s, Command.ConfirmMulligan("m1", 1, s.EventIndex, true)).State;
            var unique = c.All.First(x => x.Type == CardType.Character && x.Unique && d.MainDeckCardIds.Count(id => id == x.CardId) >= 2);
            var copies = s.Cards.Values.Where(x => x.OwnerId == s.ActivePlayerId && x.DefinitionId == unique.CardId).Take(2).ToArray();
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(s, s.ActivePlayerId, copies[0].InstanceId, 0);
            var player = s.GetPlayer(s.ActivePlayerId);
            player.Deck.Remove(copies[1].InstanceId); if (!player.Hand.Contains(copies[1].InstanceId)) player.Hand.Add(copies[1].InstanceId);
            copies[1].SetZone(CardZone.Hand); CombatTestHelpers.SetPa(s, s.ActivePlayerId, 8);
            var position = CharacterPositions.TryParse(unique.Position, out var parsed) ? parsed : throw new Exception("position");
            var row = CharacterPositions.AllowsRow(position, BoardRow.Vanguard) ? BoardRow.Vanguard : BoardRow.Rearguard;
            var legal = LegalActions.For(s, c).Any(a => a.Type == CommandType.Play && a.PrimaryInstanceId == copies[1].InstanceId);
            Assert.AreEqual(legacy, legal);
            var hash = GameStateHasher.Hash(s);
            var result = e.Submit(s, Command.Play("play", s.ActivePlayerId, s.EventIndex, copies[1].InstanceId, row, 1));
            Assert.AreEqual(legacy, result.Accepted);
            if (!legacy) Assert.AreEqual(hash, GameStateHasher.Hash(s));
            Assert.IsTrue(CombatPolicies.CanPlayUnique(s, unique, player.PlayerId, copies[0].InstanceId) || legacy);
        }
        [Test] public void MissingPrimaryWithBackupDoesNotReseed()
        {
            var dir = Path.Combine(Path.GetTempPath(), "warpaz-backup-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "library.json");
            try { File.WriteAllText(path + ".bak", "backup"); Assert.Throws<IOException>(() => new DeckLibraryStore(path).Load(new[] { Deck(Catalog()) })); Assert.IsFalse(File.Exists(path)); }
            finally { Directory.Delete(dir, true); }
        }
        [TestCase(false)] [TestCase(true)]
        public void RearGuardInterceptionResolvesOrDeclines(bool decline)
        {
            var c = Catalog(); var d = Deck(c); var e = new RulesEngine(c);
            var s = e.Submit(null, Command.CreateMatch("create", new MatchSetup(0, RuleConfig.PreviewDefaults(), c.ContentVersion,
                new PlayerSetup(PlayerId.Player0, d), new PlayerSetup(PlayerId.Player1, d)))).State;
            s = e.Submit(s, Command.ConfirmMulligan("m0", 0, s.EventIndex, true)).State;
            s = e.Submit(s, Command.ConfirmMulligan("m1", 1, s.EventIndex, true)).State;
            // Focused policy fixture: legal runtime attack/choice pipeline from a prepared board.
            var guardDef = c.All.First(x => x.Type == CardType.Character && x.Keywords.Contains("Guardián") && d.MainDeckCardIds.Contains(x.CardId));
            var defender = s.ActivePlayerId.Opponent;
            var guard = s.Cards.Values.First(x => x.OwnerId == defender && x.DefinitionId == guardDef.CardId);
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(s, defender, guard.InstanceId, 0);
            s.GetPlayer(defender).SetBoardSlot(BoardSlot.Vanguard(0, CardInstanceId.None));
            var rear = BoardSlot.Rearguard(0, guard.InstanceId); guard.SetZone(CardZone.Rearguard, rear); s.GetPlayer(defender).SetBoardSlot(rear);
            var attack = s.Cards.Values.First(x => x.OwnerId == s.ActivePlayerId && c.TryGet(x.DefinitionId, out var def) && def.Type == CardType.Character);
            CombatTestHelpers.PlaceReadyVanguardWithoutRecent(s, s.ActivePlayerId, attack.InstanceId, 0);
            var result = e.Submit(s, Command.AttackHero("attack", s.ActivePlayerId, s.EventIndex, attack.InstanceId));
            Assert.IsTrue(result.Accepted, result.RejectionReason); Assert.AreEqual(MatchPhase.PendingChoice, s.Phase);
            var command = decline ? Command.DeclineInterception("choice", defender, s.EventIndex) : Command.ChooseInterceptor("choice", defender, s.EventIndex, guard.InstanceId);
            result = e.Submit(s, command); Assert.IsTrue(result.Accepted, result.RejectionReason); Assert.AreEqual(MatchPhase.Main, s.Phase);
            GameStateInvariants.AssertValid(s);
        }
    }
}
