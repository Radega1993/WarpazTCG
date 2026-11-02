using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>
    /// Two pinned WZ1 decks for the guided tutorial. Deck order is the draw order
    /// when <see cref="MatchSetup.PreserveMainDeckOrder"/> is set (top = index 0).
    /// </summary>
    public sealed class TutorialMatchFixture
    {
        public const ulong DefaultSeed = 42UL;

        /// <summary>Learner opening hand (first 5): 018,042,026,062,030 then draw 066.</summary>
        public static readonly string[] LearnerOpeningHand =
        {
            "WZ1-018", "WZ1-042", "WZ1-026", "WZ1-062", "WZ1-030"
        };

        public TutorialMatchFixture(
            ICardCatalog catalog,
            RuleConfig rules,
            PlayerSetup learner,
            PlayerSetup tutor)
        {
            Catalog = catalog;
            Rules = rules;
            Learner = learner;
            Tutor = tutor;
        }

        public ICardCatalog Catalog { get; }

        public RuleConfig Rules { get; }

        public PlayerSetup Learner { get; }

        public PlayerSetup Tutor { get; }

        public MatchSetup ToMatchSetup()
        {
            return new MatchSetup(
                DefaultSeed,
                Rules,
                Catalog.ContentVersion,
                Learner,
                Tutor,
                preserveMainDeckOrder: true,
                forceFirstPlayer0: true);
        }

        public static TutorialMatchFixture Create(ICardCatalog catalog, RuleConfig rules = null)
        {
            var resolved = rules ?? TutorialRules();
            var learner = new PlayerSetup(
                PlayerId.Player0,
                "Aprendiz",
                new DeckList(
                    "tutorial-learner",
                    "Escuela Akkedis",
                    new CardId("WZ1-001"),
                    new CardId("WZ1-014"),
                    LearnerMainDeck()));
            var tutor = new PlayerSetup(
                PlayerId.Player1,
                "Tutor",
                new DeckList(
                    "tutorial-tutor",
                    "Demostración Ígnea",
                    new CardId("WZ1-002"),
                    new CardId("WZ1-012"),
                    TutorMainDeck()));
            return new TutorialMatchFixture(catalog, resolved, learner, tutor);
        }

        /// <summary>
        /// Short Resolution so one legal Guardián→Héroe finishes the match.
        /// Tutor AI attacks characters (not heroes) so this path stays reachable.
        /// </summary>
        public static RuleConfig TutorialRules()
        {
            var a = RuleConfig.AlphaDefaults();
            return new RuleConfig(
                a.RuleVersion,
                a.InitialHandSize,
                initialResolution: 1,
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
                a.MaximumEventsPerTurn,
                a.MaximumDecisionsPerTurn);
        }

        /// <summary>
        /// Top of deck = draw order. Scripted opening + draws, then filler (≤3 copies).
        /// </summary>
        public static IReadOnlyList<CardId> LearnerMainDeck()
        {
            return BuildDeck(
                // Hand 5
                "WZ1-018", "WZ1-042", "WZ1-026", "WZ1-062", "WZ1-030",
                // Turn 1 draw
                "WZ1-066",
                // Turn 2 draw — Custodio (P3) clears Tutor Bruto
                "WZ1-019",
                // Turn 3 draw — Guardián (P6) finishes the Héroe
                "WZ1-024",
                // Remainder (keep 40 / ≤3 copies)
                "WZ1-018", "WZ1-018",
                "WZ1-042", "WZ1-042",
                "WZ1-026", "WZ1-026",
                "WZ1-062", "WZ1-062",
                "WZ1-030", "WZ1-030",
                "WZ1-066", "WZ1-066",
                "WZ1-019", "WZ1-019",
                "WZ1-024", "WZ1-024",
                "WZ1-027", "WZ1-027", "WZ1-027",
                "WZ1-039", "WZ1-039", "WZ1-039",
                "WZ1-043", "WZ1-043", "WZ1-043",
                "WZ1-074", "WZ1-074",
                "WZ1-046", "WZ1-046",
                "WZ1-072", "WZ1-054", "WZ1-054");
        }

        public static IReadOnlyList<CardId> TutorMainDeck()
        {
            return BuildDeck(
                // Hand 5 — first play script expects WZ1-050 in Vanguard
                "WZ1-050", "WZ1-034", "WZ1-042", "WZ1-026", "WZ1-038",
                "WZ1-035",
                "WZ1-050", "WZ1-050",
                "WZ1-034", "WZ1-034",
                "WZ1-042", "WZ1-042",
                "WZ1-026", "WZ1-026",
                "WZ1-038", "WZ1-038",
                "WZ1-035", "WZ1-035",
                "WZ1-027", "WZ1-027", "WZ1-027",
                "WZ1-043", "WZ1-043", "WZ1-043",
                "WZ1-062", "WZ1-062", "WZ1-062",
                "WZ1-074", "WZ1-074", "WZ1-074",
                "WZ1-058", "WZ1-058", "WZ1-058",
                "WZ1-023", "WZ1-023", "WZ1-023",
                "WZ1-022", "WZ1-022", "WZ1-064", "WZ1-064");
        }

        private static IReadOnlyList<CardId> BuildDeck(params string[] ids)
        {
            var list = new List<CardId>(ids.Length);
            for (var i = 0; i < ids.Length; i++)
            {
                list.Add(new CardId(ids[i]));
            }

            return list;
        }
    }
}
